using Avalonia.Controls;
using ToDoApp.Avalonia.Models;
using ToDoApp.Avalonia.ViewModels;

namespace ToDoApp.Avalonia.Views;

public partial class ItemDetailWindow : Window
{
    public ItemDetailWindow()
    {
        InitializeComponent();
    }

    public ItemDetailWindow(MainWindowViewModel mainViewModel, TodoItem item)
    {
        InitializeComponent();
        DataContext = new ItemDetailViewModel(mainViewModel, item, this);
        Title = string.IsNullOrWhiteSpace(item.Title) ? "アイテム詳細" : item.Title;
    }
}
