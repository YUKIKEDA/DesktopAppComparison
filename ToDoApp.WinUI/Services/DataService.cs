using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using ToDoApp.WinUI.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace ToDoApp.WinUI.Services
{
    public class DataService : IDataService
    {
        private static readonly string DataFileName = "project.json";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Electronアプリとの互換性のためcamelCaseを使用
        };

        private string GetDataFilePath()
        {
            var localFolder = ApplicationData.Current.LocalFolder.Path;
            var dataFolder = Path.Combine(localFolder, "Data");
            Directory.CreateDirectory(dataFolder);
            return Path.Combine(dataFolder, DataFileName);
        }

        public async Task<ProjectData> LoadDataAsync()
        {
            try
            {
                var filePath = GetDataFilePath();
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var data = JsonSerializer.Deserialize<ProjectData>(json, JsonOptions);
                    return data ?? new ProjectData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }

            return new ProjectData();
        }

        public async Task SaveDataAsync(ProjectData data)
        {
            try
            {
                var filePath = GetDataFilePath();
                System.Diagnostics.Debug.WriteLine($"[DataService] Saving to: {filePath}");
                System.Diagnostics.Debug.WriteLine($"[DataService] Items count: {data.Items.Count}");
                var json = JsonSerializer.Serialize(data, JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                System.Diagnostics.Debug.WriteLine($"[DataService] Save completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataService] Error saving data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DataService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task ExportDataAsync(ProjectData data)
        {
            try
            {
                var picker = new FileSavePicker();
                var window = (Application.Current as App)?.MainWindow;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }

                picker.SuggestedFileName = "project.json";
                picker.FileTypeChoices.Add("JSON Files", new[] { ".json" });

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    var json = JsonSerializer.Serialize(data, JsonOptions);
                    await FileIO.WriteTextAsync(file, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting data: {ex.Message}");
                throw;
            }
        }

        public async Task<ProjectData?> ImportDataAsync()
        {
            try
            {
                var picker = new FileOpenPicker();
                var window = (Application.Current as App)?.MainWindow;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }

                picker.FileTypeFilter.Add(".json");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var json = await FileIO.ReadTextAsync(file);
                    var data = JsonSerializer.Deserialize<ProjectData>(json, JsonOptions);
                    return data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing data: {ex.Message}");
            }

            return null;
        }

        public async Task OpenDataFolderAsync()
        {
            try
            {
                var dataFolder = Path.GetDirectoryName(GetDataFilePath());
                if (Directory.Exists(dataFolder))
                {
                    await Launcher.LaunchFolderPathAsync(dataFolder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening data folder: {ex.Message}");
                throw;
            }
        }
    }
}

