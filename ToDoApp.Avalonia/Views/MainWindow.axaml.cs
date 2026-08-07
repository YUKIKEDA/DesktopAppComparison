using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ToDoApp.Avalonia.Services;
using ToDoApp.Avalonia.ViewModels;

namespace ToDoApp.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly IDataService _dataService;
    private ScrollViewer? _dataGridScrollViewer;

    public MainWindow()
    {
        InitializeComponent();

        _dataService = new DataService(this);
        var viewModel = new MainWindowViewModel(_dataService, this);
        DataContext = viewModel;
        viewModel.SetWindow(this);

        Opened += OnOpened;
        Closing += OnClosing;
        TodoDataGrid.TemplateApplied += TodoDataGrid_TemplateApplied;
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void TodoDataGrid_TemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        AttachDataGridScrollListener();
    }

    private void AttachDataGridScrollListener()
    {
        if (_dataGridScrollViewer != null)
        {
            _dataGridScrollViewer.ScrollChanged -= OnDataGridScrollChanged;
        }

        _dataGridScrollViewer = TodoDataGrid.FindDescendantOfType<ScrollViewer>();
        if (_dataGridScrollViewer != null)
        {
            _dataGridScrollViewer.ScrollChanged += OnDataGridScrollChanged;
        }
    }

    private void OnDataGridScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (scrollViewer.Extent.Height <= 0)
        {
            return;
        }

        var remaining = scrollViewer.Extent.Height - scrollViewer.Offset.Y - scrollViewer.Viewport.Height;
        if (remaining < 80)
        {
            viewModel.LoadMoreVisible();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        WindowGeometryService.Apply(this, _dataService.WindowSettingsPath, 1400, 900);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        WindowGeometryService.Save(this, _dataService.WindowSettingsPath);

        if (global::Avalonia.Application.Current is App app && !app.ShouldExit)
        {
            e.Cancel = true;
            Hide();
        }
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
