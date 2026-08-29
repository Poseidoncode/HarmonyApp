# Harmony Prompts — macOS menu bar app

SwiftUI desktop app for quick AI prompt templates. Works in Cursor, browser, Terminal, anywhere.

## Screenshots / 介面預覽

| 模板選擇與參數配置 (Template & Parameters) | 即時渲染預覽與一鍵注入 (Render & Copy/Paste) |
| :---: | :---: |
| <img src="../Assets/Picture1.png" alt="Template Selection & Parameters" width="400" /> | <img src="../Assets/Picture2.png" alt="Rendered Output & Copy Actions" width="400" /> |
| **Prompt 庫與動態變數配置**<br>瀏覽/搜尋 Prompt 範本，即時填寫動態參數欄位 | **即時輸出與快捷操作**<br>即時預覽 Rendered Prompt，支援一鍵複製（`⌘C`）或自動貼上（`⌘⌥V`） |

## Build & run

```bash
just install
```

Or in Xcode: open `HarmonyPrompts.xcodeproj` → Run (⌘R).

## Usage

1. Click the **text bubble** icon in the menu bar
2. Pick a template, fill fields
3. **Copy** — paste manually with ⌘V anywhere
4. **Copy & Paste** — auto-pastes into the frontmost app (needs Accessibility permission)

Settings (Harmony Prompts → Settings…): templates folder, Accessibility help.

## Templates

- Default templates ship in the app bundle
- User copy: `~/Library/Application Support/HarmonyPrompts/templates.json`
- **Import JSON…** — load Harmony `templates.json` format
- **Open templates folder** — edit JSON directly

## Sync from Harmony

Use **Import JSON…** in the app with `../Harmony/resources/templates.json`, or copy that file to:

```
~/Library/Application Support/HarmonyPrompts/templates.json
```
