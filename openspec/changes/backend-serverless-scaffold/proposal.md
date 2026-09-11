## Why

`金流帳務系統規劃.md` 與設計交付文件（`design/design-handoff/README.md`）已定義完整的業務規則（付款方式、四層費用結構、退費底線、撥款流程、退課仲裁、費率異動 30 天預告等），且使用者已定案技術棧：**後端使用 .NET，全 serverless 部署在 AWS（API Gateway + Lambda），資料層採用 DynamoDB**。目前 repo 只有前端的規劃（`frontend-mvp-scaffold` change），完全沒有後端的架構決策或行為契約。前端已經在多個 spec 中假設特定的後端可觀察行為（例如：訂單狀態唯讀、費用試算為前端純計算、認證 token 含角色 claim），需要一份後端規劃來承接並驗證這些假設，同時把業務規則文件中的狀態機轉譯成可實作的 API 行為契約。

## What Changes

- 建立後端架構決策：.NET 8 on AWS Lambda + API Gateway（HTTP API）、DynamoDB 單表設計、Cognito 身分驗證、S3 私有物件儲存、EventBridge/Step Functions 處理排程與 SLA 計時、SES/LINE Messaging API 通知、Secrets Manager 存金流商金鑰、AWS CDK（C#）作為 IaC。
- 依業務規則文件，定義八個後端能力（capability）的行為契約：身分與存取、課程與退費規則設定、報名與付款、費用與帳務核算、撥款批次、退課仲裁工單、平台費用與條款治理、通知派送。
- 排定 MVP 範圍：對齊業務規則文件第十四節「MVP 範圍界定」與前端 change 已排定的範圍，Phase 2 功能（系列課、候補、評價、Rich Menu、廣告錢包、分級權限、自動化銀行 API 撥款、電子發票）不在本次規劃內。
- 驗證並承接前端 change（`frontend-mvp-scaffold`）design.md 中列出的「前端對後端 API 的假設」，本 change 的每個假設驗證結果記錄在 design.md 的 Decisions 中。

## Capabilities

### New Capabilities
- `backend-identity-access`：帳號註冊、角色（老師/學生/管理者）、老師輕量身分驗證審核、認證 token 簽發假設、Admin 獨立驗證（含 MFA）。
- `backend-course-catalog`：課程建立、付款方式設定、退費規則設定（平台底線 + 老師微調驗證）、課程發佈狀態。
- `backend-order-checkout`：報名訂單建立、綠界/藍新金流串接、付款背景通知處理（含冪等性）、付款狀態機。
- `backend-fee-ledger`：四層費用結構自動計算（金流成本/平台服務費/撥款轉帳費）並寫入訂單成本明細。
- `backend-payout-batch`：月結撥款批次計算、最低撥款門檔、同行/跨行轉帳費判斷、撥款狀態機。
- `backend-refund-arbitration`：退課工單狀態機、老師 SLA 自動升級、管理者仲裁裁決、撥款後退款沖銷。
- `backend-platform-governance`：平台服務費/處理費/退費底線設定（30 天預告生效）、合作條款版本控管與重新同意流程、變更稽核紀錄。
- `backend-notification`：通知事件觸發、管道優先順序（站內＋Email 必開、LINE 選用）、範本化訊息內容。

### Modified Capabilities
（無，本 change 不修改 `frontend-mvp-scaffold` 已定義的前端 capability；若前端假設與本次後端決策衝突，將在 design.md 中記錄並回頭更新前端 change，而非在此直接修改前端 spec。）

## Impact

- 架構分層：採 DDD（`StepGo.Domain`/`StepGo.Application`/`StepGo.Infrastructure`）+ 獨立零依賴的 `StepGo.Contracts` 專案；`StepGo.Contracts` 是前端（`frontend-mvp-scaffold`）唯一允許 project reference 的後端專案，取代原本「前端手刻 DTO、待後端契約定案後置換」的做法。
- 新增程式碼：上述分層專案、Lambda composition root（依 capability 分組，例如 `StepGo.Api.Orders`、`StepGo.Api.Payouts` 等）、AWS CDK 基礎設施專案（實際建立於後續 apply 階段，本 change 僅規劃）。
- 不影響任何既有程式碼（目前 repo 內除設計交付檔案、業務規則文件與前端規劃外沒有其他程式碼）。
- 依賴：綠界/藍新的正式特店身分與 API 金鑰尚未取得，開發期間以金流商提供的測試環境（sandbox）或本地模擬 webhook 取代；LINE 官方帳號的正式帳號與 Messaging API channel 尚未申請，通知能力的 LINE 管道在 MVP 開發期間可先以介面留空/mock 方式驗證，不阻塞其他能力開發。
- 影響前端規劃：本 change 完成後，`frontend-mvp-scaffold` 的 Open Questions（API 契約、付款頁呈現方式、token 簽發者）需要回頭確認是否已被本次決策解答，若有落差需回去更新前端 change。
