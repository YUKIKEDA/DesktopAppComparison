using System;
using System.Runtime.InteropServices;

namespace ToDoApp.Avalonia.Services;

/// <summary>
/// Shows a Windows balloon tip via a temporary NotifyIcon (Shell_NotifyIcon).
/// </summary>
public static class WindowsBalloonNotification
{
    private const int NimAdd = 0x00000000;
    private const int NimModify = 0x00000001;
    private const int NimDelete = 0x00000002;
    private const int NifMessage = 0x00000001;
    private const int NifIcon = 0x00000002;
    private const int NifTip = 0x00000004;
    private const int NifInfo = 0x00000010;
    private const int NisHidden = 0x00000001;

    public static void Show(string title, string message)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var hwnd = GetDesktopWindow();
        var hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION

        var data = new NotifyIconData
        {
            cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
            hWnd = hwnd,
            uID = 1,
            uFlags = NifMessage | NifIcon | NifTip | NifInfo,
            uCallbackMessage = 0,
            hIcon = hIcon,
            szTip = "Todo App",
            dwState = 0,
            dwStateMask = 0,
            szInfo = Truncate(message, 255),
            uTimeoutOrVersion = 3000,
            szInfoTitle = Truncate(title, 63),
            dwInfoFlags = 1 // NIIF_INFO
        };

        Shell_NotifyIcon(NimAdd, ref data);
        data.uFlags = NifInfo;
        Shell_NotifyIcon(NimModify, ref data);

        // Remove shortly after so we don't leave a stray tray icon.
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(3500);
            Shell_NotifyIcon(NimDelete, ref data);
        });
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

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
}
