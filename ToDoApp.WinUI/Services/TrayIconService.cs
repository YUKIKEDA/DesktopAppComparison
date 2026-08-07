using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ToDoApp.WinUI.Services
{
    /// <summary>
    /// Best-effort Win32 system tray icon for WinUI (no first-class tray API).
    /// </summary>
    public sealed class TrayIconService : IDisposable
    {
        private const uint WmTrayIcon = 0x8001;
        private const uint WmLButtonDblClk = 0x0203;
        private const uint WmRButtonUp = 0x0205;
        private const uint WmCommand = 0x0111;
        private const uint NimAdd = 0x00000000;
        private const uint NimDelete = 0x00000002;
        private const uint NifMessage = 0x00000001;
        private const uint NifIcon = 0x00000002;
        private const uint NifTip = 0x00000004;
        private const int IdShow = 1001;
        private const int IdExit = 1002;

        private readonly Window _window;
        private readonly IntPtr _hwnd;
        private readonly SubclassProc _subclassProc;
        private readonly IntPtr _subclassProcPtr;
        private IntPtr _hIcon;
        private bool _added;
        private bool _disposed;
        private bool _exitRequested;

        public TrayIconService(Window window)
        {
            _window = window;
            _hwnd = WindowNative.GetWindowHandle(window);
            _subclassProc = WndProc;
            _subclassProcPtr = Marshal.GetFunctionPointerForDelegate(_subclassProc);
            SetWindowSubclass(_hwnd, _subclassProcPtr, IntPtr.Zero, IntPtr.Zero);

            _hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION
            AddTrayIcon();
        }

        public bool ExitRequested => _exitRequested;

        public event Action? ShowRequested;
        public event Action? ExitRequestedEvent;

        public void RequestExit()
        {
            _exitRequested = true;
            ExitRequestedEvent?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            RemoveTrayIcon();
            RemoveWindowSubclass(_hwnd, _subclassProcPtr, IntPtr.Zero);
            // System icon from LoadIcon(IDI_APPLICATION) must not be destroyed.
            _hIcon = IntPtr.Zero;
            GC.KeepAlive(_subclassProc);
        }

        private void AddTrayIcon()
        {
            var data = CreateNotifyIconData();
            _added = Shell_NotifyIcon(NimAdd, ref data);
        }

        private void RemoveTrayIcon()
        {
            if (!_added)
            {
                return;
            }

            var data = CreateNotifyIconData();
            Shell_NotifyIcon(NimDelete, ref data);
            _added = false;
        }

        private NotifyIconData CreateNotifyIconData()
        {
            return new NotifyIconData
            {
                cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NifMessage | NifIcon | NifTip,
                uCallbackMessage = WmTrayIcon,
                hIcon = _hIcon,
                szTip = "Todo App"
            };
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            if (msg == WmTrayIcon)
            {
                var mouseMsg = (uint)lParam.ToInt64() & 0xFFFF;
                if (mouseMsg == WmLButtonDblClk)
                {
                    ShowRequested?.Invoke();
                }
                else if (mouseMsg == WmRButtonUp)
                {
                    ShowContextMenu();
                }
            }
            else if (msg == WmCommand)
            {
                var id = wParam.ToInt32() & 0xFFFF;
                if (id == IdShow)
                {
                    ShowRequested?.Invoke();
                }
                else if (id == IdExit)
                {
                    RequestExit();
                }
            }

            return DefSubclassProc(hWnd, msg, wParam, lParam);
        }

        private void ShowContextMenu()
        {
            var menu = CreatePopupMenu();
            AppendMenu(menu, 0, (UIntPtr)IdShow, "表示");
            AppendMenu(menu, 0, (UIntPtr)IdExit, "終了");

            GetCursorPos(out var point);
            SetForegroundWindow(_hwnd);
            TrackPopupMenu(menu, 0x0100 /* TPM_RIGHTBUTTON */, point.X, point.Y, 0, _hwnd, IntPtr.Zero);
            DestroyMenu(menu);
        }

        private delegate IntPtr SubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, IntPtr pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
        private static extern bool RemoveWindowSubclass(IntPtr hWnd, IntPtr pfnSubclass, IntPtr uIdSubclass);

        [DllImport("comctl32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }
    }
}
