## 1. 分層骨架與 AWS CDK 基礎設施

- [x] 1.1 建立 `StepGo.Domain`（entity/value object/repository 介面骨架，暫無其他專案參照）、`StepGo.Contracts`（零依賴的 DTO/enum 專案，依 8 個 capability 分 namespace）兩個專案，驗證方式：`dotnet build` 成功，且用 `dotnet list <StepGo.Contracts.csproj> reference` 確認其專案參照清單為空
- [x] 1.2 建立 `StepGo.Application`（use case handler + port 介面，只參照 `StepGo.Domain`）與 `StepGo.Infrastructure`（DynamoDB repository/Cognito/金流/通知等實作，參照 `StepGo.Domain`+`StepGo.Application`）兩個專案，驗證方式：`dotnet build` 成功，且用專案參照檢查工具確認 `StepGo.Domain` 沒有反向參照到這兩者
- [x] 1.3 建立 .NET Lambda composition root 專案骨架（依 capability 分組：`StepGo.Api.Identity`、`StepGo.Api.Courses`、`StepGo.Api.Orders`、`StepGo.Api.Payouts`、`StepGo.Api.RefundTickets`、`StepGo.Api.Governance`、`StepGo.Api.Notifications`），各參照 `StepGo.Application`+`StepGo.Infrastructure`+`StepGo.Contracts`，驗證方式：`dotnet build` 全部專案成功，每個專案至少有一個 health check endpoint 可回應 200
- [x] 1.4 建立共用的 DynamoDB 存取層輔助類別於 `StepGo.Infrastructure`（封裝 PK/SK 組裝、`TransactWriteItems` 輔助方法），驗證方式：unit test 覆蓋 PK/SK 組裝邏輯與至少一個 transaction 輔助方法的成功/失敗情境
- [x] 1.5 建立 CDK（C#）`BackendStack`：DynamoDB `StepGoTable`（含 GSI1、GSI2）、Cognito 老師/學生 User Pool、Cognito Admin User Pool（強制 MFA），驗證方式：`cdk synth` 成功產出對應資源的 CloudFormation 樣板
- [x] 1.6 建立 API Gateway HTTP API 與 Cognito JWT authorizer 設定，驗證方式：`cdk synth` 樣板中可見 authorizer 綁定於路由
- [x] 1.7 建立 SQS 佇列（金流背景通知緩衝）、EventBridge 自訂 event bus（通知事件）、EventBridge Scheduler（月結撥款）、Step Functions state machine 骨架（退課 SLA 計時），驗證方式：`cdk synth` 成功且各資源可在樣板中查得
- [x] 1.8 建立 S3 私有 bucket（身分證照片）與 Secrets Manager 機密骨架（綠界/藍新金鑰、LINE channel secret 占位），驗證方式：`cdk synth` 成功且 bucket 政策為私有（無公開讀取）

## 2. `backend-identity-access`

- [x] 2.1 實作學生/老師註冊 API（手機必填驗證、Email 選填），驗證方式：整合測試涵蓋缺少手機號碼被拒絕的情境
- [x] 2.2 實作老師身分驗證送出 API（真實姓名、身分證字號加密儲存、身分證照片上傳至 S3、撥款帳戶戶名比對），驗證方式：unit test 覆蓋戶名不一致擋下送出的情境
- [x] 2.3 實作管理者審核 API（通過/退回含理由），並在課程建立/發佈 API 加上驗證狀態守衛，驗證方式：整合測試涵蓋未認證老師呼叫課程建立 API 收到 403
- [x] 2.4 實作 Row-level 授權中介層（依 JWT 中的 user id/role 過濾查詢範圍），驗證方式：整合測試涵蓋老師 A 存取老師 B 資料被拒絕
- [x] 2.5 設定 Admin User Pool 的 MFA 強制原則並驗證登入流程，驗證方式：整合測試（或 Cognito 設定檢查）確認未完成 MFA 無法取得可呼叫其他 API 的有效 token

## 3. `backend-course-catalog`

- [x] 3.1 實作課程建立 API（付款方式複選、至少一項驗證），驗證方式：unit test 覆蓋未選付款方式被拒絕的情境
- [x] 3.2 實作退費規則驗證邏輯（比對平台底線設定），驗證方式：unit test 覆蓋低於底線的設定被拒絕、高於底線可通過兩種情境
- [x] 3.3 實作課程發佈 API 與狀態守衛，驗證方式：整合測試涵蓋未認證老師無法發佈課程

## 4. `backend-order-checkout`

- [x] 4.1 實作報名下單 API（驗證付款方式是否為課程開放選項、決定 `ChoosePayment` 參數、呼叫金流商 sandbox 下單），驗證方式：unit test 覆蓋選擇未開放付款方式被拒絕、兩種付款方式皆開放時 `ChoosePayment=ALL` 兩種情境
- [x] 4.2 實作金流背景通知接收端點 → 寫入 SQS，驗證方式：整合測試確認端點收到請求後訊息出現在 SQS
- [x] 4.3 實作 SQS 消費 Lambda，含冪等性判斷（條件寫入唯一交易識別碼）與訂單狀態更新，驗證方式：整合測試覆蓋重複通知只處理一次的情境
- [x] 4.4 實作 ATM 逾期標記邏輯與退款方式分流（信用卡自動 API／ATM 待人工轉帳標記），驗證方式：unit test 覆蓋 ATM 退款被標記為待人工轉帳而非呼叫自動退款 API

## 5. `backend-fee-ledger`

- [x] 5.1 實作四層費用計算引擎（金流手續費/平台服務費/應收淨額），驗證方式：unit test 覆蓋信用卡與 ATM 兩種費率計算範例
- [x] 5.2 實作訂單費率版本綁定（記錄成立當下適用版本 id），驗證方式：unit test 驗證費率調整後舊訂單金額不受影響
- [x] 5.3 實作撥款批次層級的轉帳費計算（同行/跨行判斷），驗證方式：unit test 覆蓋同行帳戶不收費、跨行帳戶收費兩種情境

## 6. `backend-payout-batch`

- [x] 6.1 實作月結撥款批次計算 Lambda（EventBridge Scheduler 觸發），含最低門檔判斷，驗證方式：unit test 覆蓋未達 NT$500 門檔的老師本期不列入撥款清單
- [x] 6.2 實作撥款帳戶戶名檢核與銀行退回標記 API，驗證方式：整合測試覆蓋銀行退回後訂單重新進入下次撥款清單
- [x] 6.3 實作撥款批次明細查詢 API（含涵蓋訂單清單、淨額加總、轉帳手續費、實收金額），驗證方式：整合測試驗證回應內容包含完整明細欄位

## 7. `backend-refund-arbitration`

- [x] 7.1 實作退課申請 API 與自動試算/逾期自動駁回邏輯，驗證方式：unit test 覆蓋超過退費期限自動駁回但保留申訴入口的情境
- [x] 7.2 實作老師核准/駁回 API（金額微調上限、理由必填），驗證方式：unit test 覆蓋核准金額超過原始繳費金額被拒絕的情境
- [x] 7.3 實作 Step Functions state machine：工單進入審核中時啟動執行，等待 5 個工作日後檢查狀態並自動升級，驗證方式：以 Step Functions 本地測試或整合測試模擬時間流逝，驗證逾時未回應的工單被自動轉為仲裁狀態
- [x] 7.4 實作管理者仲裁裁決 API（終態、不可再升級），驗證方式：整合測試驗證裁決後的工單狀態轉換與不可逆性
- [x] 7.5 實作撥款後退款扣款來源判斷邏輯，驗證方式：unit test 覆蓋已撥款/未撥款兩種訂單的退款扣款來源
- [x] 7.6 實作共用留言串 API 與獨立內部備註 API（權限分離：內部備註僅管理者可讀），驗證方式：整合測試驗證內部備註不出現在雙方可見的留言串查詢結果中

## 8. `backend-platform-governance`

- [x] 8.1 實作費率/底線變更 API 與 30 天預告生效檢核，驗證方式：unit test 覆蓋生效日小於 30 天被拒絕的情境
- [x] 8.2 實作變更紀錄查詢 API，驗證方式：整合測試驗證每次成功變更都產生對應紀錄
- [x] 8.3 實作條款版本控管 API（新增版本不覆蓋舊版本、老師同意記錄綁定版本 id），驗證方式：整合測試驗證修改條款後舊版本內容仍可查詢
- [x] 8.4 實作費率生效後的重新同意限制邏輯（未同意老師無法發佈新課程，但舊訂單不受影響），驗證方式：整合測試覆蓋此情境

## 9. `backend-notification`

- [x] 9.1 實作通知事件發布（EventBridge custom bus）與 `StepGo.Api.Notifications` 訂閱處理，驗證方式：整合測試驗證事件發布後訂閱端有收到並處理
- [x] 9.2 實作管道優先順序邏輯（Email 保底、站內必留、LINE 選用）與 SES/LINE Messaging API 呼叫（LINE 端可先以介面 mock），驗證方式：unit test 覆蓋未留 Email 未綁 LINE 仍有站內記錄、已綁 LINE 額外推播兩種情境
- [x] 9.3 實作通知範本資料模型與管理者可調整的範本管理 API，驗證方式：整合測試驗證調整範本後下次觸發即套用新文字
- [x] 9.4 確認站內/Email 通知設定不提供關閉選項，驗證方式：unit test 驗證帳號設定 API 不接受停用這兩個管道的請求

## 10. 整合驗收與前端假設比對

- [x] 10.1 依 design.md 的「前端假設驗證結果」逐項核對 `frontend-mvp-scaffold` 的 Open Questions 是否已解答，並回頭更新該 change 的 design.md，驗證方式：兩份 design.md 的相關章節內容一致，不再互相矛盾
- [x] 10.2 產出目前已實作 API 的最小 OpenAPI 描述（供前端 `StepGo.ApiClient` 之後置換 mock 實作），驗證方式：OpenAPI 文件可通過基本 schema 驗證工具檢查
- [ ] 10.3 以金流商 sandbox 環境執行一次完整的報名下單 → 背景通知 → 費用計算 → 撥款批次的端到端演練，驗證方式：演練記錄顯示每個階段的資料狀態轉換符合對應 spec 的 Scenario 描述
