# PinToDesk 功能增强实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为 PinToDesk 增加完成标记、导出、常驻桌面显示三项功能。

**Architecture:** 在现有 WPF 主窗口与托盘菜单上扩展：数据模型增加完成状态，存储改用 Markdown 复选框语法，主列表通过 CollectionView 过滤隐藏已完成条目；新增导出按钮/菜单调用 SaveFileDialog；桌面模式通过 Win32 `SetWindowPos(HWND_BOTTOM)` 与忽略最小化消息实现。

**Tech Stack:** C# 12 / .NET 8 / WPF / Windows Forms (NotifyIcon) / user32.dll P/Invoke

## Global Constraints

- 项目无自动化测试，验证以手动运行 + `dotnet build` 为主。
- 保持中文注释风格，新增注释使用中文。
- 私有字段使用下划线前缀；UI 控件 `x:Name` 使用 PascalCase。
- 命名空间统一为 `PinToDesk`。
- 最小变更原则：不引入额外抽象，不修改现有交互逻辑（拖拽排序、内联输入、置顶、穿透）。
- 可空引用类型已启用，字段/属性需明确可空标注。

---

## File Structure

| 文件 | 变更 | 职责 |
|------|------|------|
| `Models/TodoItem.cs` | 修改 | 增加 `IsCompleted` 与 `CompletedAt` |
| `Models/AppSettings.cs` | 修改 | 增加 `IsDesktopMode` |
| `Services/MarkdownStorage.cs` | 修改 | 解析/保存 `- [ ]` / `- [x]` 语法 |
| `MainWindow.xaml` | 修改 | 列表项加复选框、标题栏加导出/桌面模式按钮 |
| `MainWindow.xaml.cs` | 修改 | 过滤已完成、完成点击、导出、桌面模式 Win32 逻辑 |
| `Helpers/TrayHelper.cs` | 修改 | 托盘菜单增加导出、桌面模式 |
| `App.xaml` | 可选修改 | 如有需要新增按钮样式 |

---

### Task 1: 扩展数据模型

**Files:**
- Modify: `Models/TodoItem.cs`
- Modify: `Models/AppSettings.cs`

**Interfaces:**
- Produces: `TodoItem.IsCompleted`, `TodoItem.CompletedAt`, `AppSettings.IsDesktopMode`

- [ ] **Step 1: 修改 `TodoItem.cs`**

```csharp
using System;

namespace PinToDesk.Models
{
    public class TodoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; } = null;
    }
}
```

- [ ] **Step 2: 修改 `AppSettings.cs`**

```csharp
namespace PinToDesk.Models
{
    public class AppSettings
    {
        public bool IsPinned { get; set; } = false;
        public bool IsPassThrough { get; set; } = false;
        public bool IsDesktopMode { get; set; } = false;
    }
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译通过，无错误。

- [ ] **Step 4: Commit**

```bash
git add Models/TodoItem.cs Models/AppSettings.cs
git commit -m "feat(models): add IsCompleted/CompletedAt and IsDesktopMode"
```

---

### Task 2: 存储层支持复选框语法

**Files:**
- Modify: `Services/MarkdownStorage.cs`

**Interfaces:**
- Consumes: `TodoItem.IsCompleted`
- Produces: `LoadTodos()` 可解析 `- [x]`，保存时写入 `- [ ]` / `- [x]`

- [ ] **Step 1: 修改 `LoadTodos` 解析逻辑**

在 `MarkdownStorage.cs` 中，将循环体改为：

```csharp
var trimmed = line.Trim();
if (string.IsNullOrWhiteSpace(trimmed)) continue;

bool isCompleted = false;
string title = trimmed;

if (trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase))
{
    isCompleted = true;
    title = trimmed.Substring(6);
}
else if (trimmed.StartsWith("- [ ] "))
{
    title = trimmed.Substring(6);
}
else if (trimmed.StartsWith("- "))
{
    title = trimmed.Substring(2);
}

todos.Add(new TodoItem
{
    Title = title,
    IsCompleted = isCompleted,
    CompletedAt = isCompleted ? DateTime.Now : null
});
```

- [ ] **Step 2: 修改 `SaveTodos` 写入逻辑**

```csharp
public void SaveTodos(IEnumerable<TodoItem> items)
{
    var sb = new StringBuilder();
    foreach (var item in items)
    {
        var marker = item.IsCompleted ? "- [x]" : "- [ ]";
        sb.AppendLine($"{marker} {item.Title}");
    }
    File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
}
```

- [ ] **Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译通过。

- [ ] **Step 4: Commit**

```bash
git add Services/MarkdownStorage.cs
git commit -m "feat(storage): use markdown checkbox syntax for completed state"
```

---

### Task 3: 主窗口 UI 增加复选框与功能按钮

**Files:**
- Modify: `MainWindow.xaml`

**Interfaces:**
- Produces: `CompleteBtn`（复选框按钮）、`ExportBtn`（导出按钮）、`DesktopModeBtn`（桌面模式按钮）

- [ ] **Step 1: 标题栏增加导出和桌面模式按钮**

在 `TitleButtons` StackPanel 中，于 `PassThroughBtn` 之后、`CloseBtn` 之前插入：

```xml
<Button x:Name="ExportBtn"
        Content="⤓"
        Style="{StaticResource TitleBarBtn}"
        Click="ExportBtn_Click"
        ToolTip="导出"/>

<Button x:Name="DesktopModeBtn"
        Content="🖥"
        Style="{StaticResource TitleBarBtn}"
        Click="DesktopModeBtn_Click"
        ToolTip="常驻桌面显示"/>
```

- [ ] **Step 2: 列表项模板增加完成复选框**

在 `DataTemplate` 的 Grid 中，将列定义改为 5 列：

```xml
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="Auto"/>
</Grid.ColumnDefinitions>
```

在 Ellipse 之前插入复选框按钮：

```xml
<!-- 完成复选框 -->
<Button x:Name="CompleteBtn"
        Grid.Column="0"
        Content="☐"
        Style="{StaticResource ActionBtn}"
        Tag="{Binding Id}"
        Click="CompleteBtn_Click"
        Width="24" Height="24"
        Margin="0,0,6,0"
        ToolTip="标记完成"/>
```

将原有 Ellipse 移到 `Grid.Column="1"`，文本移到 `Grid.Column="2"`，编辑按钮 `Grid.Column="3"`，删除按钮 `Grid.Column="4"`。

- [ ] **Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译通过（事件处理函数缺失属预期，下一步补充）。

- [ ] **Step 4: Commit**

```bash
git add MainWindow.xaml
git commit -m "feat(ui): add complete checkbox, export and desktop mode buttons"
```

---

### Task 4: 主窗口逻辑 — 完成标记与列表过滤

**Files:**
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `TodoItem.IsCompleted`
- Produces: `CompleteBtn_Click`, `ApplyTodoFilter()`

- [ ] **Step 1: 引入集合视图命名空间**

在文件顶部增加：

```csharp
using System.ComponentModel;
using System.Windows.Data;
```

- [ ] **Step 2: 增加集合视图字段与过滤方法**

在字段区域增加：

```csharp
private ICollectionView? _todoView;
```

在构造函数中，设置 `ItemsSource` 后添加：

```csharp
_todoView = CollectionViewSource.GetDefaultView(_items);
_todoView.Filter = o => o is TodoItem item && !item.IsCompleted;
```

增加方法：

```csharp
private void ApplyTodoFilter()
{
    _todoView?.Refresh();
    UpdateEmptyPlaceholder();
}
```

- [ ] **Step 3: 实现完成按钮点击事件**

```csharp
private void CompleteBtn_Click(object sender, RoutedEventArgs e)
{
    var id = (Guid)((WinButton)sender).Tag;
    var item = _items.FirstOrDefault(i => i.Id == id);
    if (item == null) return;

    item.IsCompleted = true;
    item.CompletedAt = DateTime.Now;
    _storage.SaveTodos(_items);
    ApplyTodoFilter();
}
```

- [ ] **Step 4: 更新 `UpdateEmptyPlaceholder` 以反映过滤后状态**

```csharp
private void UpdateEmptyPlaceholder()
{
    int activeCount = _items.Count(i => !i.IsCompleted);
    EmptyPlaceholder.Visibility =
        activeCount == 0 ? Visibility.Visible : Visibility.Collapsed;
}
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build`
Expected: 编译通过。

- [ ] **Step 6: 手动验证**

Run: `dotnet run`
Expected：
- 添加待办 → 列表显示，左侧出现复选框。
- 点击复选框 → 条目从列表消失。
- `%AppData%\PinToDesk\todos.md` 中对应行变为 `- [x] 标题`。

- [ ] **Step 7: Commit**

```bash
git add MainWindow.xaml.cs
git commit -m "feat(todo): mark items complete and filter from main list"
```

---

### Task 5: 导出功能

**Files:**
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `_items`, `_storage`
- Produces: `ExportBtn_Click`

- [ ] **Step 1: 实现导出方法**

在 `MainWindow.xaml.cs` 中增加：

```csharp
private void ExportBtn_Click(object sender, RoutedEventArgs e)
{
    var dialog = new Microsoft.Win32.SaveFileDialog
    {
        FileName = $"todos_{DateTime.Now:yyyyMMdd}",
        DefaultExt = ".md",
        Filter = "Markdown 文件 (*.md)|*.md|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
    };

    if (dialog.ShowDialog() != true) return;

    try
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PinToDesk 导出");
        sb.AppendLine();
        sb.AppendLine("## 待办事项");
        foreach (var item in _items.Where(i => !i.IsCompleted))
        {
            sb.AppendLine($"- [ ] {item.Title}");
        }

        sb.AppendLine();
        sb.AppendLine("## 已完成");
        foreach (var item in _items.Where(i => i.IsCompleted).OrderBy(i => i.CompletedAt))
        {
            sb.AppendLine($"- [x] {item.Title}");
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"导出失败：{ex.Message}", "导出", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

- [ ] **Step 2: 编译验证**

Run: `dotnet build`
Expected: 编译通过。

- [ ] **Step 3: 手动验证**

Run: `dotnet run`
Expected：
- 点击导出按钮 → 弹出保存对话框。
- 选择路径保存后，文件包含“待办事项”与“已完成”两个分区。

- [ ] **Step 4: Commit**

```bash
git add MainWindow.xaml.cs
git commit -m "feat(export): export active and completed todos to markdown"
```

---

### Task 6: 常驻桌面显示模式

**Files:**
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `AppSettings.IsDesktopMode`
- Produces: `DesktopModeBtn_Click`, `ToggleDesktopMode`, `WndProc` 对 `WM_SYSCOMMAND` 的处理

- [ ] **Step 1: 增加 Win32 常量与字段**

在 P/Invoke 区域增加：

```csharp
private const int WM_SYSCOMMAND = 0x0112;
private const int SC_MINIMIZE   = 0xF020;
private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

[DllImport("user32.dll")]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
    int X, int Y, int cx, int cy, uint uFlags);

private const uint SWP_NOSIZE       = 0x0001;
private const uint SWP_NOMOVE       = 0x0002;
private const uint SWP_NOACTIVATE   = 0x0010;
private const uint SWP_SHOWWINDOW   = 0x0040;
```

在字段区域增加：

```csharp
private bool _isDesktopMode = false;
private System.Windows.Threading.DispatcherTimer? _desktopModeTimer;
```

- [ ] **Step 2: 修改 `WndProc` 处理最小化与移动**

```csharp
private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    if (msg == WM_MOVING)
    {
        // 原有逻辑保持不变
        ...
    }

    if (msg == WM_SYSCOMMAND && _isDesktopMode)
    {
        int cmd = wParam.ToInt32() & 0xFFF0;
        if (cmd == SC_MINIMIZE)
        {
            // 桌面模式下忽略最小化，保持窗口在桌面层可见
            handled = true;
            return IntPtr.Zero;
        }
    }

    return IntPtr.Zero;
}
```

- [ ] **Step 3: 实现桌面模式切换逻辑**

```csharp
private void DesktopModeBtn_Click(object sender, RoutedEventArgs e)
{
    ToggleDesktopMode();
}

internal void ToggleDesktopModeFromTray()
{
    ToggleDesktopMode();
}

private void ToggleDesktopMode()
{
    _isDesktopMode = !_isDesktopMode;

    if (_isDesktopMode)
    {
        DesktopModeBtn.Content = "🖥";
        DesktopModeBtn.ToolTip = "关闭桌面模式";
        SetTitleButtonsOpacity(1);
        StartDesktopModeTimer();
        SendToBottom();
    }
    else
    {
        DesktopModeBtn.Content = "🖵";
        DesktopModeBtn.ToolTip = "常驻桌面显示";
        if (!_isPinned && !_isPassThrough) SetTitleButtonsOpacity(0);
        StopDesktopModeTimer();
    }

    SaveSettings();
}

private void StartDesktopModeTimer()
{
    if (_desktopModeTimer == null)
    {
        _desktopModeTimer = new System.Windows.Threading.DispatcherTimer();
        _desktopModeTimer.Interval = TimeSpan.FromMilliseconds(500);
        _desktopModeTimer.Tick += (s, e) => SendToBottom();
    }
    _desktopModeTimer.Start();
}

private void StopDesktopModeTimer()
{
    _desktopModeTimer?.Stop();
}

private void SendToBottom()
{
    var hwnd = new WindowInteropHelper(this).Handle;
    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
        SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
}
```

- [ ] **Step 4: 在 `LoadSettings` 中恢复桌面模式**

在 `LoadSettings` 的 `try` 块中，读取 `settings.IsDesktopMode`：

```csharp
_isDesktopMode = settings.IsDesktopMode;
```

在 `LoadSettings` 末尾应用状态：

```csharp
if (_isDesktopMode)
{
    DesktopModeBtn.Content = "🖥";
    DesktopModeBtn.ToolTip = "关闭桌面模式";
    StartDesktopModeTimer();
}
else
{
    DesktopModeBtn.Content = "🖵";
    DesktopModeBtn.ToolTip = "常驻桌面显示";
}
```

- [ ] **Step 5: 在 `SaveSettings` 中保存桌面模式**

```csharp
var settings = new AppSettings
{
    IsPinned = _isPinned,
    IsPassThrough = _isPassThrough,
    IsDesktopMode = _isDesktopMode
};
```

- [ ] **Step 6: 更新按钮命中测试**

在 `IsOverInteractiveButton` 中增加 `DesktopModeBtn` 与 `ExportBtn`：

```csharp
return IsScreenPointInElement(PinBtn, screenX, screenY) ||
       IsScreenPointInElement(PassThroughBtn, screenX, screenY) ||
       IsScreenPointInElement(ExportBtn, screenX, screenY) ||
       IsScreenPointInElement(DesktopModeBtn, screenX, screenY) ||
       IsScreenPointInElement(CloseBtn, screenX, screenY);
```

- [ ] **Step 7: 编译验证**

Run: `dotnet build`
Expected: 编译通过。

- [ ] **Step 8: 手动验证**

Run: `dotnet run`
Expected：
- 点击桌面模式按钮 → 窗口被置底。
- 打开其他窗口 → todo 列表保持在最底层。
- 按 `Win+D` → todo 列表仍显示在桌面上。
- 关闭桌面模式 → 恢复普通浮动窗口。

- [ ] **Step 9: Commit**

```bash
git add MainWindow.xaml.cs
git commit -m "feat(desktop-mode): keep window on desktop layer"
```

---

### Task 7: 托盘菜单同步

**Files:**
- Modify: `Helpers/TrayHelper.cs`
- Modify: `App.xaml.cs`
- Modify: `MainWindow.xaml.cs`

**Interfaces:**
- Consumes: `MainWindow.ToggleDesktopModeFromTray`, `MainWindow.IsDesktopMode`
- Produces: 托盘「导出」「桌面模式」菜单项

- [ ] **Step 1: 扩展 `TrayHelper` 构造函数签名**

增加导出与桌面模式回调参数：

```csharp
public TrayHelper(Window window, System.Windows.Application app,
                  Action togglePin,          Func<bool> getIsPinned,
                  Action togglePassThrough,  Func<bool> getIsPassThrough,
                  Action export,             Action toggleDesktopMode,
                  Func<bool> getIsDesktopMode)
```

保存这些委托到私有字段，并增加菜单项：

```csharp
private readonly ToolStripMenuItem _itemExport;
private readonly ToolStripMenuItem _itemDesktopMode;
```

在菜单中合适位置插入：

```csharp
_itemExport = new ToolStripMenuItem("导出");
_itemExport.Click += (s, e) => export();

_itemDesktopMode = new ToolStripMenuItem("桌面模式");
_itemDesktopMode.CheckOnClick = true;
_itemDesktopMode.Checked = getIsDesktopMode();
_itemDesktopMode.CheckedChanged += (s, e) =>
{
    if (_itemDesktopMode.Checked != getIsDesktopMode())
        toggleDesktopMode();
};
```

- [ ] **Step 2: 增加同步方法**

```csharp
public void SyncDesktopModeMenuItem()
{
    _itemDesktopMode.Checked = _getIsDesktopMode();
}
```

- [ ] **Step 3: 修改 `MainWindow` 暴露状态与回调**

在 `MainWindow.xaml.cs` 中增加：

```csharp
public bool IsDesktopMode => _isDesktopMode;
```

在 `ToggleDesktopMode` 末尾增加：

```csharp
_tray?.SyncDesktopModeMenuItem();
```

- [ ] **Step 4: 修改 `App.xaml.cs` 注入新回调**

```csharp
_tray = new TrayHelper(
    win,
    this,
    win.TogglePinFromTray,
    () => win.IsPinned,
    win.TogglePassThroughFromTray,
    () => win.IsPassThrough,
    win.ExportFromTray,          // 新增
    win.ToggleDesktopModeFromTray,
    () => win.IsDesktopMode
);
```

在 `MainWindow.xaml.cs` 中增加：

```csharp
public void ExportFromTray() => ExportBtn_Click(null, new RoutedEventArgs());
```

- [ ] **Step 5: 编译验证**

Run: `dotnet build`
Expected: 编译通过。

- [ ] **Step 6: 手动验证**

Run: `dotnet run`
Expected：
- 托盘右键菜单包含「导出」与「桌面模式」。
- 点击「桌面模式」与标题栏按钮状态同步。

- [ ] **Step 7: Commit**

```bash
git add Helpers/TrayHelper.cs App.xaml.cs MainWindow.xaml.cs
git commit -m "feat(tray): add export and desktop mode menu items"
```

---

### Task 8: 最终集成与验证

**Files:**
- All modified files

- [ ] **Step 1: 完整构建**

Run: `dotnet build`
Expected: 0 errors, 0 warnings（原有 warning 除外）。

- [ ] **Step 2: 端到端手动测试**

Run: `dotnet run`

检查清单：
- [ ] 添加 3 个待办，列表显示正常，左侧有复选框。
- [ ] 点击其中一个复选框，条目消失，主列表剩 2 个。
- [ ] 查看 `%AppData%\PinToDesk\todos.md`，已完成的行是 `- [x]`，未完成的是 `- [ ]`。
- [ ] 关闭并重新启动应用，已完成的条目不出现，未完成的条目仍在。
- [ ] 点击导出按钮，保存为 `.md`，文件包含待办和已完成两个分区。
- [ ] 开启桌面模式，打开记事本并最大化，todo 列表保持在最底层。
- [ ] 按 `Win+D`，todo 列表显示在桌面上。
- [ ] 关闭桌面模式，窗口恢复普通浮动。
- [ ] 托盘菜单中的「桌面模式」勾选状态与标题栏按钮一致。

- [ ] **Step 3: Commit 任何最终调整**

```bash
git add -A
git commit -m "feat: integrate completed state, export and desktop mode"
```

---

## Self-Review

### Spec Coverage

| 需求 | 实现任务 |
|------|----------|
| 待办完成后点击标记完成 | Task 4 `CompleteBtn_Click` |
| 完成后不在代办中显示 | Task 4 `ICollectionView.Filter` |
| 导出待办和已办 | Task 5 `ExportBtn_Click` |
| 常驻桌面显示 | Task 6 `ToggleDesktopMode` + Win32 |

### Placeholder Scan

无 TBD/TODO/"implement later"/"添加适当错误处理" 等占位描述。每步均包含完整代码或命令。

### Type Consistency

- `TodoItem.IsCompleted` 为 `bool`，`CompletedAt` 为 `DateTime?`。
- `AppSettings.IsDesktopMode` 为 `bool`。
- `TrayHelper` 新增 `Action export`、`Action toggleDesktopMode`、`Func<bool> getIsDesktopMode`。
- `MainWindow` 新增 `public bool IsDesktopMode => _isDesktopMode;`。

无命名不一致。
