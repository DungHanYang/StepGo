# Handoff: 學步 StepGo 平台設計（v2）

## Overview
學步（StepGo）是老師開課、學生上課的平台，核心是金流與費用機制透明化。本包含對外行銷頁、老師/學生系統、平台管理者（Admin）後台、行動裝置稿，共 26 個畫面。

## About the Design Files
本資料夾內的 `.dc.html` 檔案是**設計參考稿（HTML 原型）**，用來展示畫面外觀、資訊架構與互動行為，不是要直接搬上 production 的程式碼。請在你們的技術棧（前端框架 + 後端 API）中，依現有元件庫慣例**重新實作**這些畫面；若尚無既定框架，再依需求選擇合適框架。

## Fidelity
**High-fidelity**：色彩、字體、間距、文案均為最終版本；狀態機（如審核 4 階段、付款 5 狀態）都在檔案內以分頁/狀態切換完整呈現，可直接當狀態圖使用。

## Design Tokens

**色彩**
- 底色 `#f7f4ee`　卡片 `#fdfbf6`　邊框/分隔線 `#ded7c9`　深墨（主文字）`#22201c`
- 次要文字 `#7c7668`（對比需 ≥4.5:1；一般用 `#4a463d` 或 `#6c6659`）
- 老師端主色（赭石）`#a7622c`　hover `#7d461b`
- 學生端主色（深青）`#2f5560`
- 警示/錯誤（赭紅）`#8a4b3f`
- Admin/平台端側邊選單：暖灰褐 `#e3dcd1`（背景）、`#2b2822`（文字）、`#d2c6b4`（選中背景）

**字體**：標題 Noto Serif TC（600/700）；內文 Noto Sans TC（400/500/700）；數字/英文小標 EB Garamond（400/500/600）

**造型**：邊框 1px 實線，圓角 3–4px；不用陰影漸層

## Business Rules（全站一致，後端邏輯核心）
- 平台服務費 10%（可由 Admin 費用設定調整，需 30 天預告生效）
- 信用卡手續費 2.89%；ATM 每筆 NT$15
- 撥款轉帳費：同行 NT$10／跨行 NT$20
- 退款處理費 NT$25
- **退費規則採「平台底線 + 老師微調」機制**：平台設定不可調低的最低退費比例（依系統成本估算：開課前 14 日以上底線 100%、7–13 日 50%、1–6 日 15%、開課後 0%），老師可調整天數門檔與比例，但比例不得低於底線
- 退課申請由老師 5 個工作日內回覆，逾期系統自動核定退費；雙方談不成可升級「平台仲裁」，仲裁結果為最終決定
- 撥款月結每月 5 日，未達 NT$500 累計；戶名須與身分驗證姓名一致，不符會被銀行退回（見 Payout Account 的「已退回」狀態）
- 通知管道：站內通知＋Email 為必開（不可關閉），LINE 官方帳號為選用加值管道；不使用簡訊
- 老師與學生的一般問題導向 LINE 官方帳號聯繫；**只有退課/仲裁的正式溝通留在站內**（Message Thread 頁），因為這些對話會被平台當作審核與仲裁依據

## Screens

### 對外行銷
| 檔案 | 說明 |
|---|---|
| StepGo Homepage.dc.html | 首頁：hero、雙入口（老師/學生插畫）、費用試算摘要、正在招生 |
| StepGo Courses.dc.html | 課程列表（含類別篩選、搜尋空狀態）／課程詳情 |
| StepGo About.dc.html | 關於學步 |
| StepGo Terms.dc.html | 合作條款：老師條款／學生退費雙分頁 |
| StepGo Fee Calculator.dc.html | 費用試算工具 |
| StepGo For Teachers.dc.html | 老師招募頁 |
| StepGo For Students.dc.html | 學生招募頁 |
| StepGo Wireframes.dc.html | 系統線框圖總覽（探索用畫布文件，非產品畫面） |

### 老師系統
| 檔案 | 說明 |
|---|---|
| StepGo Auth.dc.html | 登入／註冊分流 |
| StepGo Teacher Dashboard.dc.html | 老師後台：總覽／我的課程／收款紀錄／撥款（含通知設定）／退課申請／建立課程（含退費底線機制、送出審核等待畫面、空狀態） |
| StepGo Teacher Verification.dc.html | 身分驗證：填寫／審核中／未通過／通過 |
| StepGo Payout Account.dc.html | 撥款帳戶設定：檢視／變更／核對中／銀行退回 4 狀態 |
| StepGo Refund Review.dc.html | 退課審核 |

### 學生系統
| 檔案 | 說明 |
|---|---|
| StepGo Student Dashboard.dc.html | 學生後台：我的課程（含空狀態）／付款紀錄／退課申請／帳號設定 |
| StepGo Checkout.dc.html | 報名付款流程，5 個狀態 |
| StepGo Course In Progress.dc.html | 學生上課中頁：進度、下一堂、繳費/退費快照 |
| StepGo Message Thread.dc.html | 退課申請的正式留言串（僅此用途，一般問題導向 LINE） |
| StepGo Order Not Found.dc.html | 訂單／課程 404 頁 |

### 平台管理者（Admin，獨立側邊選單）
| 檔案 | 說明 |
|---|---|
| StepGo Admin Cashflow.dc.html | 金流總覽：營收統計、資金核對、撥款佇列、退款總覽 |
| StepGo Admin Fees.dc.html | 費用設定：服務費（30天預告排程）、轉帳/退款處理費、退費底線、變更紀錄 |
| StepGo Admin Directory.dc.html | 老師與課程列表、搜尋 |
| StepGo Admin Accounts.dc.html | 帳號與權限：成員管理、角色權限矩陣 |
| StepGo Admin Audit Log.dc.html | 操作日誌：依類別篩選（費率/驗證/仲裁/權限/撥款） |
| StepGo Arbitration.dc.html | 平台仲裁（客服/營運視角）：案件佇列、雙方主張、裁定 |

### 行動裝置
| 檔案 | 說明 |
|---|---|
| StepGo Mobile.dc.html | 6 個手機畫面：首頁、課程列表、課程詳情、學生我的課程、學生付款紀錄、老師通知 |

## Responsive Breakpoints
全站已統一響應式斷點：系統頁（側邊選單版面）1080px／980px；行銷頁 900px／640px。範本可參考 Refund Review、Auth、Teacher Dashboard。

**已知坑**：帶側邊選單的頁面若 `<aside>` 用 inline style 設定 `flex-direction:column`，媒體查詢裡改成 `row` 必須加 `!important`，否則 inline style 會蓋掉，窄螢幕下側邊選單不會收成橫向列（Teacher/Student Dashboard 曾踩過這個坑，已修正）。

## Assets
`assets/` 資料夾內含已裁切好的插畫素材（PNG）：
- `homepage-hero-figure.png`、`teacher-illustration.png`、`students-illustration.png`：首頁人物插畫
- `owl-icon.png`：右下角貓頭鷹助手圖示（圓形裁切，暫無透明背景全身素材）
其餘 image-slot 占位（About/For Students 頁的貓頭鷹全身插畫等）仍待正式素材，實作時先用等比例佔位圖。

## Files
本包含所有 26 個 `.dc.html` 設計稿（純 HTML，內嵌樣式與邏輯，可直接在瀏覽器開啟預覽）＋ `assets/` 素材資料夾。
