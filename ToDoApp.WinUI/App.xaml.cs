using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ToDoApp.WinUI.Views;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace ToDoApp.WinUI
{
    public partial class App : Application
    {
        private Window? _window;

        public Window? MainWindow => _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();

            var cmdArgs = Environment.GetCommandLineArgs();
            var jsonPath = FindJsonPath(cmdArgs)
                           ?? FindJsonPathFromLaunchArgs(args.Arguments)
                           ?? FindJsonPathFromAppLifecycle();

            // Prefer explicit request files / flags; ui-bench request must win over leftover cpu request.
            if (UiBench.IsEnabled(cmdArgs))
            {
                jsonPath ??= UiBench.TryConsumeRequestJsonPath();
                var outPath = UiBench.ResolveOutPath(cmdArgs);
                UiBench.ClearRequestFile();
                _ = RunUiBenchAsync(outPath, jsonPath);
            }
            else if (CpuBench.IsEnabled(cmdArgs))
            {
                jsonPath ??= CpuBench.TryConsumeRequestJsonPath();
                var phasePath = CpuBench.ResolvePhasePath(cmdArgs);
                CpuBench.ClearRequestFile();
                _ = RunCpuBenchAsync(jsonPath, phasePath);
            }
            else if (jsonPath != null)
            {
                _ = ImportJsonAsync(jsonPath);
            }
        }

        private async System.Threading.Tasks.Task ImportJsonAsync(string path)
        {
            if (_window is not MainWindow mainWindow || mainWindow.MainPage == null)
            {
                return;
            }

            await mainWindow.MainPage.ViewModel.ImportFromPathAsync(path);
        }

        private async System.Threading.Tasks.Task RunCpuBenchAsync(string? jsonPath, string phasePath)
        {
            try
            {
                if (_window is not MainWindow mainWindow || mainWindow.MainPage == null)
                {
                    return;
                }

                var viewModel = mainWindow.MainPage.ViewModel;
                if (jsonPath != null)
                {
                    await viewModel.ImportFromPathAsync(jsonPath);
                }

                await CpuBench.RunAsync(viewModel, phasePath, viewModel.DispatcherQueue);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private async System.Threading.Tasks.Task RunUiBenchAsync(string outPath, string? jsonPath)
        {
            try
            {
                MainWindow? mainWindow = null;
                for (var i = 0; i < 100; i++)
                {
                    mainWindow = _window as MainWindow;
                    if (mainWindow?.MainPage != null)
                    {
                        break;
                    }

                    await System.Threading.Tasks.Task.Delay(50);
                }

                if (mainWindow?.MainPage == null || string.IsNullOrWhiteSpace(jsonPath))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"UI bench abort: page={mainWindow?.MainPage != null} json={(jsonPath != null)}");
                    return;
                }

                var viewModel = mainWindow.MainPage.ViewModel;
                viewModel.SuppressSave = true;
                await UiBench.RunAsync(viewModel, outPath, jsonPath, viewModel.DispatcherQueue);
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private static string? FindJsonPath(IEnumerable<string> args)
        {
            return args.Skip(1).FirstOrDefault(IsJsonPath);
        }

        private static string? FindJsonPathFromLaunchArgs(string? arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return null;
            }

            var trimmed = arguments.Trim().Trim('"');
            return IsJsonPath(trimmed) ? trimmed : null;
        }

        private static string? FindJsonPathFromAppLifecycle()
        {
            try
            {
                var activated = AppInstance.GetCurrent().GetActivatedEventArgs();
                if (activated.Kind == ExtendedActivationKind.File &&
                    activated.Data is IFileActivatedEventArgs fileArgs)
                {
                    var file = fileArgs.Files.OfType<StorageFile>().FirstOrDefault();
                    if (file != null && IsJsonPath(file.Path))
                    {
                        return file.Path;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppLifecycle activation check failed: {ex.Message}");
            }

            return null;
        }

        private static bool IsJsonPath(string arg) =>
            !string.IsNullOrWhiteSpace(arg) &&
            Path.GetExtension(arg).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(arg);
    }
}
