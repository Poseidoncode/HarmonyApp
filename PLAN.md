# HarmonyApp Windows Native Port — AI Agent Implementation Plan

> **狀態：Windows 原始碼已實作、Release / 125 項測試 / self-contained publish 已驗證；實機安全與跨環境驗收尚未完成**  
> **執行紀錄：** [`Windows/IMPLEMENTATION_STATUS.md`](Windows/IMPLEMENTATION_STATUS.md)。Auto Paste 因實測焦點風險預設停用，僅提供當次明確啟用的實驗模式；完整 DoD 尚未達成。CI/CD 因本機專案規則禁止而不實作。  
> **日期：2026-10-23**  
> **來源 Repository：** https://github.com/Poseidoncode/HarmonyApp  
> **目標：** 保留既有 macOS SwiftUI 版本，新增以 **C# / WPF / .NET 10 LTS** 開發的 Windows 原生版。  
> **交付模式：** 在同一個 Repository 維護兩個原生程式，共用範本 JSON 規格及一致性測試；**不改用 Tauri、Flutter 或跨平台 UI 重寫**。

---

## 0. 給 AI Coding Agent 的執行指令（請先閱讀）

你是負責此 Repository 的資深桌面應用工程師。請**直接按本計畫逐階段完成 Windows 版**，而非僅提出建議或產出虛構範例。

### 必須遵守

1. **先檢查 Repository 實際狀態與目前 commit SHA**，以檔案內容為準；本計畫所列路徑是依 2026-10-23 公開 `main` 分支檢查所得。若路徑或行為已變更，以新版程式碼為準並記錄差異。
2. **不得破壞既有 macOS 版本。** 保留 `HarmonyPrompts/HarmonyPrompts.xcodeproj`、現有 Swift 原始碼、`install.sh`、資源及既有安裝流程。除非為了共用預設 JSON 所必須，原則上不改 Swift 專案；任何必要更動必須可回退且驗證成功。
3. **Windows 使用 C# / WPF / .NET 10 LTS；採 MVVM。** 核心規則與原生 Windows 操作分層，不把 Win32 程式碼混進 ViewModel。
4. **原版 `templates.json` 是相容性基準。** 不可更改頂層陣列格式、欄位名稱、欄位型別的字串值，也不可遺失匯入的使用者範本。
5. 優先確保 **Copy & Paste 不會貼到錯誤程式**。遇到視窗焦點失敗、目標關閉或權限限制時，必須只保留剪貼簿內容並提示手動 `Ctrl+V`；禁止無條件模擬輸入。
6. **尊重作業系統安全邊界**；不得以提權、關閉系統保護或其他繞過 Windows 安全機制的方式實現貼上。
7. 維持產品「**完全離線、無遙測、無資料上傳**」原則。不要加入分析 SDK、雲端服務、隱藏網路呼叫，亦不要在記錄檔寫入 Prompt 原文。
8. 每階段完成後執行可用的建置／測試，更新本檔核取方塊與 `Windows/IMPLEMENTATION_STATUS.md`，留下命令與實際輸出摘要。**未執行的測試不得標示通過。**
9. 若執行環境不是 Windows，仍可建立所有原始碼、純邏輯單元測試與 GitHub Actions 設定，但 WPF 實機建置與跨程式貼上必須標為 **待 Windows Runner／實機驗證**，不可宣稱完成。
10. 遇到不明確的行為：先看既有 Swift 實作及 README；再採與原版等價且最小風險的預設，記錄選擇。不要擅自增加付費、同步、網路、開機自啟等新功能。
11. 不需為一般實作細節反覆詢問使用者；在既定範圍內採合理決策繼續完成。不要因為某個 Windows 限制而中止所有其他階段；應實作明確降級行為並留下測試結果。
12. 完成後提交可審查的變更清單與驗收報告；不要聲稱已 push / release，除非有執行並確認。

### Definition of Done（整體完成條件）

- Windows 專案可在受支援 Windows 10/11 x64 乾淨環境建置、執行、打包。
- 系統匣、範本清單與搜尋、五種動態欄位、即時預覽、字／詞數、複製、Auto Paste、JSON 匯入及範本資料夾操作可用。
- 共用 `templates.json` 可由 macOS 與 Windows 讀取，產生相同的 Prompt 文字。
- 已完成 Core 單元測試、Repository 故障測試、Windows Paste 實機驗證矩陣。
- 貼上功能「不會將內容送往錯誤視窗」優先於成功率；不可貼上的情況具備手動貼上 fallback。
- Mac 既有流程沒有被回歸破壞；README 與 Windows 使用文件完成。
- GitHub Actions 的 Windows CI 可在 PR 上執行 restore、build、test、publish；失敗時使 CI 失敗。

---

## 1. 已核對的原始專案事實

### 1.1 主要現有檔案

| 原始路徑（相對 Repo 根目錄） | 目前用途 | Windows 對應 |
|---|---|---|
| `HarmonyPrompts/HarmonyPrompts/Models/PromptField.swift` | 五種欄位、初始值 | `Core/Models/PromptField.cs` |
| `HarmonyPrompts/HarmonyPrompts/Models/PromptTemplate.swift` | 範本資料模型 | `Core/Models/PromptTemplate.cs` |
| `HarmonyPrompts/HarmonyPrompts/Services/PromptRenderer.swift` | 變數替換、字詞統計 | `Core/Services/PromptRenderer.cs`、`PromptStats.cs` |
| `HarmonyPrompts/HarmonyPrompts/Services/TemplateStore.swift` | 載入、seed、JSON 匯入、儲存佇列 | `Windows/Services/TemplateRepository.cs` |
| `HarmonyPrompts/HarmonyPrompts/Services/FocusTracker.swift` | 追蹤非本程式的前景 App | `Windows/Services/WindowFocusService.cs` |
| `HarmonyPrompts/HarmonyPrompts/Services/ClipboardService.swift` | 複製、切回原 App、模擬貼上 | `ClipboardService.cs`、`PasteService.cs` |
| `HarmonyPrompts/HarmonyPrompts/Views/ContentView.swift` | 雙欄 UI、匯入、Toast、主要互動 | `Views/MainWindow.xaml`、`ViewModels/MainViewModel.cs` |
| `HarmonyPrompts/HarmonyPrompts/HarmonyPromptsApp.swift` | WindowGroup、MenuBarExtra、Settings | `App.xaml`、`TrayService.cs`、`SettingsWindow.xaml` |
| `HarmonyPrompts/HarmonyPrompts/Resources/templates.json` | 內建範本 | `shared/templates.json` 的初始來源 |

> 本表是程式碼對照，不表示必須一對一翻譯；應遵循 C# 責任分層。

### 1.2 現有資料契約（必須相容）

JSON 頂層是 **`PromptTemplate[]` 陣列**，不是 `{ "templates": [...] }`。

每個 Template 具備：

- 必要：`id`, `name`, `description`, `template`, `fields`
- `fields` 內必要：`name`, `label`, `type`
- `fields` 內選用：`placeholder`, `options`, `defaultValue`, `checkedValue`, `uncheckedValue`
- `type` 允許：`text`, `textarea`, `select`, `radio`, `checkbox`
- 變數佔位符模式：`\{\{([a-zA-Z0-9_-]+)\}\}`（正式 Regex 請使用原檔案的語意）
- `PromptField.id` 等於 `name`；`PromptTemplate.id` 是獨立字串。

依既有 `PromptField.initialValue()`：

| 類型 | 規則（精確保留 Swift 語意） |
|---|---|
| `text`, `textarea` | `defaultValue ?? placeholder ?? ""`；若 `defaultValue` 為空字串，**仍使用空字串** |
| `select`, `radio` | 非空 `defaultValue`，否則 `options` 第一個元素，否則 `""`；不對 options 自動重排 |
| `checkbox` | `defaultValue` 忽略大小寫等於 `"true"` 時為勾選；勾選值 `checkedValue ?? "true"`；未勾選值 `uncheckedValue ?? ""` |

依現有 `PromptRenderer`：

- 單次從左至右替換；不遞迴解析 replacement 中的 `{{...}}`。
- 未知 key 的原始 `{{key}}` 原樣保留；key 大小寫敏感。
- key 只匹配 ASCII 字母、數字、底線、連字號；其他形式原樣保留。
- Unicode 文字要原樣保留，使用正確的 `string` 與 UTF-8 JSON 序列化。
- `PromptStats.chars` 使用 Swift Character（grapheme cluster）數量，不等於 C# `string.Length`。
- `PromptStats.words` 以 Swift `Character.isWhitespace` 切分連續非空白字元；Windows 可先用 Unicode text element API 實作，並用 fixtures 驗證，對 Unicode 分段差異需明確列出。

依 `TemplateStore`：

- 使用者資料：macOS `~/Library/Application Support/HarmonyPrompts/templates.json`；Windows 擬定 `%APPDATA%\HarmonyPrompts\templates.json`。
- 第一次沒有檔案時，從 bundle 內建 `templates.json` 複製；**已有使用者檔案不得覆蓋**。
- `importFromData` 讀取 **非空陣列**，失敗不取代現有記憶體資料；成功後儲存。
- 多次匯入使用 ticket 判斷新舊，只有最新的匯入結果可以套用。
- 儲存以 FIFO 串列化並原子性寫入，避免舊快照蓋過新快照。
- 預設範本是 5 個（`explain-code`、`find-bugs`、`generate-tests`、`refactor-code`、`add-docs`）；實際清單應以 Repository 當下 JSON 為準。

---

## 2. 技術與架構決策

### 2.1 技術堆疊

- **Windows UI：** WPF + XAML、MVVM（可用 `CommunityToolkit.Mvvm`，但優先少量且有必要的套件；鎖定版本）。
- **Runtime / SDK：** `.NET 10 LTS`、`net10.0-windows`。
- **Core：** `net10.0` Class Library，不參考 WPF / Win32；方便獨立測試。
- **資料：** `System.Text.Json`，字串 enum converter／顯式 mapping，保留 camelCase property。
- **系統匣：** Windows Forms `NotifyIcon`（WPF 專案 `<UseWindowsForms>true</UseWindowsForms>`）。
- **Windows API：** 透過 P/Invoke/LibraryImport 小型 adapter 封裝 user32 所需呼叫。
- **單元測試：** xUnit；UI／跨程式貼上用人工實機測試 + 可重複的測試工具視窗。
- **CI：** GitHub Actions `windows-latest`，restore/build/test/publish。
- **發行：** 先 self-contained win-x64 發布與 ZIP；安裝程式可用 Inno Setup 或 WiX，擇一且在文件說明選擇理由。

### 2.2 建議目錄結構

```text
HarmonyApp/
├── HarmonyPrompts/                         # 既有 macOS 專案，保留原位置
│   ├── HarmonyPrompts.xcodeproj
│   ├── HarmonyPrompts/
│   └── scripts/
├── Windows/
│   ├── HarmonyPrompts.slnx
│   ├── HarmonyPrompts.Core/
│   │   ├── Models/
│   │   │   ├── FieldType.cs
│   │   │   ├── PromptField.cs
│   │   │   └── PromptTemplate.cs
│   │   └── Services/
│   │       ├── PromptRenderer.cs
│   │       └── PromptStats.cs
│   ├── HarmonyPrompts.Windows/
│   │   ├── App.xaml
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml
│   │   │   └── SettingsWindow.xaml
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Services/
│   │   │   ├── TemplateRepository.cs
│   │   │   ├── WindowFocusService.cs
│   │   │   ├── ClipboardService.cs
│   │   │   ├── PasteService.cs
│   │   │   └── TrayService.cs
│   │   ├── Native/Win32.cs
│   │   └── Resources/
│   ├── HarmonyPrompts.Tests/
│   ├── IMPLEMENTATION_STATUS.md
│   └── README.md
├── shared/
│   ├── templates.json
│   ├── templates.schema.json
│   └── test-fixtures/
│       ├── renderer-cases.json
│       ├── field-default-cases.json
│       └── stats-cases.json
├── .github/workflows/windows.yml
├── PLAN.md
└── README.md
```

**單一預設資料來源策略：** 以 Swift 目前的 `Resources/templates.json` 為 canonical source，建立 `shared/templates.json`；先讓 Windows build 自 `shared/templates.json` 複製進發布內容。Mac 最初繼續使用原資源，CI 加入 bytes/semantic parity 檢查；待 Mac 可驗證後再選擇是否改成 Xcode 引用 shared 或由 script 同步。不可在未驗證的情況刪除原 Mac bundle 資源。

### 2.3 依賴方向

```text
WPF Views ---> ViewModels ---> Core Models + PromptRenderer
                |   |
                |   +---------> TemplateRepository (file system)
                +-------------> PasteService ---> ClipboardService + WindowFocusService
                                              \-> Win32 adapter
App lifecycle -------------------------------> TrayService
```

核心邏輯不可依賴 `System.Windows`、`user32.dll` 或特定 WPF control。

---

## 3. 分階段任務清單

> 以下 `[x]` 表示該實作／本機檢查已完成，不代表整個 Phase 的人工驗收通過；實際 PASS / FAIL / NOT RUN 與保留項目以 `Windows/IMPLEMENTATION_STATUS.md` 為準。未能驗證的 DPI、跨應用程式、安全及乾淨 VM 條件仍保持未勾選。

### Phase 0 — 基線盤點與測試規格（先完成）

- [x] 讀取 `README.md`、macOS 專案結構、上述所有 Swift 檔案與當前 JSON；記錄 commit SHA。
- [x] 建立 `Windows/IMPLEMENTATION_STATUS.md`：環境、commit SHA、階段狀態、風險、測試記錄。
- [x] 新增 `shared/templates.json`（從現有正式 JSON 複製，不人工重打一份）；保留 macOS 原檔。
- [x] 產生 JSON Schema（定義現有資料契約；不要新增會拒絕原有合法檔案的限制）。
- [x] 以 Swift 程式的精確語意建立 `renderer-cases.json`、`field-default-cases.json`、`stats-cases.json` golden fixtures。
- [x] 規劃 Windows 預設資料位置、錯誤訊息與設定路徑；確認 100% offline。

**驗收：** fixtures 能顯示來源輸入、預期輸出；`shared/templates.json` 與現有 Mac 資源具備可驗證一致性；任何無法精確還原的 Unicode 細節有註記。

### Phase 1 — Windows Shell 與貼上技術 PoC（最高風險，優先）

- [x] 建立 WPF 應用程式、通知區圖示與最小主視窗（只放「Copy」「Copy & Paste」及固定測試文字）。
- [x] 關閉視窗時預設隱藏到 tray；tray 有 Open、Settings、Exit；明確退出時正確釋放資源。
- [x] 啟動前景視窗追蹤：`GetForegroundWindow` + `GetWindowThreadProcessId`；記錄最後有效的**非 Harmony** 前景 HWND 與 process ID。
- [x] 視需要使用 `SetWinEventHook(EVENT_SYSTEM_FOREGROUND, WINEVENT_OUTOFCONTEXT)`；回呼、生命週期、UI thread 派送及 `UnhookWinEvent` 必須安全處理。
- [x] 僅在合理時點鎖定貼上對象（使用者啟動 Harmony 前），避免開啟 tray、對話方塊、檔案選取器造成 target 被意外覆蓋。
- [x] `ClipboardService.Copy` 寫入 Unicode 文字；處理剪貼簿暫時忙碌（有界重試、最後錯誤提示）。WPF Clipboard 操作需在 STA dispatcher。
- [x] `PasteService` 執行以下狀態流程（原生受控測試工具已實作；產品 Auto Paste 預設停用，安全驗收仍未完成）：
  1. 複製文字到剪貼簿。
  2. 檢查 HWND 仍存在（`IsWindow`）、不是 Harmony 自身、process 身分與前次捕捉一致；失敗則降級。
  3. 隱藏 Harmony 視窗、解除佔用焦點；在使用者操作觸發的前景切換限制下**嘗試** `SetForegroundWindow`，不可假定一定成功。
  4. 設定有界等待／重試，以 `GetForegroundWindow` 檢查 **目標 HWND** 已是實際焦點（可視情況對同進程窗口設適當判斷，但不得放寬至任意程式）。
  5. 目標焦點確認後，送出完整 Ctrl down → V down → V up → Ctrl up 事件；檢查 `SendInput` 回報插入事件數量，並盡力確保按鍵釋放。
  6. 無法確認焦點或送入事件失敗時**不對其他視窗送鍵**，保留 Clipboard 並告知使用者手動 `Ctrl+V`。
- [x] 不要使用持續全域鍵盤監聽；不需要讀取使用者輸入的內容。
- [x] 明確分類結果：`CopiedOnly`、`InputSent`、`NoTarget`、`TargetGone`、`FocusDenied`、`InputBlocked`、`ClipboardError`。
- [x] UI 字眼不能把「`SendInput` 成功」宣稱成「已證實文字到達目標輸入框」。

**驗收（Windows 實機）：** 順利從 VS Code、Cursor、Chrome/Edge 文字框、Windows Terminal 打開 Harmony 並送回內容；關閉目標、快速切換視窗、系統管理員權限目標時不得貼到錯誤位置。失敗時可手動 Ctrl+V。

### Phase 2 — Core 資料模型、渲染與統計

- [x] 建立 `HarmonyPrompts.Core` 與 `HarmonyPrompts.Tests`，均不依賴 WPF。
- [x] 定義 `FieldType`（JSON 小寫字串）、`PromptField`、`PromptTemplate`，維持相同 JSON property names 與可選欄位。
- [x] 實作 `PromptField.InitialValue()`，逐字遵守第 1.2 節規則。
- [x] 實作 `PromptTemplate.MakeInitialValues()`，建立 `name -> initial value`。對重複 field name 的處理先遵照 Swift 已有行為或顯式拒絕並清楚報錯，不能默默覆蓋資料。
- [x] 實作 `PromptRenderer.Render()`：Regex 一次掃描，未知 placeholder 不變、不遞迴、字串大小寫敏感。
- [x] 實作 `PromptStats.Measure()`：Unicode text elements、Swift whitespace 語意與 golden fixtures；不要以 `string.Length` 當 Character 數。
- [x] 建立讀取既有完整 `templates.json` 的 JSON roundtrip 測試，測試再序列化不遺失 optional property 的資訊。
- [x] 修正 `Nullable` 警告、C# code style 與例外訊息。

**驗收：** Core xUnit 測試全過，包含 emoji、組合音標、換行、未知 key、重複 placeholder、checkbox default、多種欄位型別、空字串等案例。

### Phase 3 — TemplateRepository 與 JSON 管理

- [x] Windows 資料路徑：`Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HarmonyPrompts", "templates.json")`。
- [x] 首次啟動建立資料夾並 seed bundle 內建範本；如果使用者檔案存在，絕不覆蓋。
- [x] `LoadAsync` 使用異步檔案存取；錯誤時顯示錯誤並避免破壞原檔（不可無聲刪除使用者範本）。
- [x] `ImportAsync` 從檔案讀取 JSON；反序列化成功且陣列非空才能套用；失敗時保留原有資料與檔案。
- [x] 使用 generation ticket / cancellation-aware sequencing 防止較早的匯入延遲完成後覆蓋較新匯入。
- [x] `SaveAsync` 使用 `SemaphoreSlim` 或單一序列化佇列，保證快照順序；臨時檔與目的檔置於**同一資料夾**，同一磁碟採原子替換／搬移方案，並清理失敗的暫存檔。
- [x] 加入備份／可恢復策略（至少對寫入失敗不破壞原檔；如新增 `.bak`，需明確測試還原與更新邏輯）。
- [x] 支援「Import JSON…」「Open Templates Folder」及適當的錯誤顯示。
- [x] 模擬讀取中快速匯入、連續兩次匯入、儲存中重新載入、檔案被占用、無權限、損壞 JSON 的測試。

**驗收：** 資料檔在重啟後保持正確；快速操作不會導致舊內容蓋過新內容；匯入失敗不丟資料。

### Phase 4 — WPF 完整介面與 MVVM

- [x] 實作雙欄工作流程：左側搜尋／範本列表／匯入；右側範本欄位／即時預覽／統計／Copy／Copy & Paste。
- [x] 五種欄位控制：`text -> TextBox`、`textarea -> multiline TextBox`、`select -> ComboBox`、`radio -> RadioButton`、`checkbox -> CheckBox`。
- [x] `MainViewModel` 管理選取範本、欄位值、產生結果、複製命令、錯誤與 Toast；避免 code-behind 承擔商業邏輯。
- [x] 首次載入資料採第一個範本；只有在**實際切換範本或選取的範本內容已變更**時重建該範本初始值；背景載入／畫面重繪不得清掉使用者正在輸入的內容。
- [x] 即時更新 Prompt 預覽和字詞數；保留未知 placeholder 的顯示。
- [x] 複製時回報 `Copied`；貼上時按第 3.2 節結果顯示成功送鍵／需手動貼上，不誇大狀態。
- [x] Settings 視窗顯示範本儲存位置、開啟資料夾、使用說明；Windows 版不顯示 macOS Accessibility 設定項目。
- [ ] 支援鍵盤操作、Tab 順序、文字縮放、基本 Screen Reader label、125%／150% DPI、多螢幕。
- [x] 視覺風格參考原 SwiftUI 主畫面，但以 WPF 原生可維護性為先；不需要像素級複刻 Mac。
- [x] 全域熱鍵、開機自啟、雲端同步不是本階段要求；如原始功能檢查顯示必要，單獨記錄並審核。

**驗收：** 在正常桌面工作流程中完成選範本、填參數、預覽、複製、貼回；五種欄位與資料輸出均符合 Mac 版；畫面縮放可用。

### Phase 5 — 測試、文件、CI/CD 與發行

- [ ] **不實作：本機專案規則禁止 CI/CD。** 原計畫項目：新增 `.github/workflows/windows.yml`：checkout、setup-dotnet 10、`dotnet restore`、`dotnet build -c Release`、`dotnet test -c Release`、`dotnet publish -c Release -r win-x64 --self-contained true`，以及上傳 build artifact。
- [x] 新增共享資料 parity test：本機測試驗證 Mac 內建 JSON 與 `shared/templates.json` 的 bytes 一致；CI/CD 受本機規則限制，未建立。
- [x] 形成 `Windows/README.md`：需求、建置、偵錯、資料夾位置、匯入、系統匣、Copy & Paste 限制、故障排除。
- [x] 更新根目錄 README 的 macOS / Windows 下載與建置章節，維持原有 Mac 安裝指令。
- [x] 加入 Windows icon 與 version metadata；不得誤用 Apple 權限提示。
- [ ] 產生 self-contained win-x64 ZIP，確認 fresh Windows VM 可執行；若可行再加入安裝程式（檢查安裝、升級、解除安裝是否保留使用者資料）。
- [ ] 在 Windows 10/11 標準帳號測試；另測 DPI、CJK/emoji、剪貼簿佔用、權限不同、切換虛擬桌面等。
- [x] 執行 `dotnet test`、Release Build、Publish；留下版本、命令與結果。macOS 可用 runner / 開發機時再確認 Xcode build。
- [x] 彙整問題清單、已知限制及 workaround；未通過的驗收條件明確列為 open issue。

**驗收：** PR 建置與單元測試成功；Windows 乾淨環境測試報告完整；安裝包／ZIP 實際存在且可用；macOS 版本不受影響。

---

## 4. 端到端測試矩陣（不可略過）

| ID | 測試 | 預期結果 | 測試類型 |
|---|---|---|---|
| CORE-01 | `{{language}}` 正常替換 | 生成預期文字 | Unit |
| CORE-02 | `{{unknown}}` 無對應 | 保留原佔位符 | Unit |
| CORE-03 | value 中還有 `{{x}}` | 不做第二次替換 | Unit |
| CORE-04 | 大小寫、底線、連字號 key | 與 Swift Regex 一致 | Unit |
| CORE-05 | 五種欄位預設值 | 與 Swift `initialValue()` 一致 | Unit |
| CORE-06 | Unicode grapheme / 中英文空白 | 字數詞數經 golden fixtures 驗證 | Unit |
| JSON-01 | 內建 JSON load / roundtrip | 所有範本資料保留 | Unit |
| JSON-02 | 空陣列、錯誤 JSON 匯入 | 拒絕並保留原始資料 | Unit |
| JSON-03 | 連續 Import A → B | 只採最新有效結果（依既有 ticket 規則） | Integration |
| JSON-04 | 併發 Save + Reload | 不被過期版本覆蓋 | Integration |
| UI-01 | 搜尋、選範本、填欄位 | 預覽即時正確 | Manual |
| UI-02 | 背景 reload | 未變更範本不重設使用者輸入 | Manual |
| PASTE-01 | VS Code 編輯器 | 目標焦點正確時可貼上 | Windows manual |
| PASTE-02 | Cursor 編輯器 | 目標焦點正確時可貼上 | Windows manual |
| PASTE-03 | Chrome/Edge 輸入框 | 目標焦點正確時可貼上 | Windows manual |
| PASTE-04 | Windows Terminal | 可貼上或清楚說明限制 | Windows manual |
| PASTE-05 | 目標視窗已關閉 | 不送出 Ctrl+V；可手動貼上 | Windows manual |
| PASTE-06 | 操作中切到第三個程式 | 不向第三個程式貼上 | Windows manual |
| PASTE-07 | 管理員權限目標 | 受阻時只複製並提示，不提權 | Windows manual |
| PASTE-08 | Clipboard 暫時被占用 | 有界處理，不死鎖、不當成成功 | Windows manual |
| PASTE-09 | 其他輸入法、鍵盤配置 | Ctrl+V 送鍵結果可接受 | Windows manual |
| PACK-01 | fresh Windows VM | 可啟動、載入內建 JSON | Release smoke |
| PACK-02 | 關閉視窗再由 tray 開啟 | 單一合理實例、資料不遺失 | Release smoke |
| MAC-01 | 原 Xcode 專案 build | 沒有因 Windows 變更而破壞 | macOS CI/manual |

**重點：** Windows `SendInput` 只回報輸入事件寫入狀態，並非目標編輯器「文字已寫入」的證據。驗收需按上表進行實機觀察，UI 用詞也必須一致。

---

## 5. 初始化指令（由 Agent 在 Windows 開發環境執行）

以下指令需在 Repository 根目錄執行，並依最新 SDK / CLI 行為調整；請**實際驗證結果**，不要只複製命令：

```powershell
mkdir Windows
cd Windows

dotnet new sln -n HarmonyPrompts
dotnet new wpf -n HarmonyPrompts.Windows -f net10.0
dotnet new classlib -n HarmonyPrompts.Core -f net10.0
dotnet new xunit -n HarmonyPrompts.Tests -f net10.0

dotnet sln add HarmonyPrompts.Windows/HarmonyPrompts.Windows.csproj
dotnet sln add HarmonyPrompts.Core/HarmonyPrompts.Core.csproj
dotnet sln add HarmonyPrompts.Tests/HarmonyPrompts.Tests.csproj

dotnet add HarmonyPrompts.Windows/HarmonyPrompts.Windows.csproj reference HarmonyPrompts.Core/HarmonyPrompts.Core.csproj
dotnet add HarmonyPrompts.Tests/HarmonyPrompts.Tests.csproj reference HarmonyPrompts.Core/HarmonyPrompts.Core.csproj
```

WPF 應用程式 `.csproj` 至少包含相應屬性；如範本已產生某屬性，勿重複：

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
  <UseWindowsForms>true</UseWindowsForms>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

> 注意：WPF + WinForms 混用可能導致 `Application`、`MessageBox` 等類別名稱衝突；請使用 namespace alias 或完整限定名稱。`NotifyIcon`、hook 與視窗資源應在退出時 `Dispose()`／Unhook。

Windows 發布命令：

```powershell
dotnet restore Windows/HarmonyPrompts.slnx
dotnet build Windows/HarmonyPrompts.slnx -c Release
dotnet test Windows/HarmonyPrompts.slnx -c Release
dotnet publish Windows/HarmonyPrompts.Windows/HarmonyPrompts.Windows.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true
```

**交付物至少包含：** `Windows` Source、Core Tests、Windows CI、Windows README、Release artifact、實機測試記錄；若當前環境無法執行 GUI，必須如實標示待驗證，不可用測試替身冒稱實機測試。

---

## 6. Win32 實作安全限制與常見陷阱

1. `SetForegroundWindow` 可能遭 Windows 前景鎖定政策拒絕，即使 HWND 有效也不代表能成功。**檢查回傳值及前景視窗**；不能擅自改送到其他視窗。
2. `SendInput` 受 UIPI 完整性等級限制；一般權限程式不能對較高權限目標可靠注入。**明確 fallback**，不要嘗試繞過。
3. `SetWinEventHook` 若用 out-of-context callback，需要執行緒訊息迴圈；注意保存 delegate，避免 GC 回收；退出時 `UnhookWinEvent`。
4. 若在 Harmony 開啟後使用者主動切換目標，優先遵循**使用者當下清楚選定的有效對象**，否則拒絕貼上。嚴禁背景悄悄對陌生視窗送鍵。
5. 不要使用 Clipboard 後立刻清空；貼上失敗必須保留文字以便手動貼上。
6. `PromptTemplate` 資料是一般使用者輸入，不應作為 shell 指令執行，也不應拼接進 PowerShell／cmd 自動執行。
7. 內建範本與使用者資料必須分開；升級 App 不能覆蓋 `%APPDATA%` 中的自訂內容。
8. WPF dispatcher／UI thread 載入不要同步阻塞；模擬輸入的有界延遲不可用長時間的 `Thread.Sleep` 阻塞 UI。
9. 有任何自動化權限不足，提示使用者採手動操作；不要求停用防護、不要求整個程式以管理員權限執行。

---

## 7. 建議的實作順序與提交單位

請保持 PR / commit 變更範圍清晰；以下是建議切分：

1. `chore: establish Windows solution and cross-platform fixtures`
2. `feat(windows): add tray shell and safe paste proof of concept`
3. `feat(core): port prompt models renderer and stats`
4. `feat(windows): implement transactional template repository`
5. `feat(windows): implement WPF prompt workflow`
6. `test(ci): add Windows build tests and compatibility checks`
7. `docs(release): add Windows guide and packaging`

**關卡規則：** Phase 1 的貼上安全性未達標時，應標記風險並保留只複製模式，仍可完成 Phase 2–5；不得把「自動貼上可用」標為完成。

---

## 8. Agent 每階段回報格式

更新 `Windows/IMPLEMENTATION_STATUS.md`，至少包含：

```markdown
# Windows Implementation Status
- Base commit: <sha>
- Environment: <OS, .NET SDK, Visual Studio / runner>
- Current phase: <phase>

## Completed
- [x] <實際完成項目，附路徑>

## Verification
- Command: `...`
- Result: PASS / FAIL / NOT RUN
- Notes: <關鍵輸出 / 失敗原因>

## Risks / Blockers
- <問題、使用者影響、fallback 或後續解法>

## Next Actions
- <下一個可直接執行的工作>
```

最後回報要包括：實際新增檔案、建置結果、測試通過/失敗/未執行數、Windows 實機測試紀錄、剩餘限制、如何執行 Windows 版，以及任何對 macOS 專案的更動。

---

## 9. 不在此次範圍內（Non-goals）

- 不是把 SwiftUI 改寫成 Flutter、Tauri、Electron 或 Web App。
- 不加入 Linux 支援（若未來需要，另開設計）。
- 不提供線上帳號、雲端同步、網路後端、資料分析或遙測。
- 不重構 macOS UI、不順便改寫已工作的 Mac 系統事件處理。
- 不增加全域鍵盤監控、權限提昇、繞過 UIPI 或強制奪取其他程式焦點的手段。
- 不保證能自動貼入所有應用程式、UAC 提權視窗、受保護桌面或不同權限等級的程式。

---

## 10. 參考原始碼與官方技術文件

### HarmonyApp 原始碼（以 agent 目前 checkout 內容為準）

- Repository：https://github.com/Poseidoncode/HarmonyApp
- `PromptField.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Models/PromptField.swift
- `PromptTemplate.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Models/PromptTemplate.swift
- `PromptRenderer.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Services/PromptRenderer.swift
- `TemplateStore.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Services/TemplateStore.swift
- `FocusTracker.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Services/FocusTracker.swift
- `ClipboardService.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Services/ClipboardService.swift
- `ContentView.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Views/ContentView.swift
- `HarmonyPromptsApp.swift`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/HarmonyPromptsApp.swift
- `templates.json`：https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/HarmonyPrompts/HarmonyPrompts/Resources/templates.json

### Microsoft 官方文件

- .NET Support Policy：https://dotnet.microsoft.com/en-us/platform/support/policy
- WPF Overview：https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
- `SetForegroundWindow`：https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
- `SendInput`：https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput
- `SetWinEventHook`：https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook
- .NET Single-file Deployment：https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview

---

## 11. 最終執行要求（一句話版）

**請在 `Poseidoncode/HarmonyApp` 現有 Repository 中保留所有 macOS SwiftUI 功能，依本 PLAN.md 新增可建置、可測試、可發行的 C# WPF Windows 原生 Harmony Prompts；以 JSON 與渲染結果一致、跨程式貼上安全、離線運作、CI 驗證及不破壞 Mac 版為最高優先順序，逐階段提交與記錄可驗證結果。**
