using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ToDoApp.Wpf.Services;
using ToDoApp.Wpf.ViewModels;

namespace ToDoApp.Wpf.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // DIコンテナからViewModelを取得
            var dataService = App.ServiceProvider.GetRequiredService<IDataService>();
            DataContext = new MainWindowViewModel(dataService);
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
                if (dataGrid.SelectedItem is Models.TodoItem item)
                {
                    viewModel.EditItemCommand.Execute(item);
                }
            }
        }
    }
}
