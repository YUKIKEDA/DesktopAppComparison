using System.Threading.Tasks;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Services;

public interface IDataService
{
    string DataDirectory { get; }
    string WindowSettingsPath { get; }

    Task<ProjectData> LoadDataAsync();
    Task SaveDataAsync(ProjectData data);
    Task ExportDataAsync(ProjectData data);
    Task<ProjectData?> ImportDataAsync();
    Task<ProjectData?> ImportFromPathAsync(string path);
    Task OpenDataFolderAsync();
}
