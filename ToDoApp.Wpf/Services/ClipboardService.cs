using System.Text.Json;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Services
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
            System.Windows.Clipboard.SetText(json);
        }
    }
}
