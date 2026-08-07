using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.IO;
using System.Linq;
using ToDoApp.Avalonia.ViewModels;
using ToDoApp.Avalonia.Views;

namespace ToDoApp.Avalonia;

public partial class App : Application
{
    private bool _exitRequested;
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _mainWindow = new MainWindow();
            desktop.MainWindow = _mainWindow;

            SetupTrayIcon();

            desktop.Exit += (_, _) =>
            {
                _exitRequested = true;
                if (_mainWindow?.DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.Cleanup();
                }

                DisposeTrayIcons();
            };

            var jsonPath = desktop.Args?.FirstOrDefault(IsJsonPath);
            if (jsonPath != null && _mainWindow.DataContext is MainWindowViewModel viewModel)
            {
                _ = viewModel.ImportFromPathAsync(jsonPath);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void RequestExit()
    {
        _exitRequested = true;
        if (_mainWindow?.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Cleanup();
        }

        DisposeTrayIcons();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void DisposeTrayIcons()
    {
        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons == null)
        {
            return;
        }

        foreach (var trayIcon in trayIcons.ToArray())
        {
            trayIcon.Dispose();
        }

        trayIcons.Clear();
    }

    public bool ShouldExit => _exitRequested;

    public void ShowMainWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_mainWindow == null)
            {
                return;
            }

            if (!_mainWindow.IsVisible)
            {
                _mainWindow.Show();
            }

            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Activate();
        });
    }

    private void SetupTrayIcon()
    {
        var trayIcons = TrayIcon.GetIcons(this);
        if (trayIcons == null || trayIcons.Count == 0)
        {
            return;
        }

        var trayIcon = trayIcons[0];
        trayIcon.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(ShowMainWindow);

        if (trayIcon.Menu != null)
        {
            foreach (var item in trayIcon.Menu.Items)
            {
                if (item is not NativeMenuItem menuItem)
                {
                    continue;
                }

                if (menuItem.Header == "表示")
                {
                    menuItem.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(ShowMainWindow);
                }
                else if (menuItem.Header == "終了")
                {
                    menuItem.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(RequestExit);
                }
            }
        }
    }

    private static bool IsJsonPath(string arg) =>
        !string.IsNullOrWhiteSpace(arg) &&
        Path.GetExtension(arg).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
        File.Exists(arg);

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
