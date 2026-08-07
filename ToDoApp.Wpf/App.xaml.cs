using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ToDoApp.Wpf.Services;
using ToDoApp.Wpf.ViewModels;
using ToDoApp.Wpf.Views;

namespace ToDoApp.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static TrayService TrayService { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            services.AddSingleton<IDataService, DataService>();
            ServiceProvider = services.BuildServiceProvider();

            TrayService = new TrayService();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            var jsonPath = e.Args.FirstOrDefault(IsJsonPath);
            if (jsonPath != null && mainWindow.DataContext is MainWindowViewModel viewModel)
            {
                _ = viewModel.ImportFromPathAsync(jsonPath);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (MainWindow?.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Cleanup();
            }

            TrayService?.Dispose();
            base.OnExit(e);
        }

        private static bool IsJsonPath(string arg) =>
            !string.IsNullOrWhiteSpace(arg) &&
            Path.GetExtension(arg).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(arg);
    }
}
