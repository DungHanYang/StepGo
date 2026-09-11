## Context

後端為全新（greenfield）系統，目前僅有業務規則文件（`金流帳務系統規劃.md`，共 18 節，涵蓋付款方式、老師輕量身分驗證、四層費用結構、帳務報表、撥款流程、退課仲裁工單、資料庫 ER 圖、MVP 範圍界定、學生註冊與通知管道、會員中心邏輯、老師合作條款同意流程、客服真人介入機制）與設計交付文件（狀態機的 UI 呈現）。使用者已定案技術棧：**.NET 8 on AWS Lambda + API Gateway（全 serverless），DynamoDB 作為資料層**。前端 change（`frontend-mvp-scaffold`）已先行規劃並記錄了對後端 API 的假設，本設計需要驗證這些假設並填補後端特有的決策（資料模型、狀態機轉換的伺服器端保證、非同步流程）。

業務規則文件的 ER 圖（第十三節）是以關聯式思維畫的（USERS/TEACHERS/COURSES/ORDERS/PAYOUT_BATCHES/REFUND_TICKETS/BROADCAST_WALLETS/USER_LINE_BINDINGS，多為 1-to-many 關聯），改用 DynamoDB 需要先做 access pattern 盤點才能定 table 設計，而不是照搬 ER 圖的表格結構。

## Goals / Non-Goals

**Goals:**
- 盤點 MVP 範圍內所有已知的資料存取模式（來自業務規則文件與前端 specs 的篩選/報表需求），據此設計 DynamoDB 的 table、PK/SK、GSI。
- 決定 Lambda 的分組方式、API Gateway 路由組織、認證/授權機制、非同步流程（撥款批次排程、退課工單 SLA 自動升級、金流背景通知處理）的具體 AWS 服務選型。
- 驗證前端 change 記錄的三個 Open Questions（API 契約細節、付款頁呈現方式、token 簽發者），並在本設計中給出明確答案。
- 讓 tasks.md 的每個任務都有明確的 AWS 服務與資料模型可以依循，不需要實作時才現場設計。

**Non-Goals:**
- 不產出正式 OpenAPI schema 文件（留給 apply 階段的實作產出，或作為本 change 之後的一個小任務，但不是本次規劃的必要產出）。
- 不決定綠界/藍新特店申請的商務流程（合約簽署、費率談判）——只處理技術整合面。
- 不實作自動化銀行撥款 API 串接（業務規則文件明確列為 Phase 2，MVP 為人工觸發+人工轉帳+系統回填憑證）。
- 不做電子發票串接（業務規則文件明確列為 Phase 2）。
- 不涵蓋廣告錢包/LINE 群發計費（業務規則文件列為 Phase 2）。

## Decisions

### 1. API 層：API Gateway HTTP API + Lambda（.NET 8）
選用 HTTP API 而非 REST API：延遲更低、成本更低，且 MVP 不需要 REST API 才有的請求驗證/WAF 整合等進階功能（admin 的 IP 白名單改由 CloudFront + WAF 在前端那層處理，不需要 API Gateway REST API 的資源政策）。
Lambda 依 capability 分組成數個函式（非每個 route 一個函式，也非單一巨石函式）：`Orders`、`Payouts`、`RefundTickets`、`Courses`、`Teachers`、`Admin`、`Notifications`——每個函式用 `Amazon.Lambda.AspNetCoreServer.Hosting` 包裝一個小型 ASP.NET Core Minimal API，內部路由用一般的 Minimal API endpoint 定義，降低冷啟動數量與部署複雜度之間的取捨。
替代方案：每個 API 路由一個獨立 Lambda——排除，函式數量會膨脹到數十個，部署與觀測成本過高，且 MVP 流量不需要這種細粒度的獨立擴縮。

### 2. 認證：Amazon Cognito，老師/學生共用一個 User Pool，Admin 獨立一個 User Pool（強制 MFA）
- 老師/學生 User Pool：Email 或手機號碼可作登入識別（對應業務規則「手機必填、Email 選填」，Cognito 的 username 用系統內部 user id，手機/Email 存為 attribute），簽發 JWT（access token 含自訂 claim：`role`=teacher/student）。
- Admin User Pool：獨立 pool，強制 MFA（TOTP），呼應設計文件「Admin 獨立網域、強制雙因素驗證」的要求。
- API Gateway 用 Cognito JWT authorizer 驗證 token，Lambda 內再依 `role` claim 做細粒度授權（例如老師只能操作 `teacher_id` 等於自己 user id 的資源，對應業務規則文件第十二節「RBAC + Row-level 權限控管」）。
- 回應前端 change 的 Open Question：token 由 **Cognito** 簽發，前端 `AuthenticationStateProvider` 直接解析 Cognito 簽發的 JWT 即可取得 `role` claim，不需要後端自行簽發 token。

### 3. 資料層：DynamoDB 單表設計（`StepGoTable`），以 access pattern 驅動
先列出 MVP 已知的存取模式（來自業務規則文件與前端 specs）：

| # | 存取模式 | 來源 |
|---|---|---|
| 1 | 依 user id 取得使用者/老師資料 | 登入、身分驗證頁 |
| 2 | 依 teacher id 列出該老師的課程 | 老師後台我的課程 |
| 3 | 依 course id 取得課程詳情 | 課程詳情頁、報名流程 |
| 4 | 依 student id 列出該學生的訂單（含依付款狀態/場次時間分類） | 學生我的課程四分頁 |
| 5 | 依 teacher id + 日期區間列出訂單（報表明細） | 老師收入報表 |
| 6 | 依 payout_batch id 列出該批次涵蓋的訂單 | 撥款批次明細 |
| 7 | 依 teacher id 列出該老師歷次撥款批次 | 老師撥款紀錄 |
| 8 | 依 order id 取得對應的退課工單 | 學生/老師退課頁 |
| 9 | 依狀態 = `admin_arbitration` 列出待仲裁工單 | 管理者仲裁佇列 |
| 10 | 依 teacher id 列出該老師的待處理退課工單（含 SLA 排序） | 老師退課審核清單 |
| 11 | 依類別（費率/驗證/仲裁/權限/撥款）列出操作日誌 | Admin 操作日誌 |
| 12 | 全平台待撥款總額、待審核老師數等彙總數字 | Admin 金流總覽 |

Table 設計：
```
PK                          SK                              用途（對應上表）
USER#<userId>               METADATA                        1
TEACHER#<teacherId>         METADATA                        1
TEACHER#<teacherId>         COURSE#<courseId>                2
COURSE#<courseId>           METADATA                        3
STUDENT#<studentId>         ORDER#<createdAt>#<orderId>      4
ORDER#<orderId>              METADATA                        （訂單本體，含 payment_status/payout_status 等欄位）
PAYOUTBATCH#<batchId>       ORDER#<orderId>                  6
TEACHER#<teacherId>         PAYOUTBATCH#<createdAt>          7
ORDER#<orderId>              REFUNDTICKET                    8
TEACHER#<teacherId>         REFUNDTICKET#<slaDeadline>#<id>  10
AUDITLOG#<category>         <timestamp>#<logId>              11
AGGREGATE#PLATFORM           SUMMARY                          12（滾動彙總 item，交易寫入時一併更新）

GSI1（依老師+日期區間查訂單，對應第 5 項）：
  PK = GSI1PK = TEACHER#<teacherId>   SK = GSI1SK = ORDER#<createdAt>

GSI2（依狀態查退課工單，對應第 9 項）：
  PK = GSI2PK = REFUNDTICKET#STATUS#<status>   SK = GSI2SK = <slaDeadline>#<ticketId>
```
- **交易一致性**：訂單標記已付款 → 更新老師彙總 item → （若已進入可撥款）更新待撥款彙總，這類多 item 一起成功/失敗的操作用 `TransactWriteItems`。
- **Webhook 冪等性**：綠界/藍新背景通知處理前，先以條件寫入（`attribute_not_exists(processedNotificationId)`）鎖定該次通知的唯一 id，避免重複處理同一次通知。
- **彙總數字**：Admin 金流總覽與老師收入總覽需要的即時加總數字，不現場 scan 計算，而是在每次相關寫入時以 transaction 同步更新對應的滾動彙總 item（`AGGREGATE#PLATFORM`、`TEACHER#<id>#SUMMARY`）。
- **超出 GSI 覆蓋範圍的未來報表需求**（例如管理者要做跨老師、跨月份的任意維度分析）：MVP 不做，若未來需要，走 DynamoDB Streams → Kinesis Firehose → S3 → Athena 的旁路分析管道，不影響本表設計。

### 4. 非同步與排程：EventBridge Scheduler + SQS + Step Functions
- **金流背景通知接收**：API Gateway 端點接收綠界/藍新的 webhook → 立即寫入 SQS（避免 Lambda 短暫故障或流量尖峰丟單）→ 另一 Lambda 消費 SQS 更新訂單狀態（冪等處理，見上）。
- **月結撥款批次**：EventBridge Scheduler 於每月 5 日觸發 Lambda，篩選「可撥款」訂單、依老師分組、判斷同行/跨行轉帳費（比對老師撥款帳戶銀行代碼與平台永豐帳戶）、產生撥款批次草稿（MVP 為人工確認後才真正標記已撥款，不自動轉帳）。
- **退課工單 SLA 自動升級**：老師進入 `teacher_reviewing` 狀態時，啟動一個 **Step Functions** 執行（`Wait` 5 個工作日 → 檢查工單狀態是否仍為 `teacher_reviewing` → 若是則自動轉為 `admin_arbitration`）。選用 Step Functions 而非單純的 EventBridge 定時任務，因為需要「等待期間工單狀態可能被老師的動作打斷（老師核准/駁回）」——用 Step Functions 的 `Wait` + 條件檢查表達這種「除非提前發生某事，否則等到期限做某事」的邏輯最直接，且執行歷程可觀測、可重試。
- **通知派送**：訂單狀態變更等事件透過 EventBridge 自訂 event bus 發布，`Notifications` Lambda 訂閱後依業務規則的管道優先順序（Email 保底、站內必留、LINE 選用加值）分別呼叫 SES/寫入通知資料/呼叫 LINE Messaging API。

### 5. 檔案儲存：S3（老師身分證照片等私有物件）
獨立的私有 bucket，物件金鑰包含 teacher id，存取一律透過後端產生的短效 presigned URL，僅限 Admin 角色的 API 呼叫可取得，不開放公開讀取。

### 6. 機密管理：Secrets Manager
存放綠界/藍新的 API 金鑰、LINE Messaging API channel secret；Lambda 執行角色僅有讀取自己需要的機密的權限（依 capability 分開的 secret，不共用一份萬能機密）。

### 7. IaC：AWS CDK（C#），與前端 change 共用同一個 CDK app 但不同 stack
延續前端 change 已決定的 CDK 選型；後端新增 `BackendStack`（Lambda、API Gateway、DynamoDB、Cognito、SQS、EventBridge、Step Functions、S3、Secrets）獨立於前端的 `MarketingStack`/`PortalStack`，兩者透過 CDK 的 cross-stack reference 傳遞 API Gateway 端點網址給前端。

### 8. 前端假設驗證結果
逐一回應 `frontend-mvp-scaffold` design.md 的 Open Questions：
- **認證 token 簽發者** → 已在本文件決策 2 中確定為 Cognito。
- **API 契約細節（欄位命名、分頁、錯誤格式）** → 分頁採 DynamoDB 原生的 `LastEvaluatedKey` 轉換成 opaque cursor 字串回傳給前端（`nextCursor`），不做 offset 分頁；錯誤格式採統一的 `{ code, message }` JSON 結構。前端 `StepGo.ApiClient` 的 DTO 待本 change 進入 apply 階段產出實際 API 後回頭核對調整。
- **綠界/藍新付款頁呈現方式** → 採用金流商提供的**導轉外部付款頁**（Redirect），不做 iframe 嵌入，理由：兩大金流商官方 SDK 對 Redirect 模式的文件與範例最完整、風險最低，MVP 不需要為了避免跳轉這種次要體驗優化增加整合複雜度與 PCI-DSS 相關的合規負擔。此決策需回頭更新 `frontend-mvp-scaffold` 的對應 Open Question為「已解答」。

## Risks / Trade-offs

- [DynamoDB 單表設計一旦上線後要新增「未預期的查詢模式」成本較高（通常需要新增 GSI 或做資料遷移）] → 本設計已盤點 MVP 已知的 12 種存取模式；新增查詢需求先評估能否用既有 GSI 覆蓋，真的無法覆蓋才新增 GSI（DynamoDB 可在既有表上新增 GSI，不需整表遷移，但需重新回填既有資料的 GSI 屬性）。
- [Step Functions 為每張退課工單各啟動一次執行，工單量大時執行數量與成本上升] → MVP 規模下退課工單量不高（依業務規則文件，退課本身就是相對少數情境），成本可接受；若未來量大，可改為單一定時 Lambda 掃描 SLA 到期工單，屆時再權衡改動。
- [SQS 緩衝金流通知會讓「付款完成」到「前端看到已付款狀態」多一段非同步延遲] → 延遲通常在秒級，可接受；比起讓 Lambda 直接同步處理 webhook（若 Lambda 當下故障就真的丟單）更穩健。
- [Cognito 雙 User Pool（老師/學生 vs Admin）增加維運復雜度] → 業務規則明確要求 Admin 需要獨立、更嚴格的驗證機制，複雜度為必要成本。
- [.NET 在 Lambda 上的冷啟動時間高於 Node.js/Python] → 對 API 端點（非 SSR 頁面渲染）的冷啟動影響通常可接受；若量測後發現使用者體感延遲明顯，可對高頻端點（訂單建立、webhook 接收）加購 Provisioned Concurrency。

## Migration Plan

無現有系統需遷移（greenfield）。建置順序如下（詳見 tasks.md）：
1. AWS CDK 基礎設施骨架（DynamoDB table、Cognito pools、API Gateway、Lambda 專案骨架）
2. `backend-identity-access`（註冊、老師驗證、Cognito 整合）
3. `backend-course-catalog`（課程建立、付款方式、退費規則）
4. `backend-order-checkout`（訂單建立、金流串接、webhook 處理）
5. `backend-fee-ledger`（費用計算引擎）
6. `backend-payout-batch`（撥款批次）
7. `backend-refund-arbitration`（退課工單、SLA、仲裁）
8. `backend-platform-governance`（費率設定、條款版本控管）
9. `backend-notification`（通知派送）
10. 與前端 change 的整合驗收（依 tasks.md 最後一組任務逐一比對前端假設清單）

## Open Questions

- 綠界/藍新特店申請進度與正式 API 金鑰取得時間——不影響本次規劃的架構決策，只影響 apply 階段何時能接上真實金流商（開發期間先用 sandbox/模擬 webhook）。
- LINE 官方帳號正式申請與 Messaging API channel 設定——同上，不影響架構決策，`backend-notification` 的 LINE 管道在真正申請下來前以介面留空/mock 驗證。
