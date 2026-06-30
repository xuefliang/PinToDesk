# PinToDesk iOS 功能增强 — 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 iOS 版增加编辑按钮、导入/导出、导入去重、应用图标

**Architecture:** 在现有 SwiftUI + ObservableObject 架构上增量开发，不改动整体结构。StorageService 新增导入导出方法，TodoRowView 加编辑按钮，ContentView 工具栏改造。

**Tech Stack:** Swift 5.10, SwiftUI, iOS 17.0+

## 全局约束

1. 无第三方依赖
2. 导入格式与 WPF 版一致：`- [ ] 标题` / `- [x] 标题`
3. 导入去重：按标题忽略大小写比较
4. 导出格式含 `# PinToDesk 导出` / `## 待办事项` / `## 已完成` 头部
5. 所有 UI 文字保持中文
6. 目标 iOS 17.0+

---

### Task 1: StorageService — 导入导出方法

**Files:**
- Modify: `PinToDesk-iOS/Services/StorageService.swift`

**Interfaces:**
- Consumes: `TodoItem` (existing model)
- Produces: `func importFromMarkdown(_ text: String) -> Int`, `func exportToMarkdown() -> String`

- [ ] **Step 1: 添加 importFromMarkdown 方法**

```swift
func importFromMarkdown(_ text: String) -> Int {
    var count = 0
    let existingTitles = Set(items.map { $0.title.lowercased() })
    let lines = text.components(separatedBy: .newlines)

    for line in lines {
        let trimmed = line.trimmingCharacters(in: .whitespaces)
        var title: String
        var isCompleted = false

        if trimmed.hasPrefix("- [x] ") || trimmed.hasPrefix("- [X] ") {
            title = String(trimmed.dropFirst(6))
            isCompleted = true
        } else if trimmed.hasPrefix("- [ ] ") {
            title = String(trimmed.dropFirst(6))
        } else if trimmed.hasPrefix("- ") {
            title = String(trimmed.dropFirst(2))
        } else {
            continue
        }

        title = title.trimmingCharacters(in: .whitespaces)
        guard !title.isEmpty else { continue }
        guard !existingTitles.contains(title.lowercased()) else { continue }

        var item = TodoItem(title: title)
        item.isCompleted = isCompleted
        if isCompleted { item.completedAt = Date() }
        items.append(item)
        existingTitles.insert(title.lowercased())
        count += 1
    }

    if count > 0 { save() }
    return count
}
```

- [ ] **Step 2: 添加 exportToMarkdown 方法**

```swift
func exportToMarkdown() -> String {
    var md = "# PinToDesk 导出\n\n"
    md += "## 待办事项\n"
    for item in activeItems {
        md += "- [ ] \(item.title)\n"
    }
    let completed = completedItems
    if !completed.isEmpty {
        md += "\n## 已完成\n"
        for item in completed {
            md += "- [x] \(item.title)\n"
        }
    }
    return md
}
```

- [ ] **Step 3: 验证编译**

```bash
# 无法在 Windows 编译 iOS 项目，需要推送到 GitHub Actions 验证
# 检查语法：确保没有拼写错误
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat(ios): add import/export methods to TodoStore"
```

---

### Task 2: TodoRowView — 增加编辑按钮

**Files:**
- Modify: `PinToDesk-iOS/Views/TodoRowView.swift`
- Modify: `PinToDesk-iOS/Views/ContentView.swift`

**Interfaces:**
- Consumes: `onEdit` callback passed from ContentView
- Produces: Inline edit button on each row

- [ ] **Step 1: 在 TodoRowView 添加 onEdit 参数和编辑按钮**

```swift
import SwiftUI

struct TodoRowView: View {
    @EnvironmentObject private var store: TodoStore
    let item: TodoItem
    var onEdit: (() -> Void)?
    @State private var showActions = false

    var body: some View {
        HStack(spacing: 8) {
            Button(action: {
                withAnimation(.easeInOut(duration: 0.2)) {
                    store.toggleComplete(item)
                }
            }) {
                Image(systemName: "square")
                    .font(.system(size: 16))
                    .foregroundColor(.secondary.opacity(0.7))
                    .frame(width: 28, height: 28)
            }
            .buttonStyle(.plain)

            Circle()
                .fill(Color.secondary.opacity(0.35))
                .frame(width: 5, height: 5)
                .padding(.trailing, 4)

            Text(item.title)
                .font(.system(size: 15))
                .foregroundColor(.primary)
                .frame(maxWidth: .infinity, alignment: .leading)
                .lineLimit(10)

            // 编辑按钮
            Button(action: { onEdit?() }) {
                Image(systemName: "pencil")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary.opacity(0.6))
                    .frame(width: 24, height: 24)
            }
            .buttonStyle(.plain)

            // 上移
            Button(action: { store.moveUp(item) }) {
                Image(systemName: "chevron.up")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary.opacity(0.6))
                    .frame(width: 24, height: 24)
            }
            .buttonStyle(.plain)

            // 下移
            Button(action: { store.moveDown(item) }) {
                Image(systemName: "chevron.down")
                    .font(.system(size: 11, weight: .bold))
                    .foregroundColor(.secondary.opacity(0.6))
                    .frame(width: 24, height: 24)
            }
            .buttonStyle(.plain)
        }
        .padding(.vertical, 7)
        .padding(.horizontal, 12)
        .background(
            RoundedRectangle(cornerRadius: 8, style: .continuous)
                .fill(Color.clear)
        )
        .contentShape(Rectangle())
        .onTapGesture(count: 2) {
            showActions = true
        }
    }
}
```

- [ ] **Step 2: 在 ContentView 的 ForEach 中传入 onEdit**

修改 `PinToDesk-iOS/Views/ContentView.swift` 第 75 行：

```swift
TodoRowView(item: item, onEdit: { editingItem = item; showEditSheet = true })
```

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "feat(ios): add edit button on each todo row"
```

---

### Task 3: ContentView — 导入导出工具栏按钮

**Files:**
- Modify: `PinToDesk-iOS/Views/ContentView.swift`

**Interfaces:**
- Consumes: `store.importFromMarkdown(_:) -> Int`, `store.exportToMarkdown() -> String` (from Task 1)
- Produces: 导入/导出功能入口

- [ ] **Step 1: 替换 ShareLink 为导入/导出按钮并添加文件选择器状态**

在 `ContentView` 结构体中添加状态：

```swift
@State private var showImporter = false
@State private var showExporter = false
@State private var importDocument: Data? = nil
@State private var importMessage: String? = nil
```

修改工具栏 HStack：

```swift
HStack {
    Text("TodoList")
        .font(.system(size: 17, weight: .semibold))
        .foregroundColor(.primary)
    Spacer()

    // 导入按钮
    Button(action: { showImporter = true }) {
        Image(systemName: "folder")
            .font(.system(size: 14))
            .foregroundColor(.secondary)
    }
    .frame(width: 32, height: 32)

    // 导出按钮
    Button(action: { showExporter = true }) {
        Image(systemName: "square.and.arrow.up")
            .font(.system(size: 14))
            .foregroundColor(.secondary)
    }
    .frame(width: 32, height: 32)

    Button(action: { showCompleted.toggle() }) {
        Image(systemName: showCompleted ? "eye.slash" : "eye")
            .font(.system(size: 15))
            .foregroundColor(.secondary)
    }
    .frame(width: 32, height: 32)
}
```

移除旧的 `markdownExport` 计算属性（不再需要，因为导出现在走文件选择器）。

- [ ] **Step 2: 添加 fileImporter 和 fileExporter 修饰符**

添加到 `ZStack` 的闭合括号后：

```swift
.fileImporter(
    isPresented: $showImporter,
    allowedContentTypes: [.plainText],
    allowsMultipleSelection: false
) { result in
    switch result {
    case .success(let urls):
        guard let url = urls.first else { return }
        guard url.startAccessingSecurityScopedResource() else { return }
        defer { url.stopAccessingSecurityScopedResource() }
        if let data = try? Data(contentsOf: url),
           let text = String(data: data, encoding: .utf8) {
            let count = store.importFromMarkdown(text)
            importMessage = "导入了 \(count) 条新待办"
        }
    case .failure:
        importMessage = "导入失败"
    }
}
.fileExporter(
    isPresented: $showExporter,
    document: TextFileDocument(text: store.exportToMarkdown()),
    contentType: .plainText,
    defaultFilename: "PinToDesk-导出"
) { result in
    switch result {
    case .success: importMessage = "导出成功"
    case .failure: importMessage = "导出失败"
    }
}
.alert("提示", isPresented: .init(
    get: { importMessage != nil },
    set: { if !$0 { importMessage = nil } }
)) {
    Button("确定") { importMessage = nil }
} message: {
    Text(importMessage ?? "")
}
```

- [ ] **Step 3: 添加 TextFileDocument 类型（在 ContentView.swift 底部）**

```swift
struct TextFileDocument: FileDocument {
    static var readableContentTypes: [UTType] { [.plainText] }
    var text: String

    init(text: String) { self.text = text }

    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents,
              let string = String(data: data, encoding: .utf8)
        else { throw CocoaError(.fileReadCorruptFile) }
        text = string
    }

    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper {
        FileWrapper(regularFileWithContents: Data(text.utf8))
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat(ios): add import/export toolbar buttons"
```
