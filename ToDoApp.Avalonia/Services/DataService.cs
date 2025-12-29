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
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ToDoApp.Avalonia",
        "data"
    );
    private static readonly string DataFile = Path.Combine(DataDirectory, "project.json");

    private readonly Window? _window;

    public DataService(Window? window = null)
    {
        _window = window;
    }

    public async Task<ProjectData> LoadDataAsync()
    {
        try
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            if (File.Exists(DataFile))
            {
                var json = await File.ReadAllTextAsync(DataFile);
                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
                };
                var options = jsonSerializerOptions;
                var data = JsonSerializer.Deserialize<ProjectData>(json, options);
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
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            JsonSerializerOptions jsonSerializerOptions = new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
            };
            var options = jsonSerializerOptions;
            var json = JsonSerializer.Serialize(data, options);
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
                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
                };
                var options = jsonSerializerOptions;
                var json = JsonSerializer.Serialize(data, options);
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
                await using var stream = await files[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
                };
                var options = jsonSerializerOptions;
                var data = JsonSerializer.Deserialize<ProjectData>(json, options);
                return data;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"インポートに失敗しました: {ex.Message}");
        }

        return null;
    }

    public async Task OpenDataFolderAsync()
    {
        try
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = DataDirectory,
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

