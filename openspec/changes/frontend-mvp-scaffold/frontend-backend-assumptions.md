# 前端對後端 API 的假設清單

本文件整理 `frontend-mvp-scaffold` 實作過程中，前端對尚未存在的後端 API 所做的假設，供後續
`backend-serverless-scaffold`（或其後續 change）對齊或提出修正。每項假設列出對應的 spec
需求，以及目前前端程式碼中可追蹤的位置（型別定義、Mock API 端點）。

## 1. 認證與角色

**假設**：後端簽發的 JWT 內含 `role` claim（值為 `"teacher"` 或 `"student"`），前端僅解碼
payload 取出角色作導向依據，不驗證簽章。

- 對應需求：`frontend-auth` — 「登入註冊角色分流」
- 前端型別：`StepGo.ApiClient.Auth.JwtRoleParser`、`StepGo.ApiClient.Auth.StepGoAuthenticationStateProvider`
- 風險：若後端改用 session cookie 或其他機制取代 JWT payload 內的角色 claim，`JwtRoleParser`
  需要對應調整。

**假設**：管理者後台使用與老師/學生完全獨立的登入流程（獨立 Cognito User Pool、強制 2FA），
不與 `frontend-auth` 共用任何登入端點或 token。

- 對應需求：`frontend-admin-panel` — 「獨立側邊選單版面與強化登入流程」
- 前端型別：`StepGo.AdminPanel.Pages.AdminLogin`

**假設**：管理者後台的 IP 白名單檢核發生在網路層（CloudFront + AWS WAF IP set），前端僅顯示
提示文字，不做任何客戶端 IP 驗證。

- 對應需求：`frontend-admin-panel` — 「獨立側邊選單版面與強化登入流程」
- 對應基礎設施：`infra/StepGo.Infra/src/StepGoInfra/AdminPanelStack.cs`（`CfnWebACL` + `CfnIPSet`）

## 2. 訂單／付款狀態機

**假設**：訂單的付款狀態為後端提供的唯讀欄位，共有 5 種值：`Pending`／`Processing`／`Paid`／
`Failed`／`Refunded`；前端「我的課程」四分頁、老師端收入總覽、StatusTag 顯示皆直接依此欄位
分類，不另外新增衍生狀態欄位。

- 對應需求：`frontend-student-portal` — 「我的課程四分頁依既有欄位篩選」；`frontend-design-system`
  — 「設計代幣集中管理」
- 前端型別：`StepGo.UI.Status.PaymentStatus`；Mock 端點：`GET /api/payment-scenarios/{scenario}`

**假設**：學生報名付款流程中，ATM 付款狀態的變更僅能由後端接收金流背景通知後更新；前端介面
（含結果頁、訂單頁）不提供任何「已完成轉帳」之類的自我申報操作，也不存在對應的 API 呼叫。

- 對應需求：`frontend-student-portal` — 「付款結果頁依付款方式呈現不同狀態且不提供自我申報」
- 前端型別：`StepGo.StudentPortal.Pages.Checkout`（`result-atm-pending` 區塊無任何操作按鈕）

**假設**：課程的可用付款方式（信用卡／ATM）由老師在建立課程時設定，以兩個布林欄位表示，儲存
於課程資料本身；學生報名頁僅依此欄位動態顯示選項。

- 對應需求：`frontend-teacher-portal` — 「課程建立四步驟精靈含即時試算」；`frontend-student-portal`
  — 「報名付款三步驟且付款選項依老師設定動態顯示」
- 前端型別：`StepGo.TeacherPortal.Models.CourseDraft`（`AllowCreditCard`/`AllowAtm`）、
  `StepGo.StudentPortal.Models.CheckoutCourse`

## 3. 費用試算

**假設**：平台服務費率（10%）、信用卡手續費率（2.89%）、ATM 每筆手續費（NT$15）、撥款轉帳費
（同行 NT$10／跨行 NT$20）、退款處理費（NT$25）目前寫死為前端常數，試算完全在前端計算，不呼叫
任何 API；管理者後台調整費率後（30 天預告生效），假設這些前端常數需要與後端／Admin 設定的來源
同步更新機制（目前尚未實作，屬本 change 明確標記的整合缺口）。

- 對應需求：`frontend-marketing-site` — 「費用試算工具為前端純計算」；`frontend-teacher-portal`
  — 「課程建立四步驟精靈含即時試算」；`frontend-admin-panel` — 「費用設定變更需 30 天預告生效」
- 前端型別：`StepGo.PricingRules.PricingConstants`、`StepGo.PricingRules.PricingCalculator`
- 風險：這是 design.md「Risks / Trade-offs」已標記的已知落差來源，後端 change 需決定費率設定的
  單一真實來源（source of truth）與前端同步方式。

**假設**：退費比例的「平台底線」分為四個固定天數區間（開課前 14 日以上／7–13 日／1–6 日／開課
後），區間邊界本身視為固定不可調整，僅各區間對應的底線百分比可由管理者調整；老師可在底線之上
微調實際比例。

- 對應需求：`frontend-teacher-portal` — 「退費規則採平台底線加老師微調」；`frontend-admin-panel`
  — 「費用設定變更需 30 天預告生效」
- 前端型別：`StepGo.PricingRules.RefundPolicy`、`StepGo.PricingRules.RefundTier`

## 4. 退課工單與仲裁

**假設**：退課工單狀態為多階段流程（老師審核中 → 老師核准／駁回 → 逾期系統自動核定 → 升級平台
仲裁 → 仲裁已裁決 → 已完成），且每個訂單至多對應一張進行中的工單；工單狀態變為「已完成」時，
該訂單即歸類至學生「已取消/已退款」分頁。

- 對應需求：`frontend-teacher-portal` — 「退課工單審核與金額微調限制」；`frontend-student-portal`
  — 「退課申請與正式留言串」
- 前端型別：`StepGo.UI.Status.RefundTicketStatus`；Mock 端點：`GET /api/refund-ticket-scenarios/{scenario}`

**假設**：Message Thread（留言串）以「單一退課工單」為範圍（1:1），API 形狀假設類似
`GET/POST /refund-tickets/{ticketId}/messages`；一般問題聯繫改導向 LINE 官方帳號，前端不會為
一般問題建立任何留言串資料。

- 對應需求：`frontend-student-portal` — 「退課申請與正式留言串」
- 前端型別：`StepGo.StudentPortal.Services.StudentDataStore`（`GetMessages`/`PostMessage`）

**假設**：平台仲裁裁決為終局操作——送出後案件從待裁決佇列移除且不可覆核；裁決理由為必填並會
寫入案件歷史記錄與操作日誌。

- 對應需求：`frontend-admin-panel` — 「平台仲裁裁決需填寫理由」；「操作日誌可依類別篩選」

## 5. 老師身分驗證與撥款帳戶

**假設**：老師身分驗證狀態為 4 個固定值（填寫／審核中／未通過／通過），且非「已認證」狀態會
擋下課程建立入口並導向驗證頁。

- 對應需求：`frontend-teacher-portal` — 「身分驗證四狀態控管課程建立權限」
- 前端型別：`StepGo.UI.Status.VerificationStatus`

**假設**：撥款帳戶狀態為 4 個固定值（檢視／變更／核對中／銀行退回），「銀行退回」狀態附帶一則
文字說明退回原因；戶名一致性檢查（與身分驗證姓名比對）由後端執行，前端僅顯示提示文案。

- 對應需求：`frontend-teacher-portal` — 「撥款帳戶四狀態與戶名一致性提示」
- 前端型別：`StepGo.UI.Status.PayoutAccountStatus`

## 6. 列表查詢與匯出

**假設**：老師端學生名單／繳費明細、管理者端老師與課程目錄、操作日誌，其篩選（日期區間、驗證
狀態、課程狀態、關鍵字、日誌類別）目前皆在前端對已載入的完整清單做記憶體內過濾；真實後端串接
時，若資料量成長，可能需要改為伺服器端分頁 + 篩選參數，前端篩選邏輯（`OrderFiltering`、
`DirectoryFiltering`、`AuditLogFiltering`）屆時需要改為組出對應的查詢參數而非本地過濾。

- 對應需求：`frontend-teacher-portal` — 「學生名單與收入報表匯出」；`frontend-admin-panel`
  — 「老師與課程目錄搜尋」、「操作日誌可依類別篩選」

**假設**：CSV 匯出為前端對已取得資料的純字串轉換（`StudentRosterCsvExporter`），不需要後端提供
額外的匯出端點；若未來需要匯出超過前端一次可載入的資料量，才需要後端提供專用匯出 API。

- 對應需求：`frontend-teacher-portal` — 「學生名單與收入報表匯出」

## 7. 共用資料形狀（StepGo.ApiClient DTO）

本 change 在 `StepGo.ApiClient.Dtos` 定義了 `OrderDto`、`RefundTicketDto`、`PayoutBatchDto`、
`TeacherDto` 四個 DTO，欄位對齊上述各項假設；`mock-api/StepGo.MockApi` 專案以這些 DTO 作為
各狀態機情境的回應形狀（見 `mock-api/StepGo.MockApi/Scenarios/`），可作為後端 API 契約討論的
具體起點。

依 design.md 決策 5，待後端 change 建立 `StepGo.Contracts` 專案後，前端應改為直接 project
reference 該專案的型別，屆時本文件所列的 `StepGo.ApiClient.Dtos.*` 為暫時性假 DTO，需要與
`StepGo.Contracts` 的正式契約逐一核對取代。
