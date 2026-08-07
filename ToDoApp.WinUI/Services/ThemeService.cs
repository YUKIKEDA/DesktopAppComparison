using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using ToDoApp.WinUI.Models;

namespace ToDoApp.WinUI.Services;

public class ThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _themeFilePath;

    public ThemeService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _themeFilePath = Path.Combine(dataDirectory, "theme.json");
    }

    public string CurrentTheme { get; private set; } = "light";

    public string LoadTheme()
    {
        try
        {
            if (File.Exists(_themeFilePath))
            {
                var json = File.ReadAllText(_themeFilePath);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json, JsonOptions);
                if (settings != null && IsValidTheme(settings.Theme))
                {
                    CurrentTheme = settings.Theme.ToLowerInvariant();
                    return CurrentTheme;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
        }

        CurrentTheme = "light";
        return CurrentTheme;
    }

    public void SaveTheme(string theme)
    {
        if (!IsValidTheme(theme))
        {
            theme = "light";
        }

        CurrentTheme = theme.ToLowerInvariant();
        try
        {
            var settings = new ThemeSettings { Theme = CurrentTheme };
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_themeFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving theme: {ex.Message}");
        }
    }

    public ElementTheme ToElementTheme(string? theme = null)
    {
        var value = theme ?? CurrentTheme;
        return value == "dark" ? ElementTheme.Dark : ElementTheme.Light;
    }

    public void ApplyTheme(string theme, Window? window = null, FrameworkElement? root = null)
    {
        if (!IsValidTheme(theme))
        {
            theme = "light";
        }

        CurrentTheme = theme.ToLowerInvariant();
        var elementTheme = ToElementTheme(CurrentTheme);

        if (window?.Content is FrameworkElement windowRoot)
        {
            windowRoot.RequestedTheme = elementTheme;
        }

        if (root != null)
        {
            root.RequestedTheme = elementTheme;
        }

        if (Application.Current is App app && app.MainWindow != null && app.MainWindow != window)
        {
            if (app.MainWindow.Content is FrameworkElement mainRoot)
            {
                mainRoot.RequestedTheme = elementTheme;
            }
        }
    }

    public string ToggleTheme(Window? window = null, FrameworkElement? root = null)
    {
        var next = CurrentTheme == "dark" ? "light" : "dark";
        ApplyTheme(next, window, root);
        SaveTheme(next);
        return next;
    }

    private static bool IsValidTheme(string? theme) =>
        string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
}
