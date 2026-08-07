using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ToDoApp.WinUI.Models;
using ToDoApp.WinUI.Services;
using Windows.Graphics;

namespace ToDoApp.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private const int DefaultWidth = 1400;
        private const int DefaultHeight = 900;

        private readonly DataService _dataService = new();

        public MainWindow()
        {
            InitializeComponent();
            RestoreWindowPosition();
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            SaveWindowPosition();
        }

        private string GetWindowSettingsPath() =>
            Path.Combine(_dataService.GetDataDirectory(), "window.json");

        private void RestoreWindowPosition()
        {
            var appWindow = AppWindow;
            var settings = LoadWindowSettings();

            if (settings == null)
            {
                appWindow.Resize(new SizeInt32(DefaultWidth, DefaultHeight));
                return;
            }

            var width = settings.Width > 0 ? settings.Width : DefaultWidth;
            var height = settings.Height > 0 ? settings.Height : DefaultHeight;
            var position = new PointInt32(settings.X, settings.Y);

            if (!IsPositionOnScreen(position, width, height))
            {
                appWindow.Resize(new SizeInt32(DefaultWidth, DefaultHeight));
                return;
            }

            appWindow.Move(position);
            appWindow.Resize(new SizeInt32(width, height));
        }

        private void SaveWindowPosition()
        {
            try
            {
                var appWindow = AppWindow;
                var settings = new WindowSettings
                {
                    X = appWindow.Position.X,
                    Y = appWindow.Position.Y,
                    Width = appWindow.Size.Width,
                    Height = appWindow.Size.Height
                };

                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(GetWindowSettingsPath(), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving window position: {ex.Message}");
            }
        }

        private WindowSettings? LoadWindowSettings()
        {
            try
            {
                var path = GetWindowSettingsPath();
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<WindowSettings>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading window position: {ex.Message}");
                return null;
            }
        }

        private bool IsPositionOnScreen(PointInt32 position, int width, int height)
        {
            try
            {
                var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
                var workArea = displayArea.WorkArea;

                var windowRight = position.X + Math.Min(width, 50);
                var windowBottom = position.Y + Math.Min(height, 50);

                return windowRight > workArea.X
                    && windowBottom > workArea.Y
                    && position.X < workArea.X + workArea.Width
                    && position.Y < workArea.Y + workArea.Height;
            }
            catch
            {
                return true;
            }
        }
    }
}
