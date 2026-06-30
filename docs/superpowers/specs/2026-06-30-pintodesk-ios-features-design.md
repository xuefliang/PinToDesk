# PinToDesk iOS 功能增强设计

## 概述

在现有 iOS 版（PinToDesk-iOS）基础上，增加编辑按钮、导入/导出功能、导入去重、应用图标配置。保持与 WPF 版数据格式互通。

## 涉及文件

| 文件 | 改动 |
|------|------|
| `PinToDesk-iOS/Views/TodoRowView.swift` | 每行加编辑按钮 |
| `PinToDesk-iOS/Services/StorageService.swift` | 新增导入/导出方法 |
| `PinToDesk-iOS/Views/ContentView.swift` | 工具栏加导入/导出按钮 |
| `PinToDesk-iOS/Resources/Assets.xcassets/AppIcon.appiconset/` | 新建应用图标集 |
| `PinToDesk-iOS/project.yml` | 配置 AppIcon 名称 |
| `.github/workflows/ios-build.yml` | 更新触发分支 |

## 1. TodoRowView — 每行编辑按钮

在现有上下移按钮右侧增加编辑（铅笔）按钮。

```
[☐] ● [标题文字]    [✏️] [▲] [▼]
```

- 点击 ✏️ 按钮设置 `editingItem` 并弹出 EditTodoView
- 按钮样式：`font(.system(size: 12))`，`foregroundColor(.secondary.opacity(0.6))`，尺寸 24x24
- 双击编辑功能保留不变

## 2. StorageService — 导入/导出

### 导出格式 (exportToMarkdown)

```markdown
# PinToDesk 导出

## 待办事项
- [ ] 标题1
- [ ] 标题2

## 已完成
- [x] 标题3
```

与 WPF 版 `ExportBtn_Click` 生成格式完全一致。

### 导入逻辑 (importFromMarkdown)

1. 读取输入的 Markdown 文本
2. 逐行扫描以 `- [ ] ` 或 `- [x] ` 开头的行
3. 提取 `[ ]`/`[x]` 状态和标题
4. **去重**：对每个待导入标题，检查 `items` 中是否有 `title.caseInsensitiveCompare(newTitle) == .orderedSame`
5. 重复的跳过，不重复的创建 `TodoItem` 并追加
6. 返回实际导入的数量

## 3. ContentView — 工具栏

工具栏布局：`[标题] [导入] [导出] [眼睛]`

### 导入按钮
- 图标：`folder`
- 点击弹出 `UTType.plainText` 文件选择器
- 读取文件内容 → `store.importFromMarkdown(text)`
- Alert 提示"导入了 N 条新待办"

### 导出按钮（替换现有 ShareLink）
- 图标：`square.and.arrow.up`
- 点击弹出文件选择器选择保存位置
- 写入 `store.exportToMarkdown()` 到文件
- 使用 `UTType.plainText` 指定文件类型

## 4. 应用图标

- 来源：`ico.png`（100x100 PNG，由用户提供）
- 已生成所有 iOS 必需尺寸并放入 `AppIcon.appiconset/`
- `project.yml` 已配置 `ASSETCATALOG_COMPILER_APPICON_NAME: "AppIcon"`

## 5. GitHub Actions

`ios-build.yml` 当前监听 `push` 到 `ios` 或 `main` 分支。当代码推送到 `ios` 分支时自动触发 macOS 构建，输出未签名的 `.app.zip`。

## 数据互通保证

| 方面 | WPF 版 | iOS 版 |
|------|--------|--------|
| 导出格式 | `# PinToDesk 导出` + `## 待办事项` + `## 已完成` | 完全一致 |
| 条目格式 | `- [ ]` / `- [x]` + 标题 | 完全一致 |
| 导入去重 | `HashSet<string>(OrdinalIgnoreCase)` | `caseInsensitiveCompare` |
| 导入文件 | `.md` 文本文件 | `.md` / `.txt` 文本文件 |

两端生成的 Markdown 文件可直接互换导入。
