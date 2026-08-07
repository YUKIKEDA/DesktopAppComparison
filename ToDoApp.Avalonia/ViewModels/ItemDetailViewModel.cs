using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.ViewModels;

public partial class ItemDetailViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainViewModel;
    private readonly Window _window;

    [ObservableProperty]
    private TodoItem _editingItem;

    public IReadOnlyList<string> StatusOptions => _mainViewModel.StatusOptions;

    public IReadOnlyList<string> PriorityOptions => _mainViewModel.PriorityOptions;

    public ItemDetailViewModel(MainWindowViewModel mainViewModel, TodoItem item, Window window)
    {
        _mainViewModel = mainViewModel;
        _window = window;
        _editingItem = new TodoItem
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Status = item.Status,
            Priority = item.Priority,
            DueDate = item.DueDate,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            IsCompleted = item.IsCompleted
        };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditingItem.Title))
        {
            await Views.MessageDialog.ShowAsync(_window, "バリデーションエラー", "タイトルは必須です。");
            return;
        }

        if (EditingItem.Title.Length > 200)
        {
            await Views.MessageDialog.ShowAsync(_window, "バリデーションエラー", "タイトルは200文字以内です。");
            return;
        }

        if (!string.IsNullOrEmpty(EditingItem.Description) && EditingItem.Description.Length > 500)
        {
            await Views.MessageDialog.ShowAsync(_window, "バリデーションエラー", "説明は500文字以内です。");
            return;
        }

        await _mainViewModel.ApplyItemUpdateAsync(EditingItem);
        _window.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.Close();
    }
}
