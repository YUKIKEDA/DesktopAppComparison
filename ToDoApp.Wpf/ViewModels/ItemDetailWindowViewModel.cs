using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.ViewModels
{
    public partial class ItemDetailWindowViewModel : ObservableObject
    {
        private readonly Action<TodoItem> _onSaved;
        private readonly Action _onClose;

        [ObservableProperty]
        private TodoItem _editingItem;

        public ItemDetailWindowViewModel(TodoItem item, Action<TodoItem> onSaved, Action onClose)
        {
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
            _onSaved = onSaved;
            _onClose = onClose;
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingItem.Title))
            {
                MessageBox.Show("タイトルは必須です。", "バリデーションエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingItem.Title.Length > 200)
            {
                MessageBox.Show("タイトルは200文字以内です。", "バリデーションエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(EditingItem.Description) && EditingItem.Description.Length > 500)
            {
                MessageBox.Show("説明は500文字以内です。", "バリデーションエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditingItem.UpdatedAt = DateTime.Now;
            _onSaved(EditingItem);
            _onClose();
        }

        [RelayCommand]
        private void Cancel()
        {
            _onClose();
        }
    }
}
