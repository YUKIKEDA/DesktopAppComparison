using System.IO;
using System.Windows;
using System.Windows.Input;
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

        public MainWindow()
        {
            InitializeComponent();

            _dataService = App.ServiceProvider.GetRequiredService<IDataService>();
            DataContext = new MainWindowViewModel(_dataService);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
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
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = HasJsonFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
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

        private static bool HasJsonFileDrop(DragEventArgs e)
        {
            return GetDroppedJsonPath(e) != null;
        }

        private static string? GetDroppedJsonPath(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return null;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
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
