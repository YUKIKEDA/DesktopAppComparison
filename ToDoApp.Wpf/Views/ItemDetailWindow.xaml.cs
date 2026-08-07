using System.Windows;
using ToDoApp.Wpf.Models;
using ToDoApp.Wpf.ViewModels;

namespace ToDoApp.Wpf.Views
{
    public partial class ItemDetailWindow : Window
    {
        public ItemDetailWindow(TodoItem item, Action<TodoItem> onSaved)
        {
            InitializeComponent();
            DataContext = new ItemDetailWindowViewModel(item, onSaved, Close);
        }
    }
}
