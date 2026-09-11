## Context

專案為全新（greenfield）前端，目前 repo 只有設計交付檔案（`design/design-handoff/`，26 個 `.dc.html` 高保真原型 + 插畫素材）與一份業務規則文件（`金流帳務系統規劃.md`）。設計交付文件第十二節明確建議：老師後台、學生後台、管理者後台為三個獨立應用，共用 UI 元件庫；管理者後台建議獨立網域、IP 白名單、強制雙因素驗證。設計代幣（色彩、字體、造型）與響應式斷點已在設計交付 README 中定案為最終版本。

技術棧已由使用者定案：**全 serverless 部署在 AWS，後端 .NET 跑在 API Gateway + Lambda（DynamoDB 為資料層，由後端 change 規劃），前端使用 Blazor（開發者較熟悉此技術）**。本設計取代前一版採用 Next.js/React/Turborepo 的方案。前端框架選擇必須同時滿足兩個互相拉扯的需求：(1) 行銷網站（首頁、課程列表）需要良好 SEO，(2) 整體部署目標是全 serverless、不維護常駐伺服器。

## Goals / Non-Goals

**Goals:**
- 決定 Blazor 的 hosting model、專案拆分方式、樣式方案、資料請求慣例，讓 tasks.md 的每個任務都有明確、不含糊的技術落點。
- 在「行銷頁需要 SEO」與「全 serverless（無常駐伺服器）」兩個限制下選出可行方案，並記錄取捨。
- 讓四個應用（marketing / teacher-portal / student-portal / admin-panel）從第一天就共用同一套設計代幣與元件，避免視覺或狀態語意飄移。
- 定義 Mock-first 開發策略，使前端可以在後端 change 完成前就把 specs 中的狀態機（付款 5 狀態、驗證 4 狀態、撥款帳戶 4 狀態、退課工單流程）做出可互動、可測試的介面。

**Non-Goals:**
- 不決定後端資料庫的存取模式設計（DynamoDB table/GSI 設計）——留給後端 change。
- 不產出正式 API 契約（OpenAPI schema）——前端先用型別化的 HTTP client 包裝層與假資料頂著，待後端契約確定後再置換。
- 不涵蓋 Phase 2 功能（系列課、候補、評價、Rich Menu、廣告錢包、分級權限、監控儀表板），依 proposal.md 的範圍界定排除。
- 不決定 AWS IaC 工具的細節模板（CDK stack 的具體程式碼）——本設計只定架構層級的服務選型，具體 CDK 程式碼留給 tasks.md 的實作任務與後端 change 協調。

## Decisions

1. **前端框架：.NET 8 Blazor，但兩種 hosting model 依受眾分流。**
   - **`apps/marketing`（行銷網站）：Blazor Web App，以 Static Server-Side Rendering 為主要渲染模式。** 每個頁面在伺服器端（Lambda）算出完整 HTML 後回傳，不需要 SignalR 常駐連線，因此可以跑在 Lambda 的請求/回應模型上，同時解決 SEO（爬蟲抓到的是完整 HTML，不是空殼）。頁面內僅有的少量互動元件（例如課程詳情頁的「立即報名」按鈕）可個別標記為 `InteractiveWebAssembly` render mode，做成局部 island，不影響整頁的 SSR 特性。
   - **`apps/teacher-portal`、`apps/student-portal`、`apps/admin-panel`（三個後台）：Blazor WebAssembly standalone（純 client-side）。** 這三個應用完全在登入之後才使用，沒有 SEO 需求，適合編譯成純靜態檔案（wasm + dll），部署到 S3 + CloudFront，完全沒有伺服器常駐，是最單純的 serverless 型態。
   - 替代方案：全部四個應用都用 Blazor WebAssembly standalone——排除，因為行銷頁的 SEO 需求無法被純 client-side render 滿足（爬蟲看到空殼 HTML），會直接影響「課程列表為主要導流入口」的商業目標。
   - 替代方案：全部四個應用都用 Blazor Web App（Interactive Server render mode）——排除，因為 Interactive Server 需要每個使用者維持一條 SignalR 長連線，這與 Lambda「無狀態、用完即丟」的執行模型互斥，若要支撑需接 API Gateway WebSocket + 常駐運算（ECS/Fargate），違反全 serverless 的目標。

2. **應用拆分維持四個獨立專案，各自獨立部署：`apps/marketing`、`apps/teacher-portal`、`apps/student-portal`、`apps/admin-panel`。**
   理由不變：業務規則明確要求管理者後台需獨立網域、IP 白名單（可用 CloudFront 前的 AWS WAF IP set 落實）、強制雙因素驗證（Cognito 獨立 User Pool）；四個應用的受眾、權限模型、甚至 hosting model（marketing 是 Lambda 動態渲染，其他三個是純靜態）本質不同，獨立部署可降低單一應用的風險範圍（blast radius）。

3. **樣式：Tailwind CSS（透過獨立的 CLI 建置步驟產生 CSS，不依賴任何 JS UI 框架），設計代幣集中在共用的樣式模組供四個應用引用。**
   Blazor 元件的 class 屬性可以直接套用 Tailwind 產生的 utility class，不需要 React／JS 生態系；色彩／字體／造型（1px 邊框、3–4px 圓角、無陰影漸層）依設計交付 README 逐一對應成 Tailwind theme token。字型：標題 Noto Serif TC、內文 Noto Sans TC、數字 EB Garamond，以自架字型檔（或 Google Fonts CDN）載入。

4. **共用元件庫：以 Razor Class Library（RCL）專案 `StepGo.UI` 實作，涵蓋 Button、Card、Table、StatusTag、StepIndicator、OwlTip、Wizard 等元件，四個應用皆參照此專案。**
   對應設計文件「UI 元件層」的要求；四個應用的表格、篩選器、狀態標籤共用同一套語意（例如付款狀態的顏色定義只在一處維護）。

5. **資料請求：型別化的 `HttpClient` 包裝層（`StepGo.ApiClient` 專案），以 `System.Net.Http.Json` 呼叫後端 API Gateway 端點。**
   目前沒有 OpenAPI schema，故先手刻符合 specs 定義欄位的 C# record/DTO（例如 `Order`、`RefundTicket`、`PayoutBatch`）；待後端 change 產出契約後，可將包裝層置換為由 schema 產生的 client（例如 NSwag/Kiota 產生的 client），呼叫端元件不需大改。伺服器狀態快取以 Blazor 內建的 `CascadingState`/簡單記憶體快取處理，MVP 階段不需要引入額外的狀態管理套件。

6. **表單：Blazor `EditForm` + `DataAnnotations`（或 FluentValidation，若規則複雜度超出 DataAnnotations 表達能力）。**
   驗證規則直接編碼可在前端獨立驗證的業務規則（例如：手機必填、Email 選填、退費比例不得低於底線、老師核准退款金額不得超過原始繳費金額），在欄位失焦或送出時即時回饋，不等後端往返。

7. **認證：前端假設後端簽發 JWT（由 Cognito 或後端自行簽發，交由後端 change 決定），Blazor 端以 `AuthenticationStateProvider` 包裝 token 存取與角色判斷；登入頁不分角色、登入成功後由 token 內的角色 claim 導向對應後台（teacher-portal / student-portal）。管理者後台使用獨立的登入流程與獨立 token（對應獨立 Cognito User Pool + 強制 MFA）。**
   實際的 token 簽發/刷新機制由後端 change 決定；`frontend-auth` capability 只規範前端可觀察的行為（角色導向、表單驗證），不假設特定認證協定的實作細節。

8. **Mock-first 開發：以一個輕量的本地 Mock API 專案（ASP.NET Core Minimal API，或 WireMock.Net）依 specs 中定義的狀態機提供假資料情境（例如可切換「審核中／未通過／通過」三種身分驗證假資料，直接對應四個 UI 狀態），透過設定切換 `StepGo.ApiClient` 的 base URL 指向此 mock 服務。**
   讓 teacher-portal / student-portal / admin-panel 的狀態機頁面可以在沒有後端 Lambda 的情況下開發與展示，且 mock 情境即為未來與真實後端做整合驗證的檢查清單。

9. **響應式斷點：系統頁（含側邊選單）1080px、980px；行銷頁 900px、640px，寫成共用的 CSS 變數/Tailwind 自訂 breakpoint，四應用共用同一組常數。**
   明確記錄設計文件提到的已知坑：側邊選單容器不得用 inline style 設定 `flex-direction`，避免媒體查詢被行內樣式蓋掉導致窄螢幕無法收合。

10. **多語系：MVP 僅支援繁體中文（zh-TW），但文案抽到獨立的 resource 檔案（`.resx` 或集中的 constants 類別）而非寫死在 Razor 元件中，為未來可能的多語系留一個低成本的擴充點，不引入完整 i18n 框架（過度工程）。**

11. **AWS 部署拓樸：**
    ```
    Route 53（網域）→ CloudFront（CDN + TLS）
        ├─ marketing.stepgo.tw   → API Gateway → Lambda（StepGo.Marketing，Blazor Web App SSR）
        ├─ teach.stepgo.tw       → S3（StepGo.TeacherPortal 靜態 wasm/dll）
        ├─ learn.stepgo.tw       → S3（StepGo.StudentPortal 靜態 wasm/dll）
        └─ admin.stepgo.tw       → S3（StepGo.AdminPanel 靜態 wasm/dll）+ AWS WAF IP allow list
    ```
    四個 CloudFront distribution 對應四個子網域，`admin.*` 額外掛 WAF IP set；API 請求（teacher/student/admin 呼叫後端資料 API）另外打 API Gateway，不經過上述靜態/SSR 路徑。marketing 的 SSR 頁面可在 CloudFront 設短 TTL cache（例如課程列表 60 秒），降低 Lambda 呼叫次數並加速爬蟲抓取。

## Risks / Trade-offs

- [Blazor Web App 的 Static SSR + 局部 WebAssembly island 是 .NET 8 才穩定的功能，案例與社群資源比 Next.js/React 少] → 風險可接受：業務規則要求的頁面互動並不複雜（多為表單、清單、狀態展示），且維持單一語言（C#）貫穿前後端可共用 DTO，換取的維護成本下降大於功能風險。
- [四個獨立應用 + 兩種 hosting model（Lambda SSR / 純靜態 WASM）增加部署管線種類] → 以共用的 CDK 專案統一管理四個部署目標，並在 tasks.md 中把「四個 app 各自的部署腳本」列為明確任務，避免臨時拼裝。
- [Mock-first 開發可能與真實後端行為產生落差（例如錯誤碼、分頁格式）] → specs 中定義的欄位與狀態機為後端 change 必須滿足的契約來源；整合階段以 tasks.md 最後一組「Mock→真實 API 切換」任務逐一比對驗證。
- [Admin 獨立網域/IP 白名單/獨立 Cognito Pool 提高部署複雜度] → 業務規則明確要求此安全性層級，複雜度為必要成本而非過度設計。
- [即時試算（費用試算工具、課程建立精靈）以前端固定費率公式計算，若後端費率設定變動未同步更新前端公式常數，會產生試算與實際扣款不一致] → 費率常數集中放在 `StepGo.UI`（或獨立的 `StepGo.PricingRules`）單一專案維護，並在 tasks.md 中標記為需與 Admin 費用設定 API 對齊的整合點。
- [`marketing` 應用的 SSR 頁面跑在 Lambda，冷啟動延遲可能影響首次載入體感速度] → 可用 Lambda 的 Provisioned Concurrency（若流量與預算允許）或先接受 MVP 階段的冷啟動延遲，待有真實流量數據再決定是否加購。

## Migration Plan

無現有系統需遷移（greenfield）。建置順序如下（詳見 tasks.md）：
1. .NET 解決方案與專案骨架（`StepGo.UI`、`StepGo.ApiClient`、四個 app 專案）
2. `StepGo.UI` 設計系統
3. `apps/marketing`（Blazor Web App，Static SSR）
4. `frontend-auth`（登入註冊，供後台導向使用）
5. `apps/teacher-portal`（Blazor WASM standalone）
6. `apps/student-portal`（Blazor WASM standalone）
7. `apps/admin-panel`（Blazor WASM standalone）
8. Mock API 圖層 + 跨應用響應式/整合驗收 + AWS 部署腳本（CDK）

## Open Questions

- 後端最終 API 契約（欄位命名、分頁、錯誤格式）——由後續後端 change 決定；不影響本次前端 specs/tasks 的範圍或狀態機定義，届時只需置換 `StepGo.ApiClient` 的實作。
- 綠界/藍新的付款頁呈現方式（導轉外部頁面 vs. iframe 嵌入）——由後端/金流串接 change 決定；`frontend-student-portal` 的 Checkout 規格只定義前端可觀察的狀態機（待付款/付款中/已付款/待撥款相關唯讀狀態），不預設特定金流商的頁面嵌入方式。
- 認證 token 的簽發者（Cognito Hosted UI/Amazon Cognito SDK 直接整合 vs. 後端自行簽發 JWT）——由後端 change 決定；前端 `frontend-auth` 僅假設「登入後可取得含角色 claim 的 token」，不預設簽發來源。
