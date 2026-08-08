using System;
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
            if (CpuBench.IsEnabled(e.Args) && mainWindow.DataContext is MainWindowViewModel benchVm)
            {
                var phasePath = CpuBench.ResolvePhasePath(e.Args);
                _ = RunCpuBenchAsync(benchVm, jsonPath, phasePath);
            }
            else if (UiBench.IsEnabled(e.Args) && mainWindow.DataContext is MainWindowViewModel uiBenchVm)
            {
                var outPath = UiBench.ResolveOutPath(e.Args);
                _ = RunUiBenchAsync(uiBenchVm, jsonPath, outPath);
            }
            else if (jsonPath != null && mainWindow.DataContext is MainWindowViewModel viewModel)
            {
                _ = viewModel.ImportFromPathAsync(jsonPath);
            }
        }

        private static async System.Threading.Tasks.Task RunCpuBenchAsync(
            MainWindowViewModel viewModel,
            string? jsonPath,
            string phasePath)
        {
            try
            {
                if (jsonPath != null)
                {
                    await viewModel.ImportFromPathAsync(jsonPath);
                }

                await CpuBench.RunAsync(viewModel, phasePath);
            }
            finally
            {
                Current.Shutdown();
                Environment.Exit(0);
            }
        }

        private static async System.Threading.Tasks.Task RunUiBenchAsync(
            MainWindowViewModel viewModel,
            string? jsonPath,
            string outPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonPath))
                {
                    throw new InvalidOperationException("ui-bench requires a .json data path argument.");
                }

                await UiBench.RunAsync(viewModel, jsonPath, outPath);
            }
            finally
            {
                Current.Shutdown();
                Environment.Exit(0);
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
