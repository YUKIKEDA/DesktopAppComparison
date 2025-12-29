using System.Threading.Tasks;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Services;

public interface IDataService
{
    Task<ProjectData> LoadDataAsync();
    Task SaveDataAsync(ProjectData data);
    Task ExportDataAsync(ProjectData data);
    Task<ProjectData?> ImportDataAsync();
    Task OpenDataFolderAsync();
}

