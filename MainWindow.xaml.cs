using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using PinToDesk.Helpers;
using PinToDesk.Models;
using PinToDesk.Services;
using WinPoint       = System.Windows.Point;
using WinKey         = System.Windows.Input.KeyEventArgs;
using WinMouse       = System.Windows.Input.MouseEventArgs;
using WinButton      = System.Windows.Controls.Button;
using MessageBox       = System.Windows.MessageBox;

namespace PinToDesk
{
    public partial class MainWindow : Window
    {
        // ══════════════════════════════════════════════
        // 字段
        // ══════════════════════════════════════════════
        private readonly ObservableCollection<TodoItem> _items = new();
        private readonly MarkdownStorage _storage;

        // 集合视图（过滤已完成条目）
        private ICollectionView? _todoView;

        // 拖拽排序（上移/下移一位）
        private WinPoint  _dragStart;
        private TodoItem? _dragItem;

        // 窗口调整大小
        private bool _isResizing;
        private WinPoint _resizeStart;
        private double   _resizeStartW, _resizeStartH;

        // 置顶 / 桌面模式（独立状态）
        private bool _isPinned      = false;
        private bool _isDesktopMode = false;
        private bool _allowHide     = false;   // 用户主动隐藏时设为 true

        // 托盘引用（用于同步状态）
        private TrayHelper? _tray;
        public bool IsPinned      => _isPinned;
        public bool IsDesktopMode => _isDesktopMode;
        public void SetTray(TrayHelper tray) => _tray = tray;
        public void ExportFromTray() => ExportBtn_Click(null, new RoutedEventArgs());

        // Win32 结构与接口定义
        private const int WM_MOVING             = 0x0216;
        private const int WM_SYSCOMMAND         = 0x0112;
        private const int WM_WINDOWPOSCHANGING  = 0x0046;
        private const int WM_SHOWWINDOW         = 0x0018;
        private const int SC_MINIMIZE           = 0xF020;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE     = 0x0001;
        private const uint SWP_NOMOVE     = 0x0002;
        private const uint SWP_NOZORDER   = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_HIDEWINDOW = 0x0080;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        // ══════════════════════════════════════════════
        // 构造函数
        // ══════════════════════════════════════════════
        public MainWindow()
        {
            InitializeComponent();

            new WindowInteropHelper(this).EnsureHandle();

            _storage = new MarkdownStorage();
            foreach (var item in _storage.LoadTodos())
                _items.Add(item);
            TodoList.ItemsSource = _items;

            // 集合视图过滤已完成条目
            _todoView = CollectionViewSource.GetDefaultView(_items);
            _todoView.Filter = o => o is TodoItem item && !item.IsCompleted;

            _items.CollectionChanged += (s, e) => UpdateEmptyPlaceholder();

            var area = SystemParameters.WorkArea;
            Left   = area.Right - Width - 20;
            Height = Width * 1.3;
            Top    = area.Top + 20;

            LoadSettings();

            UpdateEmptyPlaceholder();

            Loaded += (s, e) =>
            {
                UpdateEmptyPlaceholder();
                SetTitleButtonsOpacity((_isPinned || _isDesktopMode) ? 1 : 0);
            };

            IsVisibleChanged += (s, e) =>
            {
                if (!IsVisible) _allowHide = false;
            };
        }

        // ══════════════════════════════════════════════
        // 窗口初始化：挂钩 WndProc
        // ══════════════════════════════════════════════
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 阻止系统最小化（如 Win+D 通过 ShowWindow(SW_MINIMIZE) 调用）
            if (msg == WM_SYSCOMMAND)
            {
                int cmd = wParam.ToInt32() & 0xFFF0;
                if (cmd == SC_MINIMIZE && !_allowHide)
                {
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            // 阻止系统隐藏（如 Win+D 对工具窗口使用 SW_HIDE）
            if (msg == WM_SHOWWINDOW && wParam == IntPtr.Zero && !_allowHide)
            {
                handled = true;
                return IntPtr.Zero;
            }

            if (msg == WM_WINDOWPOSCHANGING)
            {
                var wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS))!;

                // 始终阻止系统隐藏窗口（如 Win+D），但放行用户主动隐藏
                if ((wp.flags & SWP_HIDEWINDOW) != 0 && !_allowHide)
                {
                    handled = true;
                    return IntPtr.Zero;
                }

                if (_isDesktopMode)
                {
                    // 强制窗口保持在桌面层
                    if ((wp.flags & SWP_NOZORDER) == 0 && wp.hwndInsertAfter != HWND_BOTTOM)
                    {
                        wp.hwndInsertAfter = HWND_BOTTOM;
                        Marshal.StructureToPtr(wp, lParam, true);
                    }
                }
                return IntPtr.Zero;
            }

            if (msg == WM_MOVING)
            {
                POINT mousePos;
                GetCursorPos(out mousePos);
                var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(mousePos.X, mousePos.Y));
                var area = screen.WorkingArea;

                var rect = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT))!;
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (rect.Left < area.Left)
                {
                    rect.Left = area.Left;
                    rect.Right = rect.Left + width;
                }
                else if (rect.Right > area.Right)
                {
                    rect.Right = area.Right;
                    rect.Left = rect.Right - width;
                }

                if (rect.Top < area.Top)
                {
                    rect.Top = area.Top;
                    rect.Bottom = rect.Top + height;
                }
                else if (rect.Bottom > area.Bottom)
                {
                    rect.Bottom = area.Bottom;
                    rect.Top = rect.Bottom - height;
                }

                Marshal.StructureToPtr(rect, lParam, true);
                handled = true;
                return new IntPtr(1);
            }
            return IntPtr.Zero;
        }

        // ══════════════════════════════════════════════
        // TitleBar 区域悬停：控制 TitleBar 按钮显示
        // ══════════════════════════════════════════════
        private void TitleBar_MouseEnter(object sender, WinMouse e) => SetTitleButtonsOpacity(1);
        private void TitleBar_MouseLeave(object sender, WinMouse e)
        {
            if (!_isPinned && !_isDesktopMode) SetTitleButtonsOpacity(0);
        }

        private void SetTitleButtonsOpacity(double opacity)
        {
            PinBtn.Opacity         = opacity;
            ExportBtn.Opacity      = opacity;
            DesktopModeBtn.Opacity = opacity;
            CloseBtn.Opacity       = opacity;
        }

        // ResizeGrip 区域悬停：控制 Grip 显示
        private void ResizeGrip_MouseEnter(object sender, WinMouse e)
        {
            if (!_isPinned) ResizeGripArea.Opacity = 1;
        }
        private void ResizeGrip_MouseLeave(object sender, WinMouse e) => ResizeGripArea.Opacity = 0;

        // ══════════════════════════════════════════════
        // 标题栏拖动
        // ══════════════════════════════════════════════
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        // ══════════════════════════════════════════════
        // 标题栏按钮
        // ══════════════════════════════════════════════
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            _allowHide = true;
            Hide();
        }

        public void TrayHide()
        {
            _allowHide = true;
            Hide();
        }

        private void PinBtn_Click(object sender, RoutedEventArgs e)
        {
            TogglePinState();
            _tray?.SyncPinMenuItem();
        }

        internal void TogglePinFromTray()
        {
            TogglePinState();
        }

        private void TogglePinState()
        {
            _isPinned    = !_isPinned;
            this.Topmost = _isPinned;

            if (_isPinned)
            {
                PinBtn.Content = "📍";
                PinBtn.ToolTip = "取消置顶";
                SetTitleButtonsOpacity(1);
            }
            else
            {
                PinBtn.Content = "📌";
                PinBtn.ToolTip = "置顶";
                if (!_isDesktopMode) SetTitleButtonsOpacity(0);
            }
            SaveSettings();
        }

        // ══════════════════════════════════════════════
        // 常驻桌面显示模式
        // ══════════════════════════════════════════════
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
                SendToBottom();
            }
            else
            {
                DesktopModeBtn.Content = "🖵";
                DesktopModeBtn.ToolTip = "常驻桌面显示";
                if (!_isPinned) SetTitleButtonsOpacity(0);
            }

            _tray?.SyncDesktopModeMenuItem();
            SaveSettings();
        }

        private void SendToBottom()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
                SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        // ══════════════════════════════════════════════
        // 右下角自定义 ResizeGrip
        // ══════════════════════════════════════════════
        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing   = true;
            _resizeStart  = e.GetPosition(null);
            _resizeStartW = Width;
            _resizeStartH = Height;
            ((UIElement)sender).CaptureMouse();
            ((UIElement)sender).MouseMove        += ResizeGrip_MouseMove;
            ((UIElement)sender).MouseLeftButtonUp += ResizeGrip_MouseLeftButtonUp;
            e.Handled = true;
        }

        private void ResizeGrip_MouseMove(object sender, WinMouse e)
        {
            if (!_isResizing) return;
            var pos   = e.GetPosition(null);
            var delta = pos - _resizeStart;

            double newW = Math.Max(MinWidth, _resizeStartW + delta.X);
            double newH = Math.Max(MinHeight, _resizeStartH + delta.Y);

            var area = GetCurrentScreenWorkArea();
            if (Left + newW > area.Right)
            {
                newW = area.Right - Left;
            }
            if (Top + newH > area.Bottom)
            {
                newH = area.Bottom - Top;
            }

            Width  = newW;
            Height = newH;
        }

        private Rect GetCurrentScreenWorkArea()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
            var area = screen.WorkingArea;
            var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            double dpiScaleX = dpi.DpiScaleX;
            double dpiScaleY = dpi.DpiScaleY;

            return new Rect(
                area.Left / dpiScaleX,
                area.Top / dpiScaleY,
                area.Width / dpiScaleX,
                area.Height / dpiScaleY
            );
        }

        private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ((UIElement)sender).ReleaseMouseCapture();
            ((UIElement)sender).MouseMove        -= ResizeGrip_MouseMove;
            ((UIElement)sender).MouseLeftButtonUp -= ResizeGrip_MouseLeftButtonUp;
        }

        // ══════════════════════════════════════════════
        // 双击空白处：显示内联输入框
        // ══════════════════════════════════════════════
        private void Window_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InlineInputArea.Visibility != Visibility.Visible) return;

            var src = e.OriginalSource as DependencyObject;
            while (src != null)
            {
                if (src == PinBtn || src == ExportBtn || src == DesktopModeBtn || src == CloseBtn)
                {
                    return;
                }
                if (src == InlineEditBox)
                {
                    return;
                }
                src = System.Windows.Media.VisualTreeHelper.GetParent(src);
            }

            CommitInlineInput();
            e.Handled = true;
        }

        private void TodoList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var src = e.OriginalSource as DependencyObject;
            while (src != null)
            {
                if (src is System.Windows.Controls.Button) return;
                src = System.Windows.Media.VisualTreeHelper.GetParent(src);
            }

            if (e.OriginalSource is FrameworkElement fe && fe.DataContext is TodoItem item)
            {
                ShowEditDialog(item);
                return;
            }

            ShowInlineInput();
        }

        private void ShowInlineInput()
        {
            InlineInputArea.Visibility = Visibility.Visible;
            InlineEditBox.Text         = string.Empty;
            InlineEditBox.Focus();
        }

        private void HideInlineInput()
        {
            InlineInputArea.Visibility = Visibility.Collapsed;
            InlineEditBox.Text         = string.Empty;
        }

        private void InlineEditBox_KeyDown(object sender, WinKey e)
        {
            if (e.Key == Key.Enter)
            {
                CommitInlineInput();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideInlineInput();
                e.Handled = true;
            }
        }

        private void InlineEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            HideInlineInput();
        }

        private void CommitInlineInput()
        {
            var text = InlineEditBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                _items.Add(new TodoItem { Title = text });
                _storage.SaveTodos(_items);
                if (_items.Count > 0)
                    TodoList.ScrollIntoView(_items[^1]);
            }
            HideInlineInput();
        }

        private void UpdateEmptyPlaceholder()
        {
            int activeCount = _items.Count(i => !i.IsCompleted);
            EmptyPlaceholder.Visibility =
                activeCount == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ══════════════════════════════════════════════
        // 编辑 / 删除 / 完成 / 导出
        // ══════════════════════════════════════════════
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

        private void ApplyTodoFilter()
        {
            _todoView?.Refresh();
            UpdateEmptyPlaceholder();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var id   = (Guid)((WinButton)sender).Tag;
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null) ShowEditDialog(item);
        }

        private void ShowEditDialog(TodoItem item)
        {
            var dlg = new EditDialog(item.Title) { Owner = this };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResultText))
            {
                item.Title = dlg.ResultText;
                var idx = _items.IndexOf(item);
                _items.RemoveAt(idx);
                _items.Insert(idx, item);
                _storage.SaveTodos(_items);
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var id   = (Guid)((WinButton)sender).Tag;
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null) { _items.Remove(item); _storage.SaveTodos(_items); }
        }

        // ══════════════════════════════════════════════
        // 拖拽排序（上移/下移一位）
        // ══════════════════════════════════════════════
        private void TodoList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var src = e.OriginalSource as DependencyObject;
            while (src != null)
            {
                if (src is FrameworkElement fe && fe.Tag as string == "DRAG_HANDLE")
                {
                    _dragStart = e.GetPosition(null);
                    _dragItem = fe.DataContext as TodoItem;
                    TodoList.CaptureMouse();
                    return;
                }
                src = System.Windows.Media.VisualTreeHelper.GetParent(src);
            }
            _dragItem = null;
        }

        private void TodoList_PreviewMouseMove(object sender, WinMouse e)
        {
            if (_dragItem == null) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragItem = null;
                TodoList.ReleaseMouseCapture();
                return;
            }

            var pos = e.GetPosition(null);
            var delta = pos - _dragStart;
            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

            int oldIdx = _items.IndexOf(_dragItem);
            if (delta.Y < 0 && oldIdx > 0)
                _items.Move(oldIdx, oldIdx - 1);
            else if (delta.Y > 0 && oldIdx < _items.Count - 1)
                _items.Move(oldIdx, oldIdx + 1);

            _storage.SaveTodos(_items);
            _dragItem = null;
            TodoList.ReleaseMouseCapture();
        }

        private void TodoList_LostMouseCapture(object sender, WinMouse e)
        {
            _dragItem = null;
        }

        private void LoadSettings()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "PinToDesk");
                var settingsPath = Path.Combine(folder, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _isPinned = settings.IsPinned;
                        _isDesktopMode = settings.IsDesktopMode;
                    }
                }
                else
                {
                    _isPinned = false;
                    _isDesktopMode = false;
                }
            }
            catch
            {
                _isPinned = false;
                _isDesktopMode = false;
            }

            this.Topmost = _isPinned;
            if (_isPinned)
            {
                PinBtn.Content = "📍";
                PinBtn.ToolTip = "取消置顶";
            }
            else
            {
                PinBtn.Content = "📌";
                PinBtn.ToolTip = "置顶";
            }

            if (_isDesktopMode)
            {
                DesktopModeBtn.Content = "🖥";
                DesktopModeBtn.ToolTip = "关闭桌面模式";
            }
            else
            {
                DesktopModeBtn.Content = "🖵";
                DesktopModeBtn.ToolTip = "常驻桌面显示";
            }
        }

        private void SaveSettings()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "PinToDesk");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                var settingsPath = Path.Combine(folder, "settings.json");

                var settings = new AppSettings
                {
                    IsPinned = _isPinned,
                    IsDesktopMode = _isDesktopMode
                };
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsPath, json, Encoding.UTF8);
            }
            catch
            {
                // 忽略保存错误
            }
        }
    }
}
