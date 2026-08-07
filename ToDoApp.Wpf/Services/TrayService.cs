using System.Drawing;
using System.Windows;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace ToDoApp.Wpf.Services
{
    public sealed class TrayService : IDisposable
    {
        private readonly Forms.NotifyIcon _notifyIcon;
        private bool _exitRequested;
        private bool _disposed;

        public TrayService()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("表示", null, (_, _) => ShowMainWindow());
            menu.Items.Add("終了", null, (_, _) => ExitApplication());

            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "Todo App",
                Visible = true,
                ContextMenuStrip = menu,
                Icon = LoadIcon()
            };
            _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
        }

        public bool ExitRequested => _exitRequested;

        public void ShowNotification(string title, string message)
        {
            if (_disposed)
            {
                return;
            }

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(3000);
        }

        public void ShowMainWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.MainWindow;
                if (window == null)
                {
                    return;
                }

                if (!window.IsVisible)
                {
                    window.Show();
                }

                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }

                window.Activate();
                window.Focus();
            });
        }

        public void ExitApplication()
        {
            _exitRequested = true;
            Application.Current.Dispatcher.Invoke(() =>
            {
                Dispose();
                Application.Current.Shutdown();
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        private static Icon LoadIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Assets/app.ico");
                var streamInfo = Application.GetResourceStream(uri);
                if (streamInfo?.Stream != null)
                {
                    return new Icon(streamInfo.Stream);
                }
            }
            catch
            {
                // Fall back to system icon.
            }

            return SystemIcons.Application;
        }
    }
}
