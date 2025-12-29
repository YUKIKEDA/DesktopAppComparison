using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.Win32;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Services
{
    public class DataService : IDataService
    {
        private static readonly string DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ToDoApp.Wpf",
            "data"
        );
        private static readonly string DataFile = Path.Combine(DataDirectory, "project.json");

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
                MessageBox.Show($"データの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"データの保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task ExportDataAsync(ProjectData data)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "データをエクスポート",
                    FileName = "project.json",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    JsonSerializerOptions jsonSerializerOptions = new()
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter(), new DateTimeConverter(), new NullableDateTimeConverter() }
                    };
                    var options = jsonSerializerOptions;
                    var json = JsonSerializer.Serialize(data, options);
                    await File.WriteAllTextAsync(dialog.FileName, json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エクスポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<ProjectData?> ImportDataAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "データをインポート",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = await File.ReadAllTextAsync(dialog.FileName);
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
                MessageBox.Show($"インポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public Task OpenDataFolderAsync()
        {
            try
            {
                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(DataDirectory);
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = DataDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダを開くのに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
    }
}

