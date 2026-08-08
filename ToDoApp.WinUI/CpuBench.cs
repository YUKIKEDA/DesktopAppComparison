using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using ToDoApp.WinUI.ViewModels;
using Windows.Storage;

namespace ToDoApp.WinUI;

internal static class CpuBench
{
    private const int PhaseMs = 5000;
    private const string Flag = "--cpu-bench";
    private const string PhasePrefix = "--cpu-bench-phase=";

    /// <summary>
    /// AppsFolder activation cannot pass CLI args; measure_cpu.ps1 drops a request file instead.
    /// Packaged LocalState (not unpackaged LocalAppData\ToDoApp.WinUI).
    /// </summary>
    public static string RequestFilePath
    {
        get
        {
            try
            {
                return Path.Combine(ApplicationData.Current.LocalFolder.Path, "cpu_bench_request.txt");
            }
            catch
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages",
                    "d58fe1bb-f479-4f19-b358-06dee335f74c_k6bmzwkfnste6",
                    "LocalState",
                    "cpu_bench_request.txt");
            }
        }
    }

    public static bool IsEnabled(IEnumerable<string>? args) =>
        (args != null && args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase)))
        || File.Exists(RequestFilePath);

    public static string? TryConsumeRequestJsonPath()
    {
        if (!TryReadRequest(out var jsonPath, out _))
        {
            return null;
        }

        return jsonPath;
    }

    public static string ResolvePhasePath(IEnumerable<string>? args)
    {
        var phaseArg = args?.FirstOrDefault(a =>
            a.StartsWith(PhasePrefix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(phaseArg))
        {
            return phaseArg[PhasePrefix.Length..].Trim().Trim('"');
        }

        if (TryReadRequest(out _, out var phasePath) && !string.IsNullOrWhiteSpace(phasePath))
        {
            return phasePath!;
        }

        return Path.Combine(Path.GetTempPath(), "todo_cpu_bench_phase.txt");
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

    private static bool TryReadRequest(out string? jsonPath, out string? phasePath)
    {
        jsonPath = null;
        phasePath = null;
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
                if (line.StartsWith("phase=", StringComparison.OrdinalIgnoreCase))
                {
                    phasePath = line["phase=".Length..].Trim().Trim('"');
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

    public static void SetPhase(string phasePath, string phase) =>
        File.WriteAllText(phasePath, phase, Encoding.ASCII);

    public static async Task RunAsync(MainWindowViewModel viewModel, string phasePath, DispatcherQueue dispatcher)
    {
        SetPhase(phasePath, "idle");
        await Task.Delay(PhaseMs).ConfigureAwait(false);

        SetPhase(phasePath, "add");
        var addDeadline = Environment.TickCount64 + PhaseMs;
        var n = 0;
        while (Environment.TickCount64 < addDeadline)
        {
            var index = ++n;
            await InvokeAsync(dispatcher, () => viewModel.CpuBenchAddOne(index)).ConfigureAwait(false);
        }

        SetPhase(phasePath, "scroll");
        var scrollDeadline = Environment.TickCount64 + PhaseMs;
        while (Environment.TickCount64 < scrollDeadline)
        {
            await InvokeAsync(dispatcher, () =>
            {
                if (!viewModel.LoadMoreVisible())
                {
                    viewModel.ResetVisibleForBench();
                }
            }).ConfigureAwait(false);
        }

        SetPhase(phasePath, "filter");
        var filterDeadline = Environment.TickCount64 + PhaseMs;
        var toggle = false;
        while (Environment.TickCount64 < filterDeadline)
        {
            var on = toggle;
            await InvokeAsync(dispatcher, () => viewModel.CpuBenchToggleFilters(on)).ConfigureAwait(false);
            toggle = !toggle;
        }

        SetPhase(phasePath, "done");
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
            tcs.TrySetException(new InvalidOperationException("Failed to enqueue UI work for cpu-bench."));
        }

        return tcs.Task;
    }
}
