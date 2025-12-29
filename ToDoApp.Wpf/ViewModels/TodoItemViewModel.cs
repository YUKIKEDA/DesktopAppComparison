using CommunityToolkit.Mvvm.ComponentModel;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.ViewModels
{
    public class TodoItemViewModel : ObservableObject
    {
        private readonly TodoItem _item;

        public TodoItemViewModel(TodoItem item)
        {
            _item = item;
        }

        public int Id => _item.Id;
        public string Title => _item.Title;
        public string Description => _item.Description;
        public string Status => _item.Status;
        public string Priority => _item.Priority;
        public DateTime? DueDate => _item.DueDate;
        public DateTime CreatedAt => _item.CreatedAt;
        public DateTime UpdatedAt => _item.UpdatedAt;
        public bool IsCompleted => _item.IsCompleted;

        public TodoItem ToModel()
        {
            return _item;
        }
    }
}

