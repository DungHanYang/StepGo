## 1. .NET 解決方案與專案骨架

- [x] 1.1 建立 .NET solution 與四個應用專案骨架：`apps/marketing`（Blazor Web App）、`apps/teacher-portal`、`apps/student-portal`、`apps/admin-panel`（皆為 Blazor WebAssembly standalone），驗證方式：`dotnet build` 全部專案成功，四個 app 各自 `dotnet run` 可在不同 port 啟動並顯示預設頁面
- [x] 1.2 建立共用專案 `StepGo.UI`（Razor Class Library）、`StepGo.ApiClient`（型別化 HttpClient 包裝層骨架）、`StepGo.PricingRules`（費率常數與試算公式），並在四個 app 中以 project reference 引用，驗證方式：任一 app 參照 `StepGo.UI` 的元件可正確編譯與渲染
- [x] 1.3 設定共用 CI pipeline（`dotnet format` 檢查、`dotnet build`、`dotnet test`），驗證方式：CI 設定在本地以對應指令全數通過
- [x] 1.4 建立 AWS CDK（C#）專案骨架，定義四個部署目標的 stack 占位（marketing 用 Lambda+API Gateway、三個後台用 S3+CloudFront），驗證方式：`cdk synth` 成功產出 CloudFormation 樣板（可先為空的資源占位）

## 2. 設計系統（StepGo.UI）

- [x] 2.1 依設計交付 README 建立 Tailwind 設定與建置腳本（色彩、Noto Serif TC/Noto Sans TC/EB Garamond 字體、1px 邊框、3–4px 圓角、無陰影），輸出 CSS 供四個 Blazor app 引用，驗證方式：四個 app 套用產出的 CSS 後可正確渲染代幣色彩
- [x] 2.2 定義系統頁（1080px/980px）與行銷頁（900px/640px）響應式斷點常數，驗證方式：以一個測試頁面在三種視窗寬度下手動驗證版面切換
- [x] 2.3 實作 Button、Card、Table、StatusTag、StepIndicator、OwlTip、Wizard 等 Razor 元件，驗證方式：每個元件搭配 bUnit 撰寫 render 測試（元件成功渲染、必要 aria 屬性存在），並在一個元件展示頁列出所有互動範例
- [x] 2.4 定義 `StatusTag` 的狀態語意對照（付款狀態、驗證狀態、撥款帳戶狀態、退課工單狀態）為共用的 enum/型別，驗證方式：四個 app 皆從 `StepGo.UI` 引用同一組型別，不各自定義重複的狀態列舉

## 3. `apps/marketing`（frontend-marketing-site，Blazor Web App / Static SSR）

- [ ] 3.1 實作首頁（雙入口 CTA、費用試算摘要區塊、正在招生課程清單），驗證方式：bUnit/整合測試涵蓋兩個入口的導向行為，且確認頁面預設為 Static SSR render mode（無需 JS 即可看到完整內容）
- [ ] 3.2 實作課程列表頁（類別篩選、關鍵字搜尋、空狀態）與課程詳情頁，驗證方式：搜尋無結果情境有對應測試斷言空狀態元件被渲染；課程詳情頁的「立即報名」互動按鈕標記為 `InteractiveWebAssembly` island 且不影響其餘內容的 SSR
- [ ] 3.3 實作費用試算工具頁，呼叫 `StepGo.PricingRules` 的固定費率公式計算（信用卡 2.89%、ATM 每筆 NT$15、平台服務費 10%、撥款轉帳費同行/跨行），驗證方式：unit test 覆蓋至少 3 組輸入（純 ATM、純信用卡、切換方式）驗證計算結果與明細顯示正確，且測試確認過程中無網路請求
- [ ] 3.4 實作 About、Terms（老師條款/學生退費雙分頁）、For Teachers、For Students 靜態頁，驗證方式：Terms 頁分頁切換有對應互動測試
- [ ] 3.5 設定 CloudFront 對行銷頁的快取策略（例如課程列表頁短 TTL），驗證方式：CDK stack 中的 CloudFront 快取行為設定可在本地 `cdk synth` 產出的樣板中檢視到對應 cache policy

## 4. `frontend-auth`（登入註冊，供後台導向使用）

- [ ] 4.1 實作共用登入頁與角色分流註冊入口，驗證方式：測試涵蓋老師登入導向老師後台、學生登入導向學生會員中心兩個情境
- [ ] 4.2 實作學生註冊表單（手機必填、Email 選填+提示文案）與對應 `DataAnnotations`/FluentValidation 驗證規則，驗證方式：unit test 覆蓋手機空白擋下送出、Email 空白仍可送出兩種情境
- [ ] 4.3 實作表單即時驗證回饋（欄位失焦觸發、送出觸發），驗證方式：測試涵蓋 Email 格式錯誤即時顯示錯誤訊息且不觸發任何網路請求
- [ ] 4.4 實作 `AuthenticationStateProvider`，從假設的 JWT 中解析角色 claim 並驅動導向邏輯，驗證方式：unit test 覆蓋含不同角色 claim 的假 token 導向至正確後台

## 5. `apps/teacher-portal`（frontend-teacher-portal，Blazor WASM standalone）

- [ ] 5.1 實作後台總覽頁（待辦優先清單含 SLA 倒數排序、本期收入總覽、下次撥款卡片），驗證方式：unit test 驗證 SLA 剩餘時間排序邏輯（最急迫排最前）
- [ ] 5.2 實作課程建立四步驟精靈（基本資料/定價與付款/退費規則/招生頁）與步驟間資料保留，驗證方式：測試涵蓋跨步驟資料不遺失、至少勾選一種付款方式才能前進兩種情境
- [ ] 5.3 實作「定價與付款」步驟的即時試算區塊（呼叫 `StepGo.PricingRules`，含金流成本/平台服務費/撥款轉帳費明細展開），驗證方式：unit test 驗證售價與付款方式變動時試算結果即時更新且不呼叫 API
- [ ] 5.4 實作退費規則設定步驟（平台底線 + 老師微調限制），驗證方式：測試涵蓋低於底線的比例被擋下、高於底線的比例可通過兩種情境
- [ ] 5.5 實作送出審核等待畫面與空狀態（尚無課程時的老師後台「我的課程」空狀態），驗證方式：對應頁面有渲染測試
- [ ] 5.6 實作身分驗證頁四狀態（填寫/審核中/未通過/通過）與課程建立入口的狀態守衛，驗證方式：測試涵蓋非「已認證」狀態時課程建立入口被導向驗證頁
- [ ] 5.7 實作撥款帳戶設定頁四狀態（檢視/變更/核對中/銀行退回）與戶名一致性提示文案，驗證方式：銀行退回狀態顯示退回原因與重送入口有對應渲染測試
- [ ] 5.8 實作退課審核頁（核准/駁回、金額微調上限、理由必填、SLA 倒數顯示），驗證方式：測試涵蓋微調金額超過原始繳費金額被擋下、微調未填理由被擋下兩種情境
- [ ] 5.9 實作學生名單、繳費狀態查看、日期區間篩選明細清單，驗證方式：測試涵蓋日期區間篩選正確過濾結果
- [ ] 5.10 實作 CSV/Excel 免費匯出功能，驗證方式：測試驗證匯出動作不顯示任何付費提示且產出檔案內容欄位與明細清單一致

## 6. `apps/student-portal`（frontend-student-portal，Blazor WASM standalone）

- [ ] 6.1 實作會員中心「我的課程」四分頁（依付款狀態與場次時間篩選，含空狀態），驗證方式：測試涵蓋四個分頁的篩選邏輯與已退款訂單正確歸類
- [ ] 6.2 實作報名付款三步驟流程，付款選項依課程設定動態顯示，驗證方式：測試涵蓋僅開放單一付款方式時另一選項不顯示
- [ ] 6.3 實作兩種付款結果頁（信用卡即時成功、ATM 待繳費含虛擬帳號資訊），且不提供自我申報按鈕，驗證方式：測試斷言 ATM 結果頁渲染結果中不存在任何「已完成付款」自我申報操作元件
- [ ] 6.4 實作課程進行中頁（進度、下一堂、繳費/退費快照），驗證方式：對應頁面渲染測試涵蓋下一堂資訊顯示
- [ ] 6.5 實作單筆訂單頁（付款證明可下載/列印）與「申請退課」入口，驗證方式：測試涵蓋送出退課申請後導向 Message Thread 頁
- [ ] 6.6 實作 Message Thread 頁（退課專用正式留言串），並在一般問題入口改為導引 LINE 官方帳號而非站內留言，驗證方式：測試涵蓋一般問題入口不產生留言串
- [ ] 6.7 實作 Order Not Found 404 頁與返回導引，驗證方式：測試涵蓋不存在訂單編號情境顯示此頁面

## 7. `apps/admin-panel`（frontend-admin-panel，Blazor WASM standalone）

- [ ] 7.1 建立獨立於老師/學生的側邊選單版面與獨立登入頁（含雙因素驗證步驟 UI、IP 白名單提示占位），驗證方式：測試確認管理者登入路由與 `frontend-auth` 登入路由完全分離
- [ ] 7.2 實作金流總覽頁（營收統計、資金核對、撥款佇列、退款總覽），驗證方式：頁面渲染測試涵蓋撥款佇列清單顯示
- [ ] 7.3 實作費用設定頁（服務費率/轉帳與退款處理費/退費底線調整）與 30 天預告生效檢核，驗證方式：測試涵蓋生效日小於 30 天被擋下、變更寫入變更紀錄兩種情境
- [ ] 7.4 實作老師與課程合併目錄頁（搜尋、依驗證狀態/課程狀態篩選），驗證方式：測試涵蓋依驗證狀態篩選結果正確
- [ ] 7.5 實作帳號與權限頁（MVP 單一角色呈現，權限矩陣版面預留擴充欄位），驗證方式：測試斷言角色選單僅顯示單一可指派角色
- [ ] 7.6 實作操作日誌頁（依費率/驗證/仲裁/權限/撥款類別篩選），驗證方式：測試涵蓋依類別篩選結果正確
- [ ] 7.7 實作平台仲裁頁（案件佇列、雙方留言與證據記錄、裁決操作含理由必填），驗證方式：測試涵蓋未填理由被擋下、裁決送出後案件從佇列移除兩種情境

## 8. Mock API 圖層、AWS 部署與跨應用整合驗收

- [ ] 8.1 建立本地 Mock API 專案（ASP.NET Core Minimal API 或 WireMock.Net），涵蓋四個核心狀態機（付款 5 狀態、身分驗證 4 狀態、撥款帳戶 4 狀態、退課工單多階段流程）的假資料情境集，驗證方式：每個狀態機至少各狀態有一組可切換的 mock 情境，並在對應頁面測試中驗證能正確渲染每個狀態
- [ ] 8.2 將 `StepGo.ApiClient` 的 DTO 對齊 specs 中列出的欄位（`Order`、`RefundTicket`、`PayoutBatch`、`Teacher` 等），驗證方式：編譯通過且 mock API 回應符合宣告的 DTO 結構
- [ ] 8.3 完成 CDK stack：`apps/marketing` 部署到 Lambda + API Gateway，三個後台部署到各自的 S3 + CloudFront，`admin` 額外設定 AWS WAF IP allow list，驗證方式：`cdk deploy`（或 `cdk synth` 若無實際 AWS 帳號可測）成功產出四個部署目標的資源定義，且 admin 的樣板中可見 WAF 關聯
- [ ] 8.4 執行四個應用在系統頁/行銷頁兩組響應式斷點下的手動 QA（含側邊選單收合已知坑驗證），驗證方式：於 1080px/980px（系統頁）與 900px/640px（行銷頁）四個關鍵寬度下逐頁截圖比對，確認無版面破版與側邊選單收合正常
- [ ] 8.5 驗證 `apps/marketing` 的 SSR 頁面在未啟用 JavaScript/WebAssembly 的情況下仍能顯示完整內容（SEO 檢核），驗證方式：以停用 JS 的瀏覽器或簡易 HTTP 請求檢視首頁與課程列表頁的原始回應 HTML，確認關鍵內容（課程名稱、售價）存在於初始 HTML 中
- [ ] 8.6 整理「前端對後端 API 的假設清單」（角色導向登入、訂單狀態唯讀、費用試算為前端計算、認證 token 含角色 claim、費率常數需與 Admin 設定同步等），作為後續後端 change 的輸入，驗證方式：清單以文件形式產出並列出每項假設對應的 spec 需求編號
