using System.IO;
using System.Text.Json;
using System.Windows;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Services;

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

    public void ApplyTheme(string theme)
    {
        if (!IsValidTheme(theme))
        {
            theme = "light";
        }

        CurrentTheme = theme.ToLowerInvariant();
        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        var dict = new ResourceDictionary
        {
            Source = new Uri(
                CurrentTheme == "dark"
                    ? "Resources/Themes/Dark.xaml"
                    : "Resources/Themes/Light.xaml",
                UriKind.Relative)
        };

        // Replace theme dictionary (keep index 0 as theme)
        var merged = app.Resources.MergedDictionaries;
        if (merged.Count > 0 && merged[0].Source != null &&
            merged[0].Source.OriginalString.Contains("Themes/", StringComparison.OrdinalIgnoreCase))
        {
            merged[0] = dict;
        }
        else
        {
            merged.Insert(0, dict);
        }
    }

    public string ToggleTheme()
    {
        var next = CurrentTheme == "dark" ? "light" : "dark";
        ApplyTheme(next);
        SaveTheme(next);
        return next;
    }

    private static bool IsValidTheme(string? theme) =>
        string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);
}
