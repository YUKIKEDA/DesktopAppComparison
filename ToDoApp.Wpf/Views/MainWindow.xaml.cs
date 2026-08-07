using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ToDoApp.Wpf.Models;
using ToDoApp.Wpf.Services;
using ToDoApp.Wpf.ViewModels;

namespace ToDoApp.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IDataService _dataService;
        private ScrollViewer? _dataGridScrollViewer;

        public MainWindow()
        {
            InitializeComponent();

            _dataService = App.ServiceProvider.GetRequiredService<IDataService>();
            DataContext = new MainWindowViewModel(_dataService);
            TodoDataGrid.Loaded += TodoDataGrid_Loaded;
        }

        private void TodoDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_dataGridScrollViewer != null)
            {
                _dataGridScrollViewer.ScrollChanged -= OnDataGridScrollChanged;
            }

            _dataGridScrollViewer = FindVisualChild<ScrollViewer>(TodoDataGrid);
            if (_dataGridScrollViewer != null)
            {
                _dataGridScrollViewer.ScrollChanged += OnDataGridScrollChanged;
            }
        }

        private void OnDataGridScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            if (e.ExtentHeight <= 0)
            {
                return;
            }

            var remaining = e.ExtentHeight - e.VerticalOffset - e.ViewportHeight;
            if (remaining < 80)
            {
                viewModel.LoadMoreVisible();
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                {
                    return typed;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.HandleKeyDown(e);
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel && sender is System.Windows.Controls.DataGrid dataGrid)
            {
                if (dataGrid.SelectedItem is TodoItem item)
                {
                    viewModel.EditItemCommand.Execute(item);
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = await _dataService.LoadWindowSettingsAsync();
            if (settings == null)
            {
                return;
            }

            if (settings.Width > 0)
            {
                Width = settings.Width;
            }

            if (settings.Height > 0)
            {
                Height = settings.Height;
            }

            Left = settings.X;
            Top = settings.Y;

            if (!IsWindowOnScreen(Left, Top, Width, Height))
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Left = (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left;
                Top = (SystemParameters.WorkArea.Height - Height) / 2 + SystemParameters.WorkArea.Top;
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            var settings = new WindowSettings
            {
                X = Left,
                Y = Top,
                Width = Width,
                Height = Height
            };
            _dataService.SaveWindowSettingsAsync(settings).GetAwaiter().GetResult();

            if (!App.TrayService.ExitRequested)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = HasJsonFileDrop(e)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (DataContext is not MainWindowViewModel viewModel)
            {
                return;
            }

            var path = GetDroppedJsonPath(e);
            if (path == null)
            {
                return;
            }

            await viewModel.ImportFromPathAsync(path);
        }

        private static bool HasJsonFileDrop(System.Windows.DragEventArgs e)
        {
            return GetDroppedJsonPath(e) != null;
        }

        private static string? GetDroppedJsonPath(System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                return null;
            }

            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return null;
            }

            var path = files[0];
            return Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
                ? path
                : null;
        }

        private static bool IsWindowOnScreen(double left, double top, double width, double height)
        {
            var windowRect = new Rect(left, top, width, height);
            var virtualScreen = new Rect(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight
            );

            // タイトルバー付近が仮想画面内にあることを確認
            var titleBarPoint = new Point(left + Math.Min(40, width / 2), top + 10);
            return virtualScreen.Contains(titleBarPoint) && virtualScreen.IntersectsWith(windowRect);
        }
    }
}
