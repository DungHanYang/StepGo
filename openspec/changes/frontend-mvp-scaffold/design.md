## Context

專案為全新（greenfield）前端，目前 repo 只有設計交付檔案（`design/design-handoff/`，26 個 `.dc.html` 高保真原型 + 插畫素材）與一份業務規則文件（`金流帳務系統規劃.md`）。設計交付文件第十二節明確建議：老師後台、學生後台、管理者後台為三個獨立應用，共用 UI 元件庫，並以 monorepo（Turborepo/Nx）管理；管理者後台建議獨立網域、IP 白名單、強制雙因素驗證。設計代幣（色彩、字體、造型）與響應式斷點已在設計交付 README 中定案為最終版本。後端 API 契約尚未存在（將於後續 change 規劃），因此本設計需明確界定前端在等待後端期間如何獨立開發與驗證。

## Goals / Non-Goals

**Goals:**
- 決定 monorepo 工具、應用拆分方式、前端框架、樣式方案、資料請求/狀態管理慣例，讓 tasks.md 的每個任務都有明確、不含糊的技術落點。
- 讓四個應用（marketing / teacher-portal / student-portal / admin-panel）從第一天就共用同一套設計代幣與元件，避免視覺或狀態語意飄移。
- 定義 Mock-first 開發策略，使前端可以在後端 change 完成前就把 specs 中的狀態機（付款 5 狀態、驗證 4 狀態、撥款帳戶 4 狀態、退課工單流程）做出可互動、可測試的介面。

**Non-Goals:**
- 不決定後端框架、資料庫、金流串接（綠界/藍新 API）實作方式——留給後端 change。
- 不產出正式 API 契約（OpenAPI/GraphQL schema）——前端先用型別化的 fetch 包裝層與假資料頂著，待後端契約確定後再置換。
- 不涵蓋 Phase 2 功能（系列課、候補、評價、Rich Menu、廣告錢包、分級權限、監控儀表板），依 proposal.md 的範圍界定排除。

## Decisions

1. **Monorepo 工具：pnpm workspaces + Turborepo。**
   理由：設計文件本身建議 Turborepo/Nx；團隊規模小、應用數量少（4 個 app + 2 個共用 package），Turborepo 設定更輕、學習成本低於 Nx 的 plugin/generator 體系。
   替代方案：Nx——功能更完整（generator、dependency graph 視覺化），但對目前規模是過度工程，故不採用。

2. **應用拆分：`apps/marketing`、`apps/teacher-portal`、`apps/student-portal`、`apps/admin-panel`，各自獨立部署。**
   理由：業務規則明確要求管理者後台需獨立網域、IP 白名單、強制雙因素驗證——在部署層級（獨立的 hosting project/子網域）落實比在單一應用內用 middleware 動態切換更簡單可靠，也降低單一應用的 bundle 體積與受眾／風險範圍（blast radius）。
   替代方案：單一 Next.js app 以 route group 區分（`(marketing)`、`(teacher)`、`(student)`、`(admin)`）——排除，因為 admin 的獨立網域/IP 白名單需求會讓單體部署變複雜，且四種受眾的權限模型混在一個 app 中容易出錯。

3. **前端框架：Next.js（App Router）+ TypeScript，四個應用一致採用。**
   理由：`marketing` 需要 SEO/SSR（首頁、課程列表為主要導流入口）；三個後台應用雖不需要 SEO，但採同一框架可共用 ESLint/TS config、共用 `packages/ui`、共用建置管線，降低多套工具鏈的維護成本；Admin 可用 Next.js middleware 實作 IP 白名單檢查點（實際名單來源仍由後端提供）。
   替代方案：後台三個應用用 Vite + React SPA——排除，因為會在同一 monorepo 內維護兩套建置工具鏈，增加無實質收益的複雜度。

4. **樣式：Tailwind CSS，設計代幣寫死在 `packages/ui/tailwind-preset` 供四個應用 extend。**
   色彩／字體／造型（1px 邊框、3–4px 圓角、無陰影漸層）依設計交付 README 逐一對應成 Tailwind theme token，不允許在應用層寫入色碼字面值。字型：標題 Noto Serif TC、內文 Noto Sans TC、數字 EB Garamond，透過 next/font 載入並設為 CSS variable。

5. **共用元件庫：`packages/ui`，涵蓋 Button、Card、Table、StatusTag、StepIndicator（多步驟精靈用）、OwlTip（貓頭鷹提示框）、Wizard 容器。**
   四個應用的表格、篩選器、狀態標籤共用同一套語意（例如付款狀態的顏色定義只在一處維護），對應設計文件「UI 元件層」的要求。

6. **資料請求/伺服器狀態：TanStack Query，搭配集中式的型別化 fetch 包裝層（`packages/api-client`）。**
   目前沒有 OpenAPI schema，故先手刻符合 specs 定義欄位的 TypeScript 介面（例如 `Order`、`RefundTicket`、`PayoutBatch`）；待後端 change 產出契約後，可將包裝層置換為由 schema 產生的 client，呼叫端（React 元件/hooks）不需大改。

7. **表單：React Hook Form + Zod。**
   Zod schema 直接編碼可在前端獨立驗證的業務規則（例如：手機必填、Email 選填、退費比例不得低於底線、老師核准退款金額不得超過原始繳費金額），在使用者離開欄位或送出時即時回饋，不等後端往返。

8. **認證：前端假設 httpOnly cookie session + `/me` bootstrap endpoint，登入頁不分角色、登入成功後由角色欄位導向對應後台。**
   實際的 session/token 機制由後端 change 決定；`frontend-auth` capability 只規範前端可觀察的行為（角色導向、表單驗證），不假設任何特定認證協定的實作細節。

9. **Mock-first 開發：使用 MSW（Mock Service Worker）依 specs 中定義的狀態機建立假資料情境（例如可切換「審核中／未通過／通過」三種身分驗證假資料，直接對應四個 UI 狀態）。**
   讓 teacher-portal / student-portal / admin-panel 的狀態機頁面可以在沒有後端的情況下開發與展示，且 mock 情境即為未來與真實後端做整合驗證的檢查清單。

10. **響應式斷點：系統頁（含側邊選單）1080px、980px；行銷頁 900px、640px，寫成 Tailwind 自訂 breakpoint（`system-lg`/`system-md`、`marketing-lg`/`marketing-md`），四應用共用同一組常數。**
    明確記錄設計文件提到的已知坑：側邊選單容器不得用 inline style 設定 `flex-direction`，避免媒體查詢被行內樣式蓋掉導致窄螢幕無法收合。

11. **多語系：MVP 僅支援繁體中文（zh-TW），但文案抽到獨立的 content/constants 檔案而非寫死在 JSX 中，為未來可能的多語系留一個低成本的擴充點，不引入完整 i18n 函式庫（過度工程）。**

## Risks / Trade-offs

- [四個獨立 Next.js 應用增加建置/部署管線數量] → 以 Turborepo 共用 `tsconfig`、`eslint-config`、CI cache 降低重複設定與建置時間成本。
- [Mock-first 開發可能與真實後端行為產生落差（例如錯誤碼、分頁格式）] → specs 中定義的欄位與狀態機為後端 change 必須滿足的契約來源；整合階段以 tasks.md 最後一組「Mock→真實 API 切換」任務逐一比對驗證。
- [Admin 獨立網域/IP 白名單提高部署複雜度] → 業務規則明確要求此安全性層級，複雜度為必要成本而非過度設計。
- [即時試算（費用試算工具、課程建立精靈）以前端固定費率公式計算，若後端費率設定變動未同步更新前端公式常數，會產生試算與實際扣款不一致] → 費率常數集中放在 `packages/ui`（或獨立的 `packages/pricing-rules`）單一檔案維護，並在 tasks.md 中標記為需與 Admin 費用設定 API 對齊的整合點。

## Migration Plan

無現有系統需遷移（greenfield）。建置順序如下（詳見 tasks.md）：
1. Monorepo 與工具鏈 scaffold
2. `packages/ui` 設計系統
3. `apps/marketing`
4. `frontend-auth`（登入註冊，供後台導向使用）
5. `apps/teacher-portal`
6. `apps/student-portal`
7. `apps/admin-panel`
8. Mock API 圖層 + 跨應用響應式/整合驗收

## Open Questions

- 後端最終 API 契約（欄位命名、分頁、錯誤格式）——由後續後端 change 決定；不影響本次前端 specs/tasks 的範圍或狀態機定義，届時只需置換 `packages/api-client` 的實作。
- 綠界/藍新的付款頁呈現方式（導轉外部頁面 vs. iframe 嵌入）——由後端/金流串接 change 決定；`frontend-student-portal` 的 Checkout 規格只定義前端可觀察的狀態機（待付款/付款中/已付款/待撥款相關唯讀狀態），不預設特定金流商的頁面嵌入方式。
