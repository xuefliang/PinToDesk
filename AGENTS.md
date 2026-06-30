# PinToDesk — AI 代理项目指南

> 本文件面向不了解本项目的 AI 编码代理。以下内容均来自项目实际文件，请勿凭假设推断。

---

## 项目简介

**PinToDesk** 是一个极致轻量的 Windows 桌面待办事项工具。

- 采用无边框、半透明磨砂风格窗口，悬浮于桌面之上。
- 支持待办项的添加、编辑、删除、拖拽排序。
- 数据以 Markdown 列表形式自动持久化到本地。
- 通过系统托盘图标常驻后台，支持显示/隐藏、置顶、鼠标穿透、开机自启等状态控制。

当前版本：`1.0.4`（见 `PinToDesk.csproj`）。

---

## 技术栈与运行架构

- **语言**：C#（使用可空引用类型 `Nullable=enable`、隐式 using `ImplicitUsings=enable`）
- **UI 框架**：WPF（`UseWPF=true`），XAML 定义主窗口与编辑弹窗
- **目标框架**：`.NET 8.0-windows`（`net8.0-windows`）
- **额外依赖**：
  - `System.Windows.Forms`（`UseWindowsForms=true`）：用于系统托盘 `NotifyIcon` 与右键菜单
  - `System.Drawing`：用于加载托盘图标
  - `user32.dll` P/Invoke：用于窗口移动边界限制与鼠标穿透（`WS_EX_TRANSPARENT` + `WM_NCHITTEST` 逻辑）
- **无第三方 NuGet 包依赖**：项目未引用任何外部 NuGet 包
- **包源**：`NuGet.Config` 配置了腾讯镜像 + nuget.org 官方源

### 运行环境要求

- 操作系统：Windows 10 / 11（64 位）
- 开发/构建：安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- 框架依赖版运行时需要预装 `.NET 8 Desktop Runtime`

---

## 目录结构与模块划分

```
PinToDesk/
├── App.xaml / App.xaml.cs          # 应用入口、初始化 MainWindow 与 TrayHelper
├── MainWindow.xaml / .xaml.cs      # 主窗口：待办列表、内联输入、置顶/穿透/关闭按钮、ResizeGrip
├── EditDialog.xaml / .xaml.cs      # 编辑待办弹窗
├── Models/
│   ├── AppSettings.cs              # 置顶与穿透状态配置模型
│   └── TodoItem.cs                 # 待办项数据模型（Id / Title / CreatedAt）
├── Services/
│   └── MarkdownStorage.cs          # 读写 %AppData%\PinToDesk\todos.md
├── Helpers/
│   └── TrayHelper.cs               # 系统托盘图标、右键菜单、开机自启注册表操作
├── Packaging/
│   ├── InstallerScript.iss         # Inno Setup 安装脚本
│   └── Placeholder.cs              # 仅作为目录占位符
├── ico/                            # 多分辨率图标源文件
├── PTD.ico                         # 应用与托盘图标（已嵌入资源）
├── PinToDesk.csproj                # 项目文件
├── PinToDesk.sln                   # Visual Studio 解决方案
├── NuGet.Config                    # NuGet 包源配置
└── README.md / 开发日志.md          # 项目说明与版本迭代记录
```

### 关键模块职责

| 模块 | 职责 |
|------|------|
| `App.xaml.cs` | 启动 `MainWindow`，构造 `TrayHelper` 并把回调注入主窗口；设置 `ShutdownMode.OnExplicitShutdown` |
| `MainWindow.xaml.cs` | 待办数据展示与操作、窗口拖动/缩放、置顶/穿透状态切换、Win32 边界限制、按钮命中测试 |
| `EditDialog` | 双击待办文本时弹出的编辑窗口，Enter 确认 / Esc 取消 |
| `MarkdownStorage` | 将待办保存为 `- 内容` 的 Markdown 文件，并从该格式加载 |
| `TrayHelper` | 托盘菜单、单/双击行为、开机自启注册表读写、菜单文字与主窗口状态同步 |

---

## 构建与运行命令

所有命令均在项目根目录执行。

### 开发调试

```bash
dotnet build
dotnet run
```

### 发布（自包含单文件，无需运行时）

```bash
dotnet publish PinToDesk.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:PublishReadyToRun=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "Publish/SelfContained"
```

### 发布（框架依赖单文件，体积更小）

```bash
dotnet publish PinToDesk.csproj -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true ^
  -o "Publish/FrameworkDependent"
```

> 注意：`^` 为 Windows CMD 换行符；在 PowerShell 中请改用 `` ` `` 或写成单行。

### 清理

```bash
dotnet clean
```

---

## 代码风格与约定

- **注释语言**：代码中的功能注释、README、开发日志均使用中文，新增注释请保持中文。
- **命名**：
  - 私有字段使用下划线前缀，如 `_items`、`_storage`、`_isPinned`
  - UI 控件 `x:Name` 使用 PascalCase，如 `PinBtn`、`TodoList`、`InlineEditBox`
  - 命名空间与项目根命名空间一致：`PinToDesk`
- **可空性**：项目启用可空引用类型，字段、属性、参数尽量明确可空标注（如 `TodoItem?`）。
- **ImplicitUsings**：隐式 using 已启用，通常无需手动写大量 `using System.*`；必要时使用别名消除歧义（如 `WinKey = System.Windows.Input.KeyEventArgs`）。
- **UI 样式**：全局样式集中在 `App.xaml`，包括 `TitleBarBtn`、`ActionBtn`、`InlineTextBox`、`NarrowScrollViewer` 等。
- **最小变更原则**：修改时只做必要改动，不要引入额外抽象或改变现有交互逻辑。

---

## 测试说明

- **本项目目前没有自动化测试项目**（没有 `*.Tests.csproj` 或 `xUnit`/`MSTest`/`NUnit` 引用）。
- 验证方式以**手动在 Windows 上运行**为主，重点检查：
  - 添加/编辑/删除待办项
  - 拖拽排序
  - 双击空白处呼出内联输入框，Enter/Esc 行为
  - 置顶、鼠标穿透、托盘菜单状态同步
  - 窗口拖动/缩放边界限制（多显示器场景）
  - 数据是否正确写入 `%AppData%\PinToDesk\todos.md`
- 修改后请至少执行 `dotnet build` 确保项目能编译通过。

---

## 数据、配置与持久化

### 待办数据

- 路径：`%AppData%\PinToDesk\todos.md`
- 格式：每行一个 Markdown 列表项

```markdown
- 完成周报
- 买牛奶
```

### 应用配置

- 路径：`%AppData%\PinToDesk\settings.json`
- 内容示例：

```json
{"IsPinned":false,"IsPassThrough":false}
```

### 开机自启

- 通过 `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` 写入当前 exe 路径实现
- 不需要管理员权限

---

## 部署与打包

1. 使用上述 `dotnet publish` 命令生成发布产物。
2. 若需要安装包，可使用 `Packaging/InstallerScript.iss`（Inno Setup 脚本）。
   - 脚本中引用的路径为 `..\bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish\PinToDesk.exe`，请确保发布路径与该脚本一致，或按需修改。
   - 安装包会在开始菜单与桌面（可选）创建快捷方式，并支持安装后自动启动。
3. 应用图标：`PTD.ico` 已作为 `EmbeddedResource` 嵌入，并设置为 `ApplicationIcon`，同时被 `TrayHelper` 加载为托盘图标。

---

## 安全注意事项

- **P/Invoke**：`MainWindow.xaml.cs` 调用 `user32.dll` 的 `GetWindowLong` / `SetWindowLong` / `GetCursorPos` 等 API 实现鼠标穿透与窗口边界限制。修改相关逻辑时需注意 32/64 位结构布局与 DPI 换算。
- **注册表操作**：`TrayHelper.SetAutoStart` / `IsAutoStartEnabled` 只读写当前用户（`HKCU`）启动项，不触碰系统级配置。
- **文件 IO**：应用仅读写 `%AppData%\PinToDesk` 目录下的 `todos.md` 与 `settings.json`。
- **单文件发布**：自包含版使用了 `IncludeNativeLibrariesForSelfExtract=true`，运行时会将 native 库解压到临时目录；分发时只需一个 `PinToDesk.exe`。
- **无网络请求**：项目代码中没有 HTTP/网络相关调用，不涉及远程服务或密钥。

---

## 给 AI 代理的简要 checklist

- [ ] 修改前确认在 Windows 环境或具备 .NET 8 SDK。
- [ ] 运行 `dotnet build` 验证编译。
- [ ] 保持中文注释风格，与现有代码一致。
- [ ] 不要修改用户交互逻辑，除非任务明确要求。
- [ ] 涉及托盘、置顶、穿透、注册表、文件持久化时，在真机/Windows 上手动验证。
- [ ] 不要提交 `bin/`、`obj/`、`Publish*/` 等构建输出（已加入 `.gitignore`）。
