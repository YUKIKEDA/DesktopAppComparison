using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Services;

public class DataService : IDataService
{
    private static readonly string AppDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ToDoApp.Avalonia",
        "data"
    );
    private static readonly string DataFile = Path.Combine(AppDataDirectory, "project.json");
    private static readonly string WindowSettingsFile = Path.Combine(AppDataDirectory, "window.json");

    private readonly Window? _window;

    public string DataDirectory => AppDataDirectory;
    public string WindowSettingsPath => WindowSettingsFile;

    public DataService(Window? window = null)
    {
        _window = window;
    }

    private static JsonSerializerOptions CreateReadOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
    };

    private static JsonSerializerOptions CreateWriteOptions() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
    };

    public async Task<ProjectData> LoadDataAsync()
    {
        try
        {
            if (!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            if (File.Exists(DataFile))
            {
                var json = await File.ReadAllTextAsync(DataFile);
                var data = JsonSerializer.Deserialize<ProjectData>(json, CreateReadOptions());
                return data ?? new ProjectData();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"データの読み込みに失敗しました: {ex.Message}");
        }

        return new ProjectData();
    }

    public async Task SaveDataAsync(ProjectData data)
    {
        try
        {
            if (!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            var json = JsonSerializer.Serialize(data, CreateWriteOptions());
            await File.WriteAllTextAsync(DataFile, json);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"データの保存に失敗しました: {ex.Message}");
            throw;
        }
    }

    public async Task ExportDataAsync(ProjectData data)
    {
        try
        {
            if (_window == null) return;

            var topLevel = TopLevel.GetTopLevel(_window);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "データをエクスポート",
                SuggestedFileName = "project.json",
                FileTypeChoices =
                [
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = ["*.json"]
                    }
                ]
            });

            if (file != null)
            {
                var json = JsonSerializer.Serialize(data, CreateWriteOptions());
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"エクスポートに失敗しました: {ex.Message}");
        }
    }

    public async Task<ProjectData?> ImportDataAsync()
    {
        try
        {
            if (_window == null) return null;

            var topLevel = TopLevel.GetTopLevel(_window);
            if (topLevel == null) return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "データをインポート",
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON Files")
                    {
                        Patterns = ["*.json"]
                    }
                ],
                AllowMultiple = false
            });

            if (files.Count > 0 && files[0] != null)
            {
                var path = files[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    return await ImportFromPathAsync(path);
                }

                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                return DeserializeProjectData(json);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"インポートに失敗しました: {ex.Message}");
        }

        return null;
    }

    public async Task<ProjectData?> ImportFromPathAsync(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                await ShowErrorAsync("インポートするファイルが見つかりません。");
                return null;
            }

            var json = await File.ReadAllTextAsync(path);
            return DeserializeProjectData(json);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"インポートに失敗しました: {ex.Message}");
            return null;
        }
    }

    private static ProjectData? DeserializeProjectData(string json)
    {
        return JsonSerializer.Deserialize<ProjectData>(json, CreateReadOptions());
    }

    public async Task OpenDataFolderAsync()
    {
        try
        {
            if (!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppDataDirectory,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"フォルダを開くのに失敗しました: {ex.Message}");
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        if (_window == null) return;

        await Views.MessageDialog.ShowAsync(_window, "エラー", message);
    }
}
