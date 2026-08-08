"""UI bench: startup, render_1000, scroll FPS, filter response → JSON."""
from __future__ import annotations

import json
import os
import sys
import tempfile
import threading
import time
from typing import Callable

import wx

SCROLL_SEC = 3.0
FILTER_CYCLES = 10
PAGE_SIZE = 100
FLAG = "--ui-bench"
OUT_PREFIX = "--ui-bench-out="


def process_creation_unix_s() -> float:
    """Windows process creation time as Unix epoch seconds (python.exe for uv run)."""
    import ctypes
    from ctypes import wintypes

    class FILETIME(ctypes.Structure):
        _fields_ = [
            ("dwLowDateTime", wintypes.DWORD),
            ("dwHighDateTime", wintypes.DWORD),
        ]

    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    get_current = kernel32.GetCurrentProcess
    get_current.restype = wintypes.HANDLE
    get_times = kernel32.GetProcessTimes
    get_times.argtypes = [
        wintypes.HANDLE,
        ctypes.POINTER(FILETIME),
        ctypes.POINTER(FILETIME),
        ctypes.POINTER(FILETIME),
        ctypes.POINTER(FILETIME),
    ]
    get_times.restype = wintypes.BOOL

    creation = FILETIME()
    exit_t = FILETIME()
    kernel = FILETIME()
    user = FILETIME()
    if not get_times(get_current(), ctypes.byref(creation), ctypes.byref(exit_t),
                     ctypes.byref(kernel), ctypes.byref(user)):
        return time.time()
    ticks = (creation.dwHighDateTime << 32) | creation.dwLowDateTime
    return (ticks - 116444736000000000) / 10_000_000.0


def parse_ui_bench_args(argv):
    """Return (enabled, out_path, json_path) from argv."""
    enabled = False
    out_path = None
    json_path = None
    for arg in argv[1:]:
        lower = arg.lower()
        if arg == FLAG or lower == FLAG:
            enabled = True
        elif arg.startswith(OUT_PREFIX) or lower.startswith(OUT_PREFIX):
            out_path = arg.split("=", 1)[1].strip().strip('"')
        elif lower.endswith(".json") and os.path.isfile(arg):
            json_path = os.path.abspath(arg)
    if out_path is None:
        out_path = os.path.join(tempfile.gettempdir(), "todo_ui_bench_result.json")
    return enabled, out_path, json_path


def write_ui_bench_result(out_path: str, metrics: dict) -> None:
    parent = os.path.dirname(out_path)
    if parent:
        os.makedirs(parent, exist_ok=True)
    with open(out_path, "w", encoding="utf-8", newline="") as f:
        json.dump(metrics, f, ensure_ascii=False)


def _ui_call(fn: Callable, *args):
    """Run callable on the UI thread and wait."""
    done = threading.Event()
    error: list = []

    def wrapper():
        try:
            fn(*args)
        except Exception as e:  # noqa: BLE001
            error.append(e)
        finally:
            done.set()

    wx.CallAfter(wrapper)
    if not done.wait(timeout=60):
        raise TimeoutError("UI call timed out")
    if error:
        raise error[0]


def _ui_call_result(fn: Callable, *args):
    """Run callable on the UI thread and return its result."""
    done = threading.Event()
    error: list = []
    result: list = []

    def wrapper():
        try:
            result.append(fn(*args))
        except Exception as e:  # noqa: BLE001
            error.append(e)
        finally:
            done.set()

    wx.CallAfter(wrapper)
    if not done.wait(timeout=60):
        raise TimeoutError("UI call timed out")
    if error:
        raise error[0]
    return result[0] if result else None


def run_ui_bench(
    *,
    out_path: str,
    json_path: str,
    process_start_monotonic: float | None = None,
    measure_startup: Callable[[], None],
    import_json: Callable[[str], None],
    wait_import_applied: Callable[[], None],
    expand_or_reset: Callable[[], None],
    toggle_filters: Callable[[bool], None],
    wait_filter_applied: Callable[[], None],
    bind_paint_counter: Callable[[], None],
    unbind_paint_counter: Callable[[], None],
    read_paint_count: Callable[[], int],
    reset_paint_count: Callable[[], None],
    on_done: Callable[[], None],
) -> None:
    """Run UI bench on a worker thread; UI work via CallAfter."""

    def worker():
        metrics = {}
        try:
            _ui_call(measure_startup)
            # Prefer OS process creation (includes interpreter bootstrap).
            try:
                startup_s = max(0.0, time.time() - process_creation_unix_s())
            except Exception:
                base = process_start_monotonic if process_start_monotonic is not None else time.monotonic()
                startup_s = max(0.0, time.monotonic() - base)
            metrics["startup_s"] = round(startup_s, 2)

            render_start = time.monotonic()
            _ui_call(import_json, json_path)
            # Must wait on worker thread — waiting on UI thread deadlocks refresh CallAfter.
            wait_import_applied()
            metrics["render_1000_s"] = round(time.monotonic() - render_start, 2)

            _ui_call(reset_paint_count)
            _ui_call(bind_paint_counter)
            scroll_start = time.monotonic()
            deadline = scroll_start + SCROLL_SEC
            while time.monotonic() < deadline:
                _ui_call(expand_or_reset)
                time.sleep(0)
            elapsed = max(time.monotonic() - scroll_start, 0.001)
            frames = _ui_call_result(read_paint_count) or 0
            _ui_call(unbind_paint_counter)
            metrics["scroll_fps"] = round(frames / elapsed, 2)

            filter_total_ms = 0.0
            on = False
            for _ in range(FILTER_CYCLES):
                cycle_start = time.monotonic()
                _ui_call(toggle_filters, on)
                wait_filter_applied()
                filter_total_ms += (time.monotonic() - cycle_start) * 1000.0
                on = not on
            metrics["filter_response_ms"] = round(filter_total_ms / FILTER_CYCLES, 2)

            write_ui_bench_result(out_path, metrics)
        except Exception as e:  # noqa: BLE001
            print(f"UI bench failed: {e}", file=sys.stderr)
        finally:
            wx.CallAfter(on_done)

    threading.Thread(target=worker, daemon=True).start()
