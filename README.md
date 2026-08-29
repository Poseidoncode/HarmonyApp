# Harmony Prompts — macOS

Quick-insert AI prompt templates via a menu bar app. Works in Cursor, browser, Terminal, anywhere.

See [`HarmonyPrompts/README.md`](HarmonyPrompts/README.md) for details.

## Screenshots / 介面預覽

| 模板選擇與參數配置 (Template & Parameters) | 即時渲染預覽與一鍵注入 (Render & Copy/Paste) |
| :---: | :---: |
| <img src="Assets/Picture1.png" alt="Template Selection & Parameters" width="400" /> | <img src="Assets/Picture2.png" alt="Rendered Output & Copy Actions" width="400" /> |
| **Prompt 庫與動態變數配置**<br>瀏覽/搜尋 Prompt 範本，即時填寫動態參數欄位（如語言、程式碼片段、焦點等） | **即時輸出與快捷操作**<br>即時預覽 Rendered Prompt，支援一鍵複製（`⌘C`）或直接自動貼入目前使用中的應用程式（`⌘⌥V`） |

## Features / 功能特色

- ⚡️ **Menu Bar 快捷存取**：常駐 macOS 選單列，隨點隨用，適用於 Cursor、VS Code、瀏覽器、Terminal 等任何應用程式。
- 📝 **動態 Prompt 模板**：支援多變數（Variable）與自訂表單控件（文字輸入、多行文本、下拉選單等）。
- 👁️ **即時渲染預覽 (Live Preview)**：輸入參數時即時產生 Prompt，並即時統計字數與字元數。
- 🚀 **一鍵注入 (Copy & Paste)**：支援快速複製（`⌘C`）與自動前台應用貼上（`⌘⌥V`）。
- 📂 **靈活的 JSON 範本管理**：支援從 Harmony 匯入範本或直接編輯本機 `templates.json`。

## Quick start

```bash
just install
```

Click the menu bar bubble → pick template → **Copy** or **Copy & Paste**.

Run from **`/Applications/Harmony Prompts.app`** (not the `build/` folder) so Accessibility permission stays valid.

## Accessibility (Copy & Paste)

**Copy & Paste** needs **System Settings → Privacy & Security → Accessibility**.

Enable **Harmony Prompts** once. If you see duplicate entries, remove old ones pointing at `build/` paths.

Until permission is granted, use **Copy** + manual **⌘V**.

## Project layout

```
HarmonyPrompts/
├── HarmonyPrompts/          # SwiftUI app source
├── HarmonyPrompts.xcodeproj
└── scripts/                 # Icon build scripts
```
