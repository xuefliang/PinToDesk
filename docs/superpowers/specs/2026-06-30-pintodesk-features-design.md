# PinToDesk 功能增强设计文档

> 设计日期：2026-06-30  
> 涉及功能：完成标记、导出、常驻桌面显示

---

## 1. 目标与范围

在保持现有轻量、无边框、托盘常驻风格的前提下，为 PinToDesk 增加三项能力：

1. **完成标记**：待办条目可标记为已完成，完成后从主列表移除，但仍保留在持久化数据中以便导出。
2. **导出**：支持将当前待办与已完成条目导出为文件。
3. **常驻桌面显示**：窗口始终位于桌面层（最底层），普通窗口在前显示；按 `Win+D` 或显示桌面时，todo 列表可见。

范围不包含：已完成列表的独立浏览/恢复 UI、云端同步、多文件格式导出、复杂动画。

---

## 2. 设计决策与方案

### 2.1 完成状态存储格式

**方案 A（推荐）：复用 Markdown 复选框语法**

- 未完成：`- [ ] 标题`
- 已完成：`- [x] 标题`
- 兼容旧数据：旧格式 `- 标题` 解析为未完成。

**方案 B：双文件存储**

- `todos.md` 存未完成，`completed.md` 存已完成。
- 更清晰，但破坏现有单文件习惯，且已完成条目与未完成条目在拖拽排序上失去统一性。

**选择方案 A**：改动最小，符合 Markdown 语义，向后兼容。

### 2.2 已完成条目是否可见

**方案 A（推荐）：主列表只显示未完成，已完成条目仅在导出时出现**

- 用户明确说“在代办中不再显示”。
- 不额外增加“已完成”标签页，保持界面极简。

**方案 B：提供 Active/Completed 切换标签**

- 功能更完整，但与用户“不再显示”的要求冲突，且增加 UI 复杂度。

**选择方案 A**。

### 2.3 导出格式

**方案 A（推荐）：导出为 Markdown 复选框列表**

- 与存储格式一致，用户可直接阅读或再次导入。
- 文件名默认 `todos_20260630.md`。

**方案 B：导出为 CSV/JSON**

- 结构化更好，但增加格式选择 UI，超出当前轻量定位。

**选择方案 A**。

### 2.4 常驻桌面显示实现

**方案 A（推荐）：Win32 窗口层级控制**

- 新增“桌面模式”状态，保存到 `settings.json`。
- 启用时：
  - 通过 `SetWindowPos(hwnd, HWND_BOTTOM, ...)` 将窗口置于最底层。
  - 忽略最小化消息（`WM_SYSCOMMAND` 的 `SC_MINIMIZE`），避免 `Win+D` 将其最小化。
  - 使用定时器周期性检查并维持 `HWND_BOTTOM`（约 500ms），防止其他窗口激活时本窗口被带到前面。
- 禁用时：恢复普通浮动窗口行为。

**方案 B：将窗口设为桌面子窗口**

- 将 `Owner` 设为 `Progman/WorkerW`，理论上最符合“桌面挂件”。
- 但 WPF 窗口作为桌面子窗口在 DPI、焦点、穿透、拖拽缩放方面风险高，稳定性差。

**选择方案 A**。

---

## 3. 数据模型变更

### 3.1 `TodoItem`

```csharp
public class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; } = null;
}
```

### 3.2 `AppSettings`

```csharp
public class AppSettings
{
    public bool IsPinned { get; set; } = false;
    public bool IsPassThrough { get; set; } = false;
    public bool IsDesktopMode { get; set; } = false;
}
```

---

## 4. UI 变更

### 4.1 主列表条目模板

在每行待办前增加一个复选框：

- 点击复选框 → 标记为已完成 → 从主列表淡出/移除。
- 保持现有编辑、删除按钮在悬停时显示。
- 为避免按钮过多，编辑与删除可略微缩小或调整间距。

### 4.2 标题栏按钮

在现有“置顶 / 穿透 / 关闭”右侧新增：

- **导出按钮**（例如 `⤓`）：点击弹出 `SaveFileDialog` 导出。
- **桌面模式按钮**（例如 `🖥` 或 `▤`）：切换常驻桌面显示。

### 4.3 托盘菜单

增加：

- 「导出」菜单项。
- 「桌面模式」菜单项（带勾选）。

---

## 5. 数据流

### 5.1 标记完成

```
用户点击复选框
  → TodoItem.IsCompleted = true; CompletedAt = now
  → 集合视图过滤条件生效，条目从主列表移除
  → MarkdownStorage.SaveTodos(_items) 写入 todos.md
```

### 5.2 加载

```
启动 → MarkdownStorage.LoadTodos()
  → 解析 - [x] 为已完成，其他为未完成
  → 主列表绑定 CollectionView 过滤 IsCompleted == false
```

### 5.3 导出

```
用户点击导出
  → SaveFileDialog 选择路径
  → 写入所有条目（未完成在前，已完成在后）
  → 格式：- [ ] / - [x] 标题
```

### 5.4 桌面模式

```
切换桌面模式
  → 更新 _isDesktopMode
  → 保存 settings.json
  → 若启用：SetWindowPos(HWND_BOTTOM) + 启动维持定时器
  → 若禁用：停止定时器，恢复普通 Z 序
```

---

## 6. 错误处理

- 文件保存/导出失败：静默忽略或显示短暂提示，不影响主流程。
- 解析旧格式失败：按未完成处理。
- 桌面模式 Win32 调用失败：回退到普通模式，避免窗口无法操作。

---

## 7. 验证方式

- `dotnet build` 通过。
- 手动验证：
  - 添加待办 → 点击复选框 → 条目从列表消失 → 文件中出现 `- [x]`。
  - 导出文件包含未完成的 `- [ ]` 和已完成的 `- [x]`。
  - 开启桌面模式后，打开其他窗口，todo 列表保持在最底层；`Win+D` 后 todo 列表显示在桌面上。
  - 重启应用后状态（置顶/穿透/桌面模式）正确恢复。

---

## 8. 不引入的变更

- 不增加已完成列表的独立浏览/撤销界面。
- 不增加导出格式选择。
- 不修改现有拖拽排序、内联输入、置顶、穿透的核心交互。
