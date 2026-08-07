using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Services;

public static class ClipboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task CopyTodoItemsAsync(Window? window, IEnumerable<TodoItem> items)
    {
        if (window?.Clipboard == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(items.ToList(), JsonOptions);
        await window.Clipboard.SetTextAsync(json);
    }
}
