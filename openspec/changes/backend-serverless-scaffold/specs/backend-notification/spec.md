## Purpose

依業務規則的管道優先順序（站內＋Email 必開、LINE 選用加值）派送系統通知，確保核心通知有保底送達管道，且文案可由管理者調整而不需改動程式碼。

## ADDED Requirements

### Requirement: 通知管道優先順序與保底送達
系統 SHALL 於每個通知事件觸發時：若使用者已留 Email，優先寄送 Email；無論是否留有 Email，SHALL 一律在站內會員中心留存一份通知記錄；若使用者已綁定 LINE，SHALL 額外推播 LINE 作為加值管道。未綁定 LINE 或未留 Email 的使用者，核心通知的送達 SHALL NOT 因此完全中斷（至少站內通知記錄存在）。

#### Scenario: 未留 Email 也未綁 LINE 的使用者仍有站內通知記錄
- **WHEN** 一個未填 Email 且未綁定 LINE 的使用者的訂單狀態變更
- **THEN** 系統仍在其站內會員中心留存一筆對應的通知記錄

#### Scenario: 已綁 LINE 的使用者額外收到 LINE 推播
- **WHEN** 一個已綁定 LINE 的使用者的訂單狀態變更且該使用者也已留 Email
- **THEN** 系統寄送 Email、留存站內通知記錄，並額外呼叫 LINE Messaging API 推播

### Requirement: 通知文案範本化
系統 SHALL 將通知文案存於可由管理者調整的範本資料中，範本 SHALL 支援變數替換（例如課程名稱、金額、繳費期限），SHALL NOT 將通知文案寫死於程式碼中。

#### Scenario: 調整範本文案不需部署程式碼
- **WHEN** 管理者修改某個通知範本的文字內容
- **THEN** 下一次觸發該通知事件時即套用新文字，不需要重新部署程式碼

### Requirement: 核心通知管道不可由使用者關閉
系統 SHALL 將站內通知與 Email 列為核心必要通知管道，使用者 SHALL NOT 能將其關閉；僅 LINE 推播可由使用者自行選擇是否綁定/接收。

#### Scenario: 使用者無法關閉 Email 通知選項
- **WHEN** 使用者在帳號設定中尋找關閉 Email 通知的選項
- **THEN** 系統不提供此選項，Email 通知維持必開狀態
