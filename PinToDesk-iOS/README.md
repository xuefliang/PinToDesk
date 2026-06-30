# PinToDesk iOS

PinToDesk 的 iPhone 版本，使用 SwiftUI 构建，视觉风格与 Windows 版一致（磨砂半透明背景）。

## 功能

- 添加待办事项
- 点击 ☐ 标记完成（已完成项折叠显示）
- 双击待办文本编辑
- 左滑删除
- 上移 / 下移按钮调整排序
- 数据持久化（UserDefaults）
- 磨砂玻璃（ultraThinMaterial）背景

## 构建要求

- macOS 14+ (Sonoma) 或更新版本
- Xcode 15.4+
- Apple Developer 账号（用于签名和导出 IPA）
- iOS 17.0+ 目标设备

## 构建步骤

### 1. 在 Xcode 中创建项目

```bash
# 方式 A：直接用 Xcode 打开文件夹
open PinToDesk-iOS

# 方式 B：在 Xcode 中 File > New > Project
#   - iOS > App
#   - Interface: SwiftUI, Language: Swift
#   - 取消选中所有复选框
# 然后将以下文件拖入项目导航器：
```

将以下文件拖入 Xcode 项目导航器（勾选 "Copy items if needed"）：

| 文件 | 说明 |
|------|------|
| `PinToDeskApp.swift` | 入口 |
| `Models/TodoItem.swift` | 数据模型 |
| `Services/StorageService.swift` | 存储+逻辑 |
| `Views/ContentView.swift` | 主列表视图 |
| `Views/TodoRowView.swift` | 单行视图 |
| `Views/EditTodoView.swift` | 编辑弹窗 |
| `Info.plist` | 配置 |
| `Resources/Assets.xcassets` | 资源目录 |

### 2. 配置签名

在 Xcode 中：
1. 选择项目 target > Signing & Capabilities
2. 选择你的 Team（Apple ID）
3. 修改 `Bundle Identifier`（如 `com.yourname.PinToDesk`）

### 3. 构建并运行

- 选择 iOS Simulator 或真机
- `⌘R` 运行
- 或 `Product > Archive` 后导出 IPA

### 4. 导出 IPA

1. `Product > Archive`
2. 在 Organizer 中点击 "Distribute App"
3. 选择 "iOS App Store" 或 "Enterprise" 或 "Development"
4. 按向导完成导出

## 项目结构

```
PinToDesk-iOS/
├── PinToDeskApp.swift          # App 入口
├── Models/
│   └── TodoItem.swift          # 待办项模型
├── Services/
│   └── StorageService.swift    # 数据持久化 + 业务逻辑
├── Views/
│   ├── ContentView.swift       # 主界面（列表 + 输入）
│   ├── TodoRowView.swift       # 单行待办项
│   └── EditTodoView.swift      # 编辑弹窗
├── Resources/
│   └── Assets.xcassets/        # 资源目录
├── Info.plist                  # 应用配置
└── README.md                   # 本文件
```
