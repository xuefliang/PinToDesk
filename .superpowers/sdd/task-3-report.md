# Task 3 Report: ContentView — 导入导出工具栏按钮

## What I implemented

Replaced the `ShareLink` export button in the toolbar with separate **Import** and **Export** buttons, connected to `store.importFromMarkdown(_:)` and `store.exportToMarkdown()` from Task 1.

### Changes applied:

1. **Added `import UniformTypeIdentifiers`** at top of file
2. **Removed `markdownExport` computed property** (now uses `store.exportToMarkdown()`)
3. **Added state variables**: `showImporter`, `showExporter`, `importMessage`
4. **Replaced ShareLink** with Import (folder icon) + Export (share icon) buttons
5. **Added `.fileImporter` modifier** — reads `.txt` files, calls `store.importFromMarkdown(text)`, shows import count
6. **Added `.fileExporter` modifier** — exports via `TextFileDocument` wrapping `store.exportToMarkdown()`
7. **Added `.alert` modifier** — shows success/failure messages after import/export
8. **Added `TextFileDocument` struct** — `FileDocument` conformance for `.fileExporter`

## Files changed

- `PinToDesk-iOS/Views/ContentView.swift` — all the changes above

## Issues or concerns

None.
