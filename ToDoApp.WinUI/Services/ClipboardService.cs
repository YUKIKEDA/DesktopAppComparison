using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ToDoApp.WinUI.Models;
using Windows.ApplicationModel.DataTransfer;

namespace ToDoApp.WinUI.Services
{
    public static class ClipboardService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static void CopyTodoItems(IEnumerable<TodoItem> items)
        {
            var json = JsonSerializer.Serialize(items.ToList(), JsonOptions);
            var package = new DataPackage();
            package.SetText(json);
            Clipboard.SetContent(package);
        }
    }
}
