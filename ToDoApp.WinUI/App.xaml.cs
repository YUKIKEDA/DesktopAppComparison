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

            var jsonPath = FindJsonPath(Environment.GetCommandLineArgs())
                           ?? FindJsonPathFromLaunchArgs(args.Arguments)
                           ?? FindJsonPathFromAppLifecycle();
            if (jsonPath != null)
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
