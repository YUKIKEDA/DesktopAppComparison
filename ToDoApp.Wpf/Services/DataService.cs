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
        private static readonly string WindowFile = Path.Combine(DataDirectory, "window.json");

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

        private static void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }

        private static ProjectData? DeserializeProjectData(string json)
        {
            return JsonSerializer.Deserialize<ProjectData>(json, CreateReadOptions());
        }

        public async Task<ProjectData> LoadDataAsync()
        {
            try
            {
                EnsureDataDirectory();

                if (File.Exists(DataFile))
                {
                    var json = await File.ReadAllTextAsync(DataFile);
                    var data = DeserializeProjectData(json);
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
                EnsureDataDirectory();

                var json = JsonSerializer.Serialize(data, CreateWriteOptions());
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
                    var json = JsonSerializer.Serialize(data, CreateWriteOptions());
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
                    return await ImportFromPathAsync(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"インポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public async Task<ProjectData?> ImportFromPathAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(path);
                return DeserializeProjectData(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"インポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        public Task OpenDataFolderAsync()
        {
            try
            {
                EnsureDataDirectory();

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

        public async Task<WindowSettings?> LoadWindowSettingsAsync()
        {
            try
            {
                EnsureDataDirectory();

                if (!File.Exists(WindowFile))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(WindowFile);
                return JsonSerializer.Deserialize<WindowSettings>(json, CreateReadOptions());
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveWindowSettingsAsync(WindowSettings settings)
        {
            try
            {
                EnsureDataDirectory();

                var json = JsonSerializer.Serialize(settings, CreateWriteOptions());
                await File.WriteAllTextAsync(WindowFile, json);
            }
            catch
            {
                // ウィンドウ位置の保存失敗は致命的ではないため無視
            }
        }
    }
}
