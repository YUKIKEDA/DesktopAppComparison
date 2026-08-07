using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ToDoApp.Avalonia.Services;
using ToDoApp.Avalonia.ViewModels;

namespace ToDoApp.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly IDataService _dataService;

    public MainWindow()
    {
        InitializeComponent();

        _dataService = new DataService(this);
        var viewModel = new MainWindowViewModel(_dataService, this);
        DataContext = viewModel;
        viewModel.SetWindow(this);

        Opened += OnOpened;
        Closing += OnClosing;
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        WindowGeometryService.Apply(this, _dataService.WindowSettingsPath, 1400, 900);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        WindowGeometryService.Save(this, _dataService.WindowSettingsPath);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = GetFirstJsonPath(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var path = GetFirstJsonPath(e);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        await viewModel.ImportFromPathAsync(path);
    }

    private static string? GetFirstJsonPath(DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File))
        {
            return null;
        }

        var files = e.DataTransfer.TryGetFiles();
        if (files == null)
        {
            return null;
        }

        foreach (var item in files)
        {
            var path = item switch
            {
                IStorageFile storageFile => storageFile.TryGetLocalPath(),
                _ => item.Path.LocalPath
            };

            if (!string.IsNullOrEmpty(path) &&
                string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
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
