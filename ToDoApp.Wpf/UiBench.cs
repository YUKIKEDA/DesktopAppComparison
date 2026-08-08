using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ToDoApp.Wpf.ViewModels;

namespace ToDoApp.Wpf;

internal static class UiBench
{
    private const string Flag = "--ui-bench";
    private const string OutPrefix = "--ui-bench-out=";
    private const double ScrollDurationSec = 3.0;
    private const int FilterToggleCount = 10;

    public static bool IsEnabled(IEnumerable<string>? args) =>
        args != null && args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase));

    public static string ResolveOutPath(IEnumerable<string>? args)
    {
        var outArg = args?.FirstOrDefault(a =>
            a.StartsWith(OutPrefix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(outArg))
        {
            return outArg[OutPrefix.Length..].Trim().Trim('"');
        }

        return Path.Combine(Path.GetTempPath(), "todo_ui_bench_result.json");
    }

    public static async Task RunAsync(
        MainWindowViewModel viewModel,
        string jsonPath,
        string outPath)
    {
        var processStartUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var startupS = await MeasureStartupSecondsAsync(processStartUtc).ConfigureAwait(false);
        var renderS = await MeasureRender1000SecondsAsync(viewModel, jsonPath).ConfigureAwait(false);
        var scrollFps = await MeasureScrollFpsAsync(viewModel).ConfigureAwait(false);
        var filterMs = await MeasureFilterResponseMsAsync(viewModel).ConfigureAwait(false);

        WriteResult(outPath, startupS, renderS, scrollFps, filterMs);
    }

    private static async Task<double> MeasureStartupSecondsAsync(DateTime processStartUtc)
    {
        await WaitForNextFrameAsync().ConfigureAwait(false);
        return Math.Round((DateTime.UtcNow - processStartUtc).TotalSeconds, 2);
    }

    private static async Task<double> MeasureRender1000SecondsAsync(
        MainWindowViewModel viewModel,
        string jsonPath)
    {
        var sw = Stopwatch.StartNew();
        await InvokeUiAsync(async () => await viewModel.ImportFromPathAsync(jsonPath)).ConfigureAwait(false);
        await WaitForImportRenderedAsync(viewModel).ConfigureAwait(false);
        sw.Stop();
        return Math.Round(sw.Elapsed.TotalSeconds, 2);
    }

    private static async Task WaitForImportRenderedAsync(MainWindowViewModel viewModel)
    {
        for (var i = 0; i < 300; i++)
        {
            var visible = await InvokeUiAsync(() => viewModel.FilteredItems.Count).ConfigureAwait(false);
            if (visible > 0)
            {
                break;
            }

            await WaitForNextFrameAsync().ConfigureAwait(false);
        }

        await WaitForNextFrameAsync().ConfigureAwait(false);
    }

    private static Task<double> MeasureScrollFpsAsync(MainWindowViewModel viewModel)
    {
        var tcs = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF Application dispatcher is unavailable.");

        _ = dispatcher.BeginInvoke(() =>
        {
            try
            {
                var frameCount = 0;
                var sw = Stopwatch.StartNew();
                EventHandler? handler = null;
                handler = (_, _) =>
                {
                    frameCount++;
                    if (sw.Elapsed.TotalSeconds >= ScrollDurationSec)
                    {
                        CompositionTarget.Rendering -= handler!;
                        var fps = Math.Round(frameCount / sw.Elapsed.TotalSeconds, 1);
                        tcs.TrySetResult(fps);
                        return;
                    }

                    if (!viewModel.LoadMoreVisible())
                    {
                        viewModel.ResetVisibleForBench();
                    }
                };
                CompositionTarget.Rendering += handler;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, DispatcherPriority.Normal);

        return tcs.Task;
    }

    private static async Task<double> MeasureFilterResponseMsAsync(MainWindowViewModel viewModel)
    {
        var samples = new double[FilterToggleCount];
        for (var i = 0; i < FilterToggleCount; i++)
        {
            var active = i % 2 == 0;
            var sw = Stopwatch.StartNew();
            await viewModel.UiBenchToggleFiltersAsync(active).ConfigureAwait(false);
            sw.Stop();
            samples[i] = sw.Elapsed.TotalMilliseconds;
        }

        return Math.Round(samples.Average(), 1);
    }

    private static Task WaitForNextFrameAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF Application dispatcher is unavailable.");

        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(() => EnqueueFrameWait(tcs), DispatcherPriority.Normal);
            return tcs.Task;
        }

        EnqueueFrameWait(tcs);
        return tcs.Task;
    }

    private static void EnqueueFrameWait(TaskCompletionSource tcs)
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            CompositionTarget.Rendering -= handler!;
            tcs.TrySetResult();
        };
        CompositionTarget.Rendering += handler;
    }

    private static Task InvokeUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF Application dispatcher is unavailable.");
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = dispatcher.BeginInvoke(() =>
        {
            try
            {
                action();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, DispatcherPriority.Normal);
        return tcs.Task;
    }

    private static Task InvokeUiAsync(Func<Task> action)
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF Application dispatcher is unavailable.");
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = dispatcher.BeginInvoke(async () =>
        {
            try
            {
                await action().ConfigureAwait(true);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, DispatcherPriority.Normal);
        return tcs.Task;
    }

    private static Task<T> InvokeUiAsync<T>(Func<T> func)
    {
        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException("WPF Application dispatcher is unavailable.");
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = dispatcher.BeginInvoke(() =>
        {
            try
            {
                tcs.TrySetResult(func());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, DispatcherPriority.Normal);
        return tcs.Task;
    }

    private static void WriteResult(
        string outPath,
        double startupS,
        double render1000S,
        double scrollFps,
        double filterResponseMs)
    {
        var dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var result = new UiBenchResult
        {
            StartupS = startupS,
            Render1000S = render1000S,
            ScrollFps = scrollFps,
            FilterResponseMs = filterResponseMs
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        File.WriteAllText(outPath, json, System.Text.Encoding.UTF8);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private sealed class UiBenchResult
    {
        [JsonPropertyName("startup_s")]
        public double StartupS { get; set; }

        [JsonPropertyName("render_1000_s")]
        public double Render1000S { get; set; }

        [JsonPropertyName("scroll_fps")]
        public double ScrollFps { get; set; }

        [JsonPropertyName("filter_response_ms")]
        public double FilterResponseMs { get; set; }
    }
}
