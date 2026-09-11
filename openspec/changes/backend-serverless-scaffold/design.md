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

### 1. 程式碼分層與跨專案合約：DDD（Domain/Application/Infrastructure）+ 獨立的 `StepGo.Contracts`
前後端同屬一個 repo（monorepo），且都是 .NET/C#，因此用一個獨立、零依賴的合約專案取代「維護 OpenAPI schema 再產生 client」的做法：
- **`StepGo.Domain`**：entity、value object、領域事件、repository 介面、純業務規則（退費底線驗證、費用計算核心邏輯、狀態機轉換規則）。不依賴任何其他層。
- **`StepGo.Application`**：use case（command/query handler），定義對外部依賴的 port 介面（`IPaymentGateway`、`INotificationSender`、`IUnitOfWork` 等）；只依賴 `StepGo.Domain`。
- **`StepGo.Infrastructure`**：DynamoDB repository 實作、Cognito、綠界/藍新 client、SES/LINE、EventBridge/SQS/Step Functions 整合；實作 `StepGo.Application` 定義的 port，依賴 `StepGo.Domain` 與 `StepGo.Application`。
- **`StepGo.Contracts`**：純 DTO/enum（例如 `OrderDto`、`RefundTicketDto`、`PayoutBatchDto`），**零依賴**（不 reference Domain/Application/Infrastructure 任何一層）。這是唯一前端四個 Blazor 應用允許 reference 的後端專案。
- **`StepGo.Api.<Capability>`**（Lambda composition root）：依賴 `StepGo.Application` + `StepGo.Infrastructure` + `StepGo.Contracts`，負責在請求進入時把 HTTP 請求映射成 Application 的 command/query，處理完後把 Application 回傳的結果映射成 `StepGo.Contracts` 的 DTO 再序列化回應——這一層是 Domain/Application 內部模型與對外合約之間唯一的轉譯點。

依賴方向為單向：`Domain ← Application ← Infrastructure`，`StepGo.Api.*` 同時依賴三者並對外曝露 `StepGo.Contracts`；前端只依賴 `StepGo.Contracts`，不會、也不能拉到 Domain/Application/Infrastructure（以及它們攜帶的 AWS SDK/DynamoDB 相依性）。

`StepGo.Contracts` 內部可依 8 個 capability 分 namespace（`Contracts.Orders`、`Contracts.RefundTickets`…）方便對照，但 MVP 階段先放同一個專案，不拆成多個獨立套件，避免過度模組化；若之後真的變得難維護再拆。

替代方案：讓前端直接消費 Application 層的 command/query 物件——排除，因為 Application 層的形狀會跟著 use case 內部重構變動，直接暴露會讓前端被迫跟著每次內部重構改動；也會把 Infrastructure 的 AWS SDK 相依性透過 project reference 鏈條帶進前端專案。
替代方案：用 OpenAPI schema + 產生的 client（NSwag/Kiota）取代共用專案——保留作為未來若要支援非 .NET 的第三方客戶端時的路徑，但既然前後端目前都是同一個 repo 裡的 C#，直接共用型別更簡單、零轉譯成本，MVP 階段不需要多一層 schema 產生流程。

### 2. API 層：API Gateway HTTP API + Lambda（.NET 10，Native AOT，`provided.al2023` 自訂執行環境）
選用 HTTP API 而非 REST API：延遲更低、成本更低，且 MVP 不需要 REST API 才有的請求驗證/WAF 整合等進階功能（admin 的 IP 白名單改由 CloudFront + WAF 在前端那層處理，不需要 API Gateway REST API 的資源政策）。
Lambda 依 capability 分組成數個函式（非每個 route 一個函式，也非單一巨石函式）：`Orders`、`Payouts`、`RefundTickets`、`Courses`、`Teachers`、`Admin`、`Notifications`。

**執行模型改為 Native AOT**（取代原先 `Amazon.Lambda.AspNetCoreServer.Hosting` 包裝 Minimal API 的方案）：
- 每個 `StepGo.Api.<Capability>` 專案設定 `<PublishAot>true</PublishAot>`，以 `dotnet publish -r linux-x64` 產出原生執行檔 `bootstrap`，部署到 Lambda 的 `provided.al2023` 自訂執行環境（CDK `Runtime.PROVIDED_AL2023`），不使用託管的 `dotnet` 執行環境——因此 Lambda 執行環境版本與專案的 .NET SDK 版本（.NET 10）脫鉤，AOT 產出的是自帶執行期的原生二進位。
- 用 `Amazon.Lambda.RuntimeSupport` 的 `LambdaBootstrapBuilder` 搭配一個輕量、手寫的路由器（依 HTTP method + path 比對，非反射式的 ASP.NET Core Minimal API 端點解析），避免 `Amazon.Lambda.AspNetCoreServer.Hosting` 內部大量反射造成 AOT 相容性問題與可觀測的執行期警告。
- 所有序列化改用 `System.Text.Json` 的 source generator（每個 capability 一個 `JsonSerializerContext`，`[JsonSerializable(typeof(XxxDto))]` 逐一標註 `StepGo.Contracts` 的 DTO），不依賴反射式序列化。
- AWS SDK 相依套件一律取最新穩定版（`AWSSDK.DynamoDBv2`、`AWSSDK.CognitoIdentityProvider`、`AWSSDK.S3`、`AWSSDK.SecretsManager`、`AWSSDK.SQS`、`AWSSDK.EventBridge`、`AWSSDK.SimpleEmail`、`AWSSDK.StepFunctions` 等），並避開 `Amazon.DynamoDBv2.DocumentModel` 的動態 `Document` 型別，改用強型別的低階 `AttributeValue` 轉換，降低 AOT trim 警告面積。
- 好處：AOT 原生執行檔啟動速度顯著優於託管 CLR 冷啟動，改變了原先「決策 2 的取捨」與「Risks 一節『.NET 冷啟動高於 Node/Python』」的判斷（見下方 Risks 更新）。
替代方案：每個 API 路由一個獨立 Lambda——排除，函式數量會膨脹到數十個，部署與觀測成本過高，且 MVP 流量不需要這種細粒度的獨立擴縮。
替代方案：沿用 `Amazon.Lambda.AspNetCoreServer.Hosting` 託管執行環境（非 AOT）——排除，使用者已明確要求 Lambda 端要用 Native AOT 部署。

### 3. 認證：Amazon Cognito，老師/學生共用一個 User Pool，Admin 獨立一個 User Pool（強制 MFA）
- 老師/學生 User Pool：Email 或手機號碼可作登入識別（對應業務規則「手機必填、Email 選填」，Cognito 的 username 用系統內部 user id，手機/Email 存為 attribute），簽發 JWT（access token 含自訂 claim：`role`=teacher/student）。
- Admin User Pool：獨立 pool，強制 MFA（TOTP），呼應設計文件「Admin 獨立網域、強制雙因素驗證」的要求。
- API Gateway 用 Cognito JWT authorizer 驗證 token，Lambda 內再依 `role` claim 做細粒度授權（例如老師只能操作 `teacher_id` 等於自己 user id 的資源，對應業務規則文件第十二節「RBAC + Row-level 權限控管」）。
- 回應前端 change 的 Open Question：token 由 **Cognito** 簽發，前端 `AuthenticationStateProvider` 直接解析 Cognito 簽發的 JWT 即可取得 `role` claim，不需要後端自行簽發 token。

### 4. 資料層：DynamoDB 單表設計（`StepGoTable`），以 access pattern 驅動
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

Table 設計（v2，取代初版草稿——初版對模式 2/4/10 用「副本 item」而非 GSI，會需要應用層在每次訂單/課程/工單狀態變更時手動同步兩份資料，屬於不必要的一致性風險，改為一律用 GSI 承載次要查詢維度，讓 DynamoDB 自己保證 base item 與索引同步）：
```
PK                          SK                              用途（對應上表）
USER#<userId>               METADATA                        1
TEACHER#<teacherId>         METADATA                        1
COURSE#<courseId>           METADATA                        2、3（本體含 teacherId 屬性）
ORDER#<orderId>             METADATA                        4、5（本體含 studentId/teacherId/payment_status/payout_status 等欄位）
PAYOUTBATCH#<batchId>       METADATA                        6、7（批次本體：total_amount/transfer_fee/paid_at，含 teacherId 屬性）
PAYOUTBATCH#<batchId>       ORDER#<orderId>                 6（批次結算當下的訂單金額快照，非訂單本體的即時鏡像，見下方交易一致性）
ORDER#<orderId>             REFUNDTICKET                    8（固定 SK，天然保證一筆訂單最多一張工單）
AUDITLOG#<category>         <timestamp>#<logId>             11
AGGREGATE#PLATFORM          SUMMARY                         12（滾動彙總 item，交易寫入時一併更新，見下方熱分區風險）
TEACHER#<teacherId>         SUMMARY#<yyyymm>                12（老師當月彙總；用月份切分 item，一是不與模式 7 的 PAYOUTBATCH 查詢共用同一個熱 partition，二是讓老師報表的「本期」有明確月份邊界）

**teacherId = userId**：老師是使用者的 1:1 身分延伸（對應業務規則 ER 圖 `USERS ||--o| TEACHERS`），兩者共用同一個 id，登入時拿到 userId 就能直接組出 `TEACHER#<teacherId>` 這個 PK 去查是否為已認證老師，不需要額外的 id 對照查詢。

GSI1（依老師查詢，涵蓋課程/訂單/撥款批次三種本體，用 SK 前綴區分實體，對應第 2、5、7 項）：
  PK = GSI1PK = TEACHER#<teacherId>
  SK = GSI1SK = COURSE#<courseId> | ORDER#<createdAt>#<orderId> | PAYOUTBATCH#<createdAt>
  （分別作為屬性加在 COURSE/METADATA、ORDER/METADATA、PAYOUTBATCH/METADATA 三種本體 item 上；查詢時用 `begins_with` 篩選要哪種實體，或不加條件一次拿到某老師所有時間軸活動）

GSI2（依狀態查退課工單，對應第 9 項）：
  PK = GSI2PK = REFUNDTICKET#STATUS#<status>   SK = GSI2SK = <slaDeadline>#<ticketId>

GSI3（依老師查其待處理退課工單並依 SLA 排序，對應第 10 項；與 GSI2 同一個 base item，只是另一組索引屬性）：
  PK = GSI3PK = TEACHER#<teacherId>   SK = GSI3SK = <slaDeadline>#<ticketId>

GSI4（依學生查訂單，對應第 4 項；付款狀態/場次時間分類直接讀本體欄位在應用層篩選，不需要為每種篩選條件各開一個索引）：
  PK = GSI4PK = STUDENT#<studentId>   SK = GSI4SK = ORDER#<createdAt>
```
- **交易一致性**：所有會動到訂單狀態的寫入流程都用 `TransactWriteItems`，明確列出每個流程觸及哪些 item，不再只描述付款這一條路徑：
  - 付款完成（webhook 消費端）：更新 `ORDER/METADATA`（payment_status）→ 更新 `TEACHER#<teacherId>/SUMMARY#<yyyymm>` → 更新 `AGGREGATE#PLATFORM/SUMMARY`。
  - 撥款批次標記已撥款：更新 `ORDER/METADATA`（payout_status）→ 寫入 `PAYOUTBATCH#<batchId>/ORDER#<orderId>` 快照 → 更新 `PAYOUTBATCH#<batchId>/METADATA` 彙總欄位。
  - 退款核准完成：更新 `ORDER/METADATA`（payment_status=refunded）→ 更新 `ORDER#<orderId>/REFUNDTICKET`（status）→ 視是否已撥款決定是否更新對應 `TEACHER#<teacherId>/SUMMARY#<yyyymm>` 扣回金額。
  因為改用 GSI 取代副本 item，上述每個流程需要交易寫入的 item 數量比初版草稿少（不用再多寫一份 STUDENT#.../ORDER#... 或 TEACHER#.../COURSE#... 副本），出錯機率也隨之降低。
- **Webhook 冪等性**：綠界/藍新背景通知處理前，先以條件寫入（`attribute_not_exists(processedNotificationId)`）鎖定該次通知的唯一 id，避免重複處理同一次通知。
- **熱分區風險（新增，初版未提及）**：`AGGREGATE#PLATFORM/SUMMARY` 與 `TEACHER#<teacherId>/SUMMARY#<yyyymm>` 是每次相關交易都會寫入的固定 item，MVP 流量下沒有問題，但屬於典型的 DynamoDB 熱 item；高併發下 `TransactWriteItems` 對同一 item 的競爭會拋出 `TransactionConflictException`，需要應用層做重試。量大後的出口是把彙總計算移出付款這條關鍵交易路徑，改用 DynamoDB Streams 非同步更新彙總（犧牲彙總數字的即時性換取交易路徑降壓），詳見 Risks / Trade-offs。
- **超出 GSI 覆蓋範圍的未來報表需求**（例如管理者要做跨老師、跨月份的任意維度分析）：MVP 不做，若未來需要，走 DynamoDB Streams → Kinesis Firehose → S3（Iceberg table）→ Athena 的旁路分析管道，不影響本表設計，詳見決策 10。

### 5. 非同步與排程：EventBridge Scheduler + SQS + Step Functions
- **金流背景通知接收**：API Gateway 端點接收綠界/藍新的 webhook → 立即寫入 SQS（避免 Lambda 短暫故障或流量尖峰丟單）→ 另一 Lambda 消費 SQS 更新訂單狀態（冪等處理，見上）。
  - **DLQ/redrive 策略**：主 queue 設定 `maxReceiveCount=5`，超過後訊息轉入專屬 DLQ；DLQ 深度 > 0 觸發 CloudWatch Alarm → SNS Email 通知人工介入（MVP 不做自動重放，人工確認原因後手動 redrive）。
- **月結撥款批次**：EventBridge Scheduler 於每月 5 日觸發 Lambda，篩選「可撥款」訂單、依老師分組、判斷同行/跨行轉帳費（比對老師撥款帳戶銀行代碼與平台永豐帳戶）、產生撥款批次草稿（MVP 為人工確認後才真正標記已撥款，不自動轉帳）。
- **退課工單 SLA 自動升級**：老師進入 `teacher_reviewing` 狀態時，啟動一個 **Step Functions** 執行（`Wait` 5 個工作日 → 檢查工單狀態是否仍為 `teacher_reviewing` → 若是則自動轉為 `admin_arbitration`）。選用 Step Functions 而非單純的 EventBridge 定時任務，因為需要「等待期間工單狀態可能被老師的動作打斷（老師核准/駁回）」——用 Step Functions 的 `Wait` + 條件檢查表達這種「除非提前發生某事，否則等到期限做某事」的邏輯最直接，且執行歷程可觀測、可重試。
  - **狀態機定義方式**：直接用 AWS CDK 的 C# Step Functions L2 constructs（`Wait`、`Choice`、`Task` 等）在 `BackendStack` 程式碼中組裝，不維護獨立的 ASL JSON 檔案——與其餘基礎設施一致用 C# 表達，型別檢查、重構安全性較好。
- **通知派送**：訂單狀態變更等事件透過 EventBridge 自訂 event bus 發布，`Notifications` Lambda 訂閱後依業務規則的管道優先順序（Email 保底、站內必留、LINE 選用加值）分別呼叫 SES/寫入通知資料/呼叫 LINE Messaging API。
  - **事件 schema 組織**：每個 Domain Event 類別對應一個 EventBridge `detail-type`（例如 `OrderPaymentConfirmed`、`RefundApproved`、`TeacherVerificationApproved`），`detail` payload 為該事件的強型別欄位（透過 `StepGo.Domain` 定義的事件類別，經 `StepGo.Application` 的 `IEventPublisher` port、`StepGo.Infrastructure` 的 EventBridge 實作發布，用 source-generated `JsonSerializerContext` 序列化）。`Notifications` Lambda 用 EventBridge rule 的 `detail-type` pattern 訂閱所需事件，新增事件類型不影響既有訂閱規則。

### 6. 檔案儲存：S3（老師身分證照片等私有物件）
獨立的私有 bucket，物件金鑰包含 teacher id，存取一律透過後端產生的短效 presigned URL，僅限 Admin 角色的 API 呼叫可取得，不開放公開讀取。

### 7. 機密管理：Secrets Manager
存放綠界/藍新的 API 金鑰、LINE Messaging API channel secret；Lambda 執行角色僅有讀取自己需要的機密的權限（依 capability 分開的 secret，不共用一份萬能機密）。

### 8. IaC：AWS CDK（C#），與前端 change 共用同一個 CDK app 但不同 stack
延續前端 change 已決定的 CDK 選型；後端新增 `BackendStack`（Lambda、API Gateway、DynamoDB、Cognito、SQS、EventBridge、Step Functions、S3、Secrets）獨立於前端的 `MarketingStack`/`PortalStack`，兩者透過 CDK 的 cross-stack reference 傳遞 API Gateway 端點網址給前端。CI/CD 管線與「如何授權開發者/自動化代理人操作 AWS」的細節（IAM 權限邊界、部署環境分級）留待下一輪決策，尚未定案（見 Open Questions）。

### 9. 前端假設驗證結果
逐一回應 `frontend-mvp-scaffold` design.md 的 Open Questions：
- **認證 token 簽發者** → 已在本文件決策 3 中確定為 Cognito。
- **API 契約細節（欄位命名、分頁、錯誤格式）** → 分頁採 DynamoDB 原生的 `LastEvaluatedKey` 轉換成 opaque cursor 字串回傳給前端（`nextCursor`），不做 offset 分頁；錯誤格式採統一的 `{ code, message }` JSON 結構。這些形狀直接體現在 `StepGo.Contracts` 的 DTO 定義中（決策 1），前端不需要另外維護一份手刻 DTO 或等 OpenAPI 產生 client——直接 project reference `StepGo.Contracts` 即可拿到與後端一致的型別。
- **綠界/藍新付款頁呈現方式** → 採用金流商提供的**導轉外部付款頁**（Redirect），不做 iframe 嵌入，理由：兩大金流商官方 SDK 對 Redirect 模式的文件與範例最完整、風險最低，MVP 不需要為了避免跳轉這種次要體驗優化增加整合複雜度與 PCI-DSS 相關的合規負擔。此決策需回頭更新 `frontend-mvp-scaffold` 的對應 Open Question為「已解答」。

### 10. 未來分析資料湖方向（不在本次 MVP 範圍，先定調不現場建）
使用者已決定 DB 全面採用 DynamoDB，但預期未來會加上數據分析功能，因此先定調方向，避免之後動到資料模型：
- **落地格式選 Apache Iceberg，不是單純 S3 + Hive 分區 Parquet**：這個系統有「事後修正」的業務特性（退款仲裁核准後會回頭調整已撥款訂單的金額、撥款批次可能事後帳務更正），Iceberg 的 `MERGE INTO` 能直接修正歷史快照的某一列，純 append-only 的 Parquet 資料湖難以處理這種情境；另外 Iceberg 的 schema evolution 也能承接 MVP → Phase 2 一路會新增的欄位（系列課、優惠券、電子發票…）而不用重寫歷史資料。技術面上 Kinesis Data Firehose 原生支援直接寫入 Iceberg table、AWS Glue Data Catalog 有 managed compaction，接在既有的 EventBridge/SQS/Lambda serverless 技術棧上不需要多引入一個運算平台（Spark/Flink）。
- **用途邊界（重要）：Iceberg/Athena 只服務 Admin 端跨老師、跨時間的分析與長期歸檔，不作為老師端互動式報表的查詢來源**。老師收入報表、明細清單、撥款紀錄這類面向使用者的畫面，維持由決策 4 的 DynamoDB GSI1 服務（低延遲、即時、單一老師範圍查詢）；改指到 Athena 會因查詢啟動延遲（通常秒級起跳）、串流落地的非同步延遲、以及依掃描量計費的成本模型，讓互動式頁面的體感與成本都變差。
- **例外**：若未來資料量大到不想讓 DynamoDB 無限期保存所有歷史訂單，可用 DynamoDB TTL 把超過保留期限（例如 2 年）的舊訂單移出熱表，這類「很舊」的資料查詢才轉走 Iceberg/Athena 的冷路徑，應用層對這類查詢用不同（較慢、有心理預期）的 UX 呈現——這是資料量大到一定規模才需要評估的優化，MVP 不做。
- **現在唯一需要做的準備**：確保 DynamoDB Streams 開啟，且 `Order`、`PayoutBatch` 等未來必定會拿去分析的實體，屬性命名/型別保持乾淨一致，讓之後接「Streams → Firehose → Iceberg」只是加一段管線，不需要回頭改資料模型。實際建 Iceberg table、Glue Catalog、保留政策、誰能查 Athena，待分析功能真的排進 roadmap 再定案（見 Open Questions）。

## Risks / Trade-offs

- [`AGGREGATE#PLATFORM/SUMMARY` 與 `TEACHER#<teacherId>/SUMMARY#<yyyymm>` 是每次訂單/撥款交易都會寫入的固定 item，屬於 DynamoDB 熱 item] → MVP 流量下可接受；高併發下 `TransactWriteItems` 對同一 item 的競爭會拋出 `TransactionConflictException`，需在 Infrastructure 層的 repository 實作加重試邏輯；若未來量大到成為交易路徑瓶頸，改用 DynamoDB Streams 非同步更新彙總，把彙總維護從付款這條關鍵路徑上移除，犧牲彙總數字的即時性（從交易內同步變成秒級異步）換取吞吐量。
- [DynamoDB 單表設計一旦上線後要新增「未預期的查詢模式」成本較高（通常需要新增 GSI 或做資料遷移）] → 本設計已盤點 MVP 已知的 12 種存取模式；新增查詢需求先評估能否用既有 GSI 覆蓋，真的無法覆蓋才新增 GSI（DynamoDB 可在既有表上新增 GSI，不需整表遷移，但需重新回填既有資料的 GSI 屬性）。
- [Step Functions 為每張退課工單各啟動一次執行，工單量大時執行數量與成本上升] → MVP 規模下退課工單量不高（依業務規則文件，退課本身就是相對少數情境），成本可接受；若未來量大，可改為單一定時 Lambda 掃描 SLA 到期工單，屆時再權衡改動。
- [SQS 緩衝金流通知會讓「付款完成」到「前端看到已付款狀態」多一段非同步延遲] → 延遲通常在秒級，可接受；比起讓 Lambda 直接同步處理 webhook（若 Lambda 當下故障就真的丟單）更穩健。
- [Cognito 雙 User Pool（老師/學生 vs Admin）增加維運復雜度] → 業務規則明確要求 Admin 需要獨立、更嚴格的驗證機制，複雜度為必要成本。
- [改用 .NET 10 Native AOT 取代原先評估的託管執行環境（決策 2 更新）] → 原本「.NET 在 Lambda 上冷啟動高於 Node.js/Python」的風險因改用 Native AOT 大幅緩解（原生執行檔省去 JIT/組件載入時間，冷啟動與 Node.js 級別相近甚至更快），交換成一個新風險：AOT 對反射/動態程式碼的限制較嚴格，`StepGo.Contracts` 與部分 AWS SDK 呼叫路徑需要用 source-generated JSON、避開動態型別，開發時需留意 trim/AOT 分析警告；若未來新增的第三方套件（例如金流商官方 SDK）不支援 AOT，需自行寫薄的相容層或改叫其 REST API。

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
- **CI/CD 管線設計尚未定案**：環境分級（dev/staging/prod）、每個 push/PR 觸發什麼檢查、`cdk deploy` 由誰/什麼機制觸發（人工核准 vs 自動部署）、四個前端部署目標與後端 `BackendStack` 是否共用同一條 pipeline 或分開——這會改變 tasks.md 的驗證方式（目前多數任務寫的是 `dotnet build`/`cdk synth` 這種本地可執行的驗證，CI/CD 定案後可能要補上「PR 檢查通過」「部署到 dev 環境驗證」等驗證方式），需要使用者決定後再回頭補這部分的 design 與 tasks。
- **給開發代理人（Claude Code）的 AWS 操作權限範圍尚未定案**：是否允許直接 `cdk deploy` 到真實 AWS 帳號、用哪個 IAM 角色/權限邊界、是否只給 dev/sandbox 帳號的權限而 staging/prod 一律要人工執行——這是需要使用者明確決定的信任邊界問題，不由本設計自行假設，待決定後補充到本文件的 IaC/CI-CD 決策中。
- **分析資料湖的規模與時程尚未定案**：決策 10 只定調了技術方向（Iceberg、用途邊界），實際要匯出哪些欄位/事件、保留多久、由誰查 Athena、是否要接 QuickSight 等 BI 工具給管理者用，待分析功能真的排進 roadmap 後再回頭定案，目前只要求 `backend-order-checkout`/`backend-payout-batch` 開 DynamoDB Streams 並保持屬性命名乾淨即可。
