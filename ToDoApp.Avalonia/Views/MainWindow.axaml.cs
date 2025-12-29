using Avalonia.Controls;
using Avalonia.Input;
using ToDoApp.Avalonia.Services;
using ToDoApp.Avalonia.ViewModels;

namespace ToDoApp.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // DIコンテナからViewModelを取得
        var dataService = new DataService(this);
        var viewModel = new MainWindowViewModel(dataService, this);
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.HandleKeyDown(e);
        }
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is DataGrid dataGrid)
        {
            if (dataGrid.SelectedItem is Models.TodoItem item)
            {
                viewModel.EditItemCommand.Execute(item);
            }
        }
    }

    private void StatusComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                viewModel.SelectedStatus = item.Content?.ToString() ?? string.Empty;
            }
            else if (comboBox.SelectedItem is string str)
            {
                viewModel.SelectedStatus = str;
            }
        }
    }

    private void PriorityComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                viewModel.SelectedPriority = item.Content?.ToString() ?? string.Empty;
            }
            else if (comboBox.SelectedItem is string str)
            {
                viewModel.SelectedPriority = str;
            }
        }
    }
}
