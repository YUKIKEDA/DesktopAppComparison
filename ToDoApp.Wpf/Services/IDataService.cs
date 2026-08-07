using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Services
{
    public interface IDataService
    {
        Task<ProjectData> LoadDataAsync();
        Task SaveDataAsync(ProjectData data);
        Task ExportDataAsync(ProjectData data);
        Task<ProjectData?> ImportDataAsync();
        Task<ProjectData?> ImportFromPathAsync(string path);
        Task OpenDataFolderAsync();
        Task<WindowSettings?> LoadWindowSettingsAsync();
        Task SaveWindowSettingsAsync(WindowSettings settings);
    }
}
