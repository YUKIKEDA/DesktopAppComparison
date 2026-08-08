using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using ToDoApp.WinUI.ViewModels;
using Windows.Storage;

namespace ToDoApp.WinUI;

internal static class UiBench
{
    private const string Flag = "--ui-bench";
    private const string OutPrefix = "--ui-bench-out=";
    private const double ScrollDurationSec = 3.0;
    private const int FilterToggleCount = 10;

    /// <summary>
    /// AppsFolder activation cannot pass CLI args; measure_ui.ps1 drops a request file instead.
    /// Packaged LocalState (not unpackaged LocalAppData\ToDoApp.WinUI).
    /// </summary>
    public static string RequestFilePath
    {
        get
        {
            try
            {
                return Path.Combine(ApplicationData.Current.LocalFolder.Path, "ui_bench_request.txt");
            }
            catch
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages",
                    "d58fe1bb-f479-4f19-b358-06dee335f74c_k6bmzwkfnste6",
                    "LocalState",
                    "ui_bench_request.txt");
            }
        }
    }

    public static bool IsEnabled(IEnumerable<string>? args) =>
        (args != null && args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase)))
        || File.Exists(RequestFilePath);

    public static string ResolveOutPath(IEnumerable<string>? args)
    {
        var outArg = args?.FirstOrDefault(a =>
            a.StartsWith(OutPrefix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(outArg))
        {
            return outArg[OutPrefix.Length..].Trim().Trim('"');
        }

        if (TryReadRequest(out var outPath, out _) && !string.IsNullOrWhiteSpace(outPath))
        {
            return outPath!;
        }

        return Path.Combine(Path.GetTempPath(), "todo_ui_bench_result.json");
    }

    public static string? TryConsumeRequestJsonPath()
    {
        if (!TryReadRequest(out _, out var jsonPath))
        {
            return null;
        }

        return jsonPath;
    }

    public static void ClearRequestFile()
    {
        try
        {
            if (File.Exists(RequestFilePath))
            {
                File.Delete(RequestFilePath);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private static bool TryReadRequest(out string? outPath, out string? jsonPath)
    {
        outPath = null;
        jsonPath = null;
        try
        {
            if (!File.Exists(RequestFilePath))
            {
                return false;
            }

            var lines = File.ReadAllLines(RequestFilePath);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith("out=", StringComparison.OrdinalIgnoreCase))
                {
                    outPath = line["out=".Length..].Trim().Trim('"');
                }
                else if (line.StartsWith("json=", StringComparison.OrdinalIgnoreCase))
                {
                    jsonPath = line["json=".Length..].Trim().Trim('"');
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task RunAsync(
        MainWindowViewModel viewModel,
        string outPath,
        string jsonPath,
        DispatcherQueue dispatcher)
    {
        var processStart = Process.GetCurrentProcess().StartTime;
        double startupS = 0, renderS = 0, scrollFps = 0, filterMs = 0;
        try
        {
            startupS = await MeasureStartupSecondsAsync(processStart, dispatcher).ConfigureAwait(false);
            renderS = await MeasureRender1000SecondsAsync(viewModel, jsonPath, dispatcher).ConfigureAwait(false);
            scrollFps = await MeasureScrollFpsAsync(viewModel, dispatcher).ConfigureAwait(false);
            filterMs = await MeasureFilterResponseMsAsync(viewModel, dispatcher).ConfigureAwait(false);
        }
        finally
        {
            WriteResult(outPath, startupS, renderS, scrollFps, filterMs);
        }
    }

    private static async Task<double> MeasureStartupSecondsAsync(
        DateTime processStart,
        DispatcherQueue dispatcher)
    {
        await WaitForNextFrameAsync(dispatcher).ConfigureAwait(false);
        return Math.Round((DateTime.Now - processStart).TotalSeconds, 2);
    }

    private static async Task<double> MeasureRender1000SecondsAsync(
        MainWindowViewModel viewModel,
        string jsonPath,
        DispatcherQueue dispatcher)
    {
        var sw = Stopwatch.StartNew();
        // ImportFromPathAsync marshals collection updates to the UI thread and awaits filters.
        await viewModel.ImportFromPathAsync(jsonPath).ConfigureAwait(false);
        await WaitForImportRenderedAsync(viewModel, dispatcher).ConfigureAwait(false);
        sw.Stop();
        return Math.Round(sw.Elapsed.TotalSeconds, 2);
    }

    private static async Task WaitForImportRenderedAsync(
        MainWindowViewModel viewModel,
        DispatcherQueue dispatcher)
    {
        // Should already be populated after awaited ApplyFiltersAsync; short poll as safety.
        for (var i = 0; i < 40; i++)
        {
            var visible = await InvokeAsync(dispatcher, () => viewModel.FilteredItems.Count).ConfigureAwait(false);
            if (visible > 0)
            {
                break;
            }

            await Task.Delay(25).ConfigureAwait(false);
        }

        await WaitForNextFrameAsync(dispatcher).ConfigureAwait(false);
    }

    private static async Task<double> MeasureScrollFpsAsync(
        MainWindowViewModel viewModel,
        DispatcherQueue dispatcher)
    {
        var tcs = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    var frameCount = 0;
                    var sw = Stopwatch.StartNew();
                    EventHandler<object>? handler = null;
                    handler = (_, _) =>
                    {
                        frameCount++;
                        if (sw.Elapsed.TotalSeconds >= ScrollDurationSec)
                        {
                            CompositionTarget.Rendering -= handler;
                            var fps = Math.Round(frameCount / Math.Max(sw.Elapsed.TotalSeconds, 0.001), 1);
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
            }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue scroll fps measurement."));
        }

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(ScrollDurationSec + 3)))
            .ConfigureAwait(false);
        if (completed != tcs.Task)
        {
            return 0;
        }

        return await tcs.Task.ConfigureAwait(false);
    }

    private static async Task<double> MeasureFilterResponseMsAsync(
        MainWindowViewModel viewModel,
        DispatcherQueue dispatcher)
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

    private static async Task WaitForNextFrameAsync(DispatcherQueue dispatcher)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(() =>
            {
                EventHandler<object>? handler = null;
                handler = (_, _) =>
                {
                    CompositionTarget.Rendering -= handler;
                    tcs.TrySetResult();
                };
                CompositionTarget.Rendering += handler;
                _ = dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => { });
            }))
        {
            throw new InvalidOperationException("Failed to enqueue frame wait for ui-bench.");
        }

        await Task.WhenAny(tcs.Task, Task.Delay(2000)).ConfigureAwait(false);
    }

    private static Task InvokeAsync(DispatcherQueue dispatcher, Action action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(() =>
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
            }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue UI work for ui-bench."));
        }

        return tcs.Task;
    }

    private static Task<T> InvokeAsync<T>(DispatcherQueue dispatcher, Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue UI work for ui-bench."));
        }

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

        // Avoid System.Text.Json in trimmed Release — write manually.
        var json =
            $"{{\"startup_s\":{startupS.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
            $"\"render_1000_s\":{render1000S.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
            $"\"scroll_fps\":{scrollFps.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
            $"\"filter_response_ms\":{filterResponseMs.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}";
        File.WriteAllText(outPath, json);
    }
}
