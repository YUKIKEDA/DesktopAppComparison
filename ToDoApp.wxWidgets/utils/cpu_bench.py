"""CPU bench protocol: idle → add → scroll → filter → done."""
from __future__ import annotations

import os
import sys
import tempfile
import threading
import time
from typing import Callable

import wx

PHASE_MS = 5.0
PAGE_SIZE = 100
FLAG = "--cpu-bench"
PHASE_PREFIX = "--cpu-bench-phase="


def parse_cpu_bench_args(argv):
    """Return (enabled, phase_path) from argv."""
    enabled = False
    phase_path = None
    for arg in argv[1:]:
        if arg == FLAG or arg.lower() == FLAG:
            enabled = True
        elif arg.startswith(PHASE_PREFIX) or arg.lower().startswith(PHASE_PREFIX):
            phase_path = arg.split("=", 1)[1].strip().strip('"')
    if phase_path is None:
        phase_path = os.path.join(tempfile.gettempdir(), "todo_cpu_bench_phase.txt")
    return enabled, phase_path


def write_phase(phase_path: str, phase: str) -> None:
    with open(phase_path, "w", encoding="ascii", newline="") as f:
        f.write(phase)


def _ui_call(fn: Callable, *args) -> None:
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
    if not done.wait(timeout=30):
        raise TimeoutError("UI call timed out")
    if error:
        raise error[0]


def run_cpu_bench(
    *,
    phase_path: str,
    add_one: Callable[[int], None],
    expand_or_reset: Callable[[], None],
    toggle_filters: Callable[[bool], None],
    on_done: Callable[[], None],
) -> None:
    """Run phases on a worker thread; UI work via CallAfter."""

    def worker():
        try:
            write_phase(phase_path, "idle")
            time.sleep(PHASE_MS)

            write_phase(phase_path, "add")
            deadline = time.monotonic() + PHASE_MS
            n = 0
            while time.monotonic() < deadline:
                n += 1
                _ui_call(add_one, n)
                time.sleep(0)

            # Let pending table refresh settle before scroll phase
            time.sleep(0.25)

            write_phase(phase_path, "scroll")
            deadline = time.monotonic() + PHASE_MS
            while time.monotonic() < deadline:
                _ui_call(expand_or_reset)
                time.sleep(0)

            write_phase(phase_path, "filter")
            deadline = time.monotonic() + PHASE_MS
            on = False
            while time.monotonic() < deadline:
                flag = on
                _ui_call(toggle_filters, flag)
                on = not on
                time.sleep(0)

            write_phase(phase_path, "done")
        except Exception as e:  # noqa: BLE001
            print(f"CPU bench failed: {e}", file=sys.stderr)
            try:
                write_phase(phase_path, "error")
            except Exception:
                pass
        finally:
            wx.CallAfter(on_done)

    threading.Thread(target=worker, daemon=True).start()
