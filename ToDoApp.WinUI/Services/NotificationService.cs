using System;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ToDoApp.WinUI.Services
{
    public static class NotificationService
    {
        private static bool _registered;

        public static void EnsureRegistered()
        {
            if (_registered)
            {
                return;
            }

            try
            {
                AppNotificationManager.Default.Register();
                _registered = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppNotification register failed: {ex.Message}");
            }
        }

        public static void Show(string title, string message)
        {
            try
            {
                EnsureRegistered();
                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppNotification show failed: {ex.Message}");
            }
        }
    }
}
