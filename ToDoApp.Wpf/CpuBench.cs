using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ToDoApp.Wpf.ViewModels;

namespace ToDoApp.Wpf;

internal static class CpuBench
{
    private const int PhaseMs = 5000;
    private const string Flag = "--cpu-bench";
    private const string PhasePrefix = "--cpu-bench-phase=";

    public static bool IsEnabled(IEnumerable<string>? args) =>
        args != null && args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase));

    public static string ResolvePhasePath(IEnumerable<string>? args)
    {
        var phaseArg = args?.FirstOrDefault(a =>
            a.StartsWith(PhasePrefix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(phaseArg))
        {
            return phaseArg[PhasePrefix.Length..].Trim().Trim('"');
        }

        return Path.Combine(Path.GetTempPath(), "todo_cpu_bench_phase.txt");
    }

    public static void SetPhase(string phasePath, string phase) =>
        File.WriteAllText(phasePath, phase, Encoding.ASCII);

    public static async Task RunAsync(MainWindowViewModel viewModel, string phasePath)
    {
        try
        {
            SetPhase(phasePath, "idle");
            await Task.Delay(PhaseMs).ConfigureAwait(false);

            SetPhase(phasePath, "add");
            var addDeadline = Environment.TickCount64 + PhaseMs;
            var n = 0;
            while (Environment.TickCount64 < addDeadline)
            {
                var index = ++n;
                await InvokeUiAsync(() => viewModel.CpuBenchAddOne(index)).ConfigureAwait(false);
            }

            SetPhase(phasePath, "scroll");
            var scrollDeadline = Environment.TickCount64 + PhaseMs;
            while (Environment.TickCount64 < scrollDeadline)
            {
                await InvokeUiAsync(() =>
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
                await InvokeUiAsync(() => viewModel.CpuBenchToggleFilters(on)).ConfigureAwait(false);
                toggle = !toggle;
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText(phasePath + ".error.txt", ex.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore secondary failures while reporting
            }

            throw;
        }
        finally
        {
            SetPhase(phasePath, "done");
        }
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
}
