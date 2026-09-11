## Why

Design 團隊已交付 26 個高保真畫面（`design/design-handoff/`）與完整業務規則（付款方式、四層費用結構、退費底線、撥款流程、退課仲裁等），但專案目前沒有任何前端程式碼、技術棧決策或可執行的實作任務。若直接照 HTML 設計稿一頁一頁刻，容易在跨頁共用的狀態機（付款 5 狀態、驗證 4 狀態、撥款帳戶 4 狀態、退課工單多階段流程）上出現不一致或遺漏business rule。需要先把設計稿與業務規則轉譯成前端的行為契約（specs）與架構決策（design），再據此排出可執行的任務清單，讓實作可以直接開工而不必反覆回頭重讀設計稿。

## What Changes

- 建立前端 monorepo 架構決策（apps 拆分、技術棧、共用元件庫、狀態管理、Mock-first 開發策略），對應設計交付文件第十二節「系統架構總覽」的建議。
- 依 26 個設計畫面與業務規則文件，定義六個前端能力（capability）的行為契約：共用設計系統、對外行銷網站、登入註冊、老師後台、學生後台、管理者後台。
- 排定 MVP 範圍：僅涵蓋設計交付文件第十四節「MVP 範圍界定」中標記 ✅ 的功能；系列課、候補、評價、Rich Menu、老師端 LINE 通知、廣告錢包、分級權限、系統監控儀表板等 Phase 2 功能不在本次規劃內。
- 本次僅規劃「前端」；後端 API、金流串接、資料庫設計為後續獨立的 change（本次 design.md 會標記前端對後端契約的假設，供後續驗證）。

## Capabilities

### New Capabilities
- `frontend-design-system`：跨應用共用的設計代幣（色彩/字體/造型）與核心 UI 元件庫。
- `frontend-marketing-site`：對外行銷網站（首頁、課程列表/詳情、關於、條款、費用試算、老師/學生招募頁）。
- `frontend-auth`：老師與學生共用的登入/註冊入口與角色導向。
- `frontend-teacher-portal`：老師後台（總覽、課程建立精靈、身分驗證、撥款帳戶、退課審核、學生名單與報表）。
- `frontend-student-portal`：學生後台（我的課程、報名付款流程、課程進行中頁、退課留言串、404 頁）。
- `frontend-admin-panel`：平台管理者後台（金流總覽、費用設定、老師與課程目錄、帳號權限、操作日誌、平台仲裁）。

### Modified Capabilities
（無，此為全新專案，尚無既有 spec。）

## Impact

- 新增程式碼：`apps/marketing`、`apps/teacher-portal`、`apps/student-portal`、`apps/admin-panel`、`packages/ui`、`packages/config` 等 monorepo 結構（實際建立於後續 apply 階段，本 change 僅規劃）。
- 不影響任何既有程式碼（目前 repo 內除設計交付檔案與業務規則文件外沒有其他程式碼）。
- 依賴：真實後端 API 尚未定義，前端開發將採 Mock-first（本地假資料/MSW），待後端 change 完成後再串接；付款方式（綠界/藍新）的實際串接細節不在本次前端規劃內，Checkout 流程僅規劃其前端可觀察的狀態機。
- 影響未來的後端規劃：本 change 的 design.md 會列出前端對 API 的假設（如角色導向登入、訂單狀態唯讀、費用試算為前端純計算），供後端 change 對齊或提出修正。
