using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace ToDoApp.Avalonia.Services;

public class WindowGeometry
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public static class WindowGeometryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Apply(Window window, string settingsPath, double defaultWidth, double defaultHeight)
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return;
            }

            var json = File.ReadAllText(settingsPath);
            var geometry = JsonSerializer.Deserialize<WindowGeometry>(json, JsonOptions);
            if (geometry == null || geometry.Width < 100 || geometry.Height < 100)
            {
                return;
            }

            var position = new PixelPoint((int)Math.Round(geometry.X), (int)Math.Round(geometry.Y));
            if (!IsOnAnyScreen(window, position, geometry.Width, geometry.Height))
            {
                return;
            }

            window.Width = geometry.Width;
            window.Height = geometry.Height;
            window.Position = position;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
        }
        catch
        {
            window.Width = defaultWidth;
            window.Height = defaultHeight;
        }
    }

    public static void Save(Window window, string settingsPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var geometry = new WindowGeometry
            {
                X = window.Position.X,
                Y = window.Position.Y,
                Width = window.Width,
                Height = window.Height
            };

            var json = JsonSerializer.Serialize(geometry, JsonOptions);
            File.WriteAllText(settingsPath, json);
        }
        catch
        {
            // 位置保存の失敗はアプリ動作を妨げない
        }
    }

    private static bool IsOnAnyScreen(Window window, PixelPoint position, double width, double height)
    {
        var screens = window.Screens?.All;
        if (screens == null || screens.Count == 0)
        {
            return true;
        }

        var rect = new PixelRect(
            position,
            new PixelSize(
                Math.Max(1, (int)Math.Round(width)),
                Math.Max(1, (int)Math.Round(height))));

        return screens.Any(screen => screen.WorkingArea.Intersects(rect));
    }
}
