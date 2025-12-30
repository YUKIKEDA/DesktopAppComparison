using System.Threading.Tasks;
using ToDoApp.WinUI.Models;

namespace ToDoApp.WinUI.Services
{
    public interface IDataService
    {
        Task<ProjectData> LoadDataAsync();
        Task SaveDataAsync(ProjectData data);
        Task ExportDataAsync(ProjectData data);
        Task<ProjectData?> ImportDataAsync();
        Task OpenDataFolderAsync();
    }
}

