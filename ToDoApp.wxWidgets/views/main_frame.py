"""Main application frame."""
import threading
import wx
import sys
import time
import traceback

from controllers.todo_controller import TodoController
from views.toolbar import Toolbar
from views.filter_bar import FilterBar
from views.todo_table import TodoTable
from views.todo_form_dialog import TodoFormDialog
from views.detail_frame import TodoDetailFrame
from models.todo_item import TodoItem
from utils.theme import get_theme
from utils.platform_integration import TodoTaskBarIcon, show_notification


class JsonFileDropTarget(wx.FileDropTarget):
    """Accept dropped .json files for import."""

    def __init__(self, callback):
        super().__init__()
        self._callback = callback

    def OnDropFiles(self, x, y, filenames):
        for path in filenames:
            if str(path).lower().endswith(".json"):
                self._callback(path)
        return True


class MainFrame(wx.Frame):
    """Main application frame."""
    def __init__(
        self,
        startup_json_paths=None,
        cpu_bench=False,
        cpu_bench_phase=None,
        ui_bench=False,
        ui_bench_out=None,
        ui_bench_json=None,
        process_start_monotonic=None,
    ):
        print("MainFrame.__init__: Starting...")
        try:
            print("MainFrame.__init__: Creating frame...")
            super().__init__(
                None,
                title="Todo App - wxPython",
                size=(1400, 900),
                style=wx.DEFAULT_FRAME_STYLE
            )
            print("MainFrame.__init__: Frame created")

            self._allow_close = False
            self._tray_icon = None
            self._startup_json_paths = list(startup_json_paths or [])
            self._cpu_bench = bool(cpu_bench)
            self._cpu_bench_phase = cpu_bench_phase
            self._cpu_bench_active = False
            self._cpu_bench_pending_imports = 0
            self._ui_bench = bool(ui_bench)
            self._ui_bench_out = ui_bench_out
            self._ui_bench_json = ui_bench_json
            self._ui_bench_active = False
            self._process_start_monotonic = process_start_monotonic or time.monotonic()
            self._paint_count = 0
            self._paint_bound = False
            self._bench_refresh_wait = None
            self._refresh_generation = 0

            print("MainFrame.__init__: Creating controller...")
            self.controller = TodoController()
            print("MainFrame.__init__: Controller created")

            self._detail_frames = {}
            self._theme_name = self.controller.load_theme()

            print("MainFrame.__init__: Adding callback...")
            self.controller.add_callback(self._on_data_changed)
            print("MainFrame.__init__: Callback added")

            print("MainFrame.__init__: Creating timer...")
            # Auto-save timer (2 seconds debounce)
            self._auto_save_timer = wx.Timer(self)
            self.Bind(wx.EVT_TIMER, self._on_auto_save_timer, self._auto_save_timer)
            print("MainFrame.__init__: Timer created")

            print("MainFrame.__init__: Creating UI...")
            self._create_ui()
            print("MainFrame.__init__: UI created")

            print("MainFrame.__init__: Binding events...")
            self._bind_events()
            print("MainFrame.__init__: Events bound")

            print("MainFrame.__init__: Restoring window geometry...")
            self._restore_window_geometry()
            print("MainFrame.__init__: Window geometry restored")

            print("MainFrame.__init__: Applying transparency...")
            self._apply_transparency()
            print("MainFrame.__init__: Transparency applied")

            print("MainFrame.__init__: Setting up drag and drop...")
            self.SetDropTarget(JsonFileDropTarget(self._on_drop_json))
            print("MainFrame.__init__: Drag and drop set up")

            print("MainFrame.__init__: Setting up system tray...")
            self._tray_icon = TodoTaskBarIcon(self)
            print("MainFrame.__init__: System tray set up")

            print("MainFrame.__init__: Loading data...")
            self._load_data()
            print("MainFrame.__init__: Data loaded")

            print("MainFrame.__init__: Setting up accelerators...")
            # Setup accelerators for keyboard shortcuts
            self._setup_accelerators()
            print("MainFrame.__init__: Accelerators set up")

            if self._cpu_bench or self._ui_bench:
                # Import + bench chained from load callback
                pass
            elif self._startup_json_paths:
                wx.CallAfter(self._import_startup_paths)

            print("MainFrame.__init__: Initialization complete")
        except Exception as e:
            import traceback
            error_msg = f"フレームの初期化に失敗しました: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in MainFrame.__init__: {error_msg}", file=sys.stderr)
            try:
                wx.LogError(error_msg)
                wx.MessageBox(
                    error_msg,
                    "エラー",
                    wx.OK | wx.ICON_ERROR
                )
            except Exception:
                pass
            raise

    def _import_startup_paths(self) -> None:
        """Import .json paths from argv after UI is ready."""
        for path in self._startup_json_paths:
            try:
                self.controller.import_from_path_async(
                    path,
                    parent=self,
                    on_done=lambda ok, err, p=path: self._on_import_done(ok, err, p),
                )
            except Exception as e:
                wx.MessageBox(
                    f"インポートに失敗しました: {e}",
                    "エラー",
                    wx.OK | wx.ICON_ERROR
                )

    def _on_import_done(self, ok: bool, err, path: str = "") -> None:
        if not ok:
            if self._cpu_bench:
                self._cpu_bench_pending_imports = max(
                    0, self._cpu_bench_pending_imports - 1
                )
                if self._cpu_bench_pending_imports <= 0:
                    wx.CallAfter(self._start_cpu_bench)
            elif self._ui_bench:
                self._signal_bench_refresh()
            return
        if self._ui_bench and not self._ui_bench_active:
            self.SetStatusText(f"インポートしました: {path}" if path else "インポートしました")
            self._signal_bench_refresh()
            return
        if not self._cpu_bench_active and not self._cpu_bench:
            self.controller.save_data_async()
            show_notification("Todo App", "インポートしました", self)
            self.SetStatusText(f"インポートしました: {path}" if path else "インポートしました")
        elif self._cpu_bench and not self._cpu_bench_active:
            # Apply import without notification; skip save for bench
            self.SetStatusText(f"インポートしました: {path}" if path else "インポートしました")
            self._cpu_bench_pending_imports = max(
                0, self._cpu_bench_pending_imports - 1
            )
            if self._cpu_bench_pending_imports <= 0:
                wx.CallAfter(self._start_cpu_bench)

    def _create_ui(self) -> None:
        """Create main UI."""
        try:
            print("_create_ui: Creating panel...")
            panel = wx.Panel(self)
            main_sizer = wx.BoxSizer(wx.VERTICAL)

            print("_create_ui: Creating toolbar...")
            # Toolbar
            self.toolbar = Toolbar(panel, self.controller)
            self.toolbar.set_on_open_in_new_window(self._on_open_in_new_window)
            self.toolbar.set_on_theme_toggle(self._on_theme_toggle)
            main_sizer.Add(self.toolbar, flag=wx.EXPAND)
            print("_create_ui: Toolbar created")

            print("_create_ui: Creating filter bar...")
            # Filter bar
            self.filter_bar = FilterBar(panel, self.controller)
            main_sizer.Add(self.filter_bar, flag=wx.EXPAND)
            print("_create_ui: Filter bar created")

            print("_create_ui: Creating table...")
            # Table
            self.table = TodoTable(panel, self.controller)
            self.table.set_on_edit(self._on_edit_item)
            main_sizer.Add(self.table, proportion=1, flag=wx.EXPAND | wx.ALL, border=5)
            print("_create_ui: Table created")

            print("_create_ui: Setting sizer...")
            panel.SetSizer(main_sizer)
            self._main_panel = panel

            print("_create_ui: Creating status bar...")
            # Status bar
            self.CreateStatusBar()
            self.SetStatusText("準備完了")

            print("_create_ui: Applying theme...")
            self._apply_theme()
            print("_create_ui: UI creation complete")
        except Exception as e:
            error_msg = f"UI作成エラー: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in _create_ui: {error_msg}", file=sys.stderr)
            raise

    def _bind_events(self) -> None:
        """Bind events."""
        # Custom event for add button
        self.Bind(wx.EVT_BUTTON, self._on_add_item, id=-1)
        # Custom event for filter change
        self.Bind(wx.EVT_BUTTON, self._on_filter_change, id=-2)
        # Custom event for sort change
        self.Bind(wx.EVT_BUTTON, self._on_sort_change, id=-3)

        # Window close
        self.Bind(wx.EVT_CLOSE, self._on_close)

    def _restore_window_geometry(self) -> None:
        """Restore window position and size from window.json."""
        geometry = self.controller.load_window_geometry()
        if not geometry:
            return

        width = geometry["width"]
        height = geometry["height"]
        x = geometry["x"]
        y = geometry["y"]

        display = wx.Display.GetFromWindow(self)
        if display == wx.NOT_FOUND:
            display = 0
        client_area = wx.Display(display).GetClientArea()
        rect = wx.Rect(x, y, width, height)
        if not client_area.Intersects(rect):
            return

        self.SetSize(width, height)
        self.SetPosition((x, y))

    def _apply_transparency(self) -> None:
        """Apply slight window transparency (~0.95)."""
        try:
            if self.CanSetTransparent():
                self.SetTransparent(242)
        except Exception as e:
            print(f"Transparency not applied: {e}", file=sys.stderr)

    def _apply_theme(self) -> None:
        """Apply light/dark palette to main window controls."""
        colors = get_theme(self._theme_name)
        self.SetBackgroundColour(colors.bg)
        if hasattr(self, "_main_panel") and self._main_panel:
            self._main_panel.SetBackgroundColour(colors.bg)
            self._main_panel.SetForegroundColour(colors.text)
        self.toolbar.apply_theme(colors)
        self.filter_bar.apply_theme(colors)
        try:
            self.table.SetBackgroundColour(colors.surface)
            self.table.SetForegroundColour(colors.text)
            self.table.Refresh()
        except Exception:
            pass
        self.Refresh()
        self.Update()

    def _on_theme_toggle(self) -> None:
        """Toggle and persist theme."""
        self._theme_name = "light" if self._theme_name == "dark" else "dark"
        self.controller.save_theme(self._theme_name)
        self._apply_theme()
        self.SetStatusText(
            "テーマ: ダーク" if self._theme_name == "dark" else "テーマ: ライト"
        )

    def _setup_accelerators(self) -> None:
        """Setup keyboard accelerators."""
        accel_table = wx.AcceleratorTable([
            (wx.ACCEL_CTRL, ord('N'), wx.ID_NEW),
            (wx.ACCEL_CTRL, ord('S'), wx.ID_SAVE),
            (wx.ACCEL_CTRL, ord('F'), wx.ID_FIND),
            (wx.ACCEL_NORMAL, wx.WXK_DELETE, wx.ID_DELETE),
        ])

        self.SetAcceleratorTable(accel_table)

        self.Bind(wx.EVT_MENU, self._on_add_item, id=wx.ID_NEW)
        self.Bind(wx.EVT_MENU, self._on_save, id=wx.ID_SAVE)
        self.Bind(wx.EVT_MENU, self._on_focus_search, id=wx.ID_FIND)
        self.Bind(wx.EVT_MENU, self._on_delete_selected, id=wx.ID_DELETE)

    def _load_data(self) -> None:
        """Load data on startup asynchronously."""
        try:
            print("_load_data: Loading data from controller (async)...")
            self.SetStatusText("読み込み中...")

            def on_done(err):
                if err is not None:
                    wx.MessageBox(
                        f"データの読み込みに失敗しました: {err}\n空の状態で開始します。",
                        "警告",
                        wx.OK | wx.ICON_WARNING
                    )
                self._refresh_table()
                self.SetStatusText("準備完了")
                if self._ui_bench:
                    wx.CallAfter(self._ui_bench_after_load)
                elif self._cpu_bench:
                    wx.CallAfter(self._cpu_bench_after_load)

            self.controller.load_data_async(on_done=on_done)
        except Exception as e:
            error_msg = f"データの読み込みに失敗しました: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in _load_data: {error_msg}", file=sys.stderr)
            try:
                self._refresh_table()
            except Exception as e2:
                print(f"ERROR refreshing table after load failure: {e2}", file=sys.stderr)
            if self._ui_bench:
                wx.CallAfter(self._ui_bench_after_load)
            elif self._cpu_bench:
                wx.CallAfter(self._cpu_bench_after_load)

    def _signal_bench_refresh(self) -> None:
        waiter = self._bench_refresh_wait
        if waiter is not None:
            waiter.set()

    def _wait_bench_refresh(self) -> None:
        waiter = self._bench_refresh_wait
        if waiter is None:
            wx.YieldIfNeeded()
            return
        deadline = time.monotonic() + 30
        while time.monotonic() < deadline:
            if waiter.is_set():
                self._bench_refresh_wait = None
                return
            time.sleep(0.005)
        raise TimeoutError("UI refresh timed out during ui-bench")

    def _ui_bench_after_load(self) -> None:
        """After project load, run UI bench (imports json inside bench)."""
        if self._ui_bench_active:
            return
        self._ui_bench_active = True
        try:
            self._auto_save_timer.Stop()
        except Exception:
            pass

        from utils.ui_bench import run_ui_bench, PAGE_SIZE

        json_path = self._ui_bench_json
        if not json_path and self._startup_json_paths:
            json_path = self._startup_json_paths[0]
        if not json_path:
            print("UI bench: missing project JSON path", file=sys.stderr)
            wx.CallAfter(self._ui_bench_finish)
            return

        def measure_startup() -> None:
            wx.YieldIfNeeded()

        def import_json(path: str) -> None:
            self._bench_refresh_wait = threading.Event()
            ok = self.controller.import_from_path(path)
            if not ok:
                raise RuntimeError(f"Failed to import {path}")
            self._refresh_table()

        def expand_or_reset() -> None:
            if not self.table.expand_visible(PAGE_SIZE):
                self.table.reset_visible()
            wx.YieldIfNeeded()

        def toggle_filters(on: bool) -> None:
            self._bench_refresh_wait = threading.Event()
            self.filter_bar.set_bench_filters(on)
            self._refresh_table()
            wx.YieldIfNeeded()

        def bind_paint_counter() -> None:
            if self._paint_bound:
                return
            self._paint_count = 0
            self.table.Bind(wx.EVT_PAINT, self._on_ui_bench_paint)
            self._paint_bound = True

        def unbind_paint_counter() -> None:
            if not self._paint_bound:
                return
            self.table.Unbind(wx.EVT_PAINT, handler=self._on_ui_bench_paint)
            self._paint_bound = False

        def read_paint_count() -> int:
            return self._paint_count

        def reset_paint_count() -> None:
            self._paint_count = 0

        run_ui_bench(
            out_path=self._ui_bench_out,
            json_path=json_path,
            process_start_monotonic=self._process_start_monotonic,
            measure_startup=measure_startup,
            import_json=import_json,
            wait_import_applied=self._wait_bench_refresh,
            expand_or_reset=expand_or_reset,
            toggle_filters=toggle_filters,
            wait_filter_applied=self._wait_bench_refresh,
            bind_paint_counter=bind_paint_counter,
            unbind_paint_counter=unbind_paint_counter,
            read_paint_count=read_paint_count,
            reset_paint_count=reset_paint_count,
            on_done=self._ui_bench_finish,
        )

    def _on_ui_bench_paint(self, event: wx.PaintEvent) -> None:
        self._paint_count += 1
        event.Skip()

    def _ui_bench_finish(self) -> None:
        """Exit after writing UI bench JSON."""
        self._allow_close = True
        try:
            if self._tray_icon:
                self._tray_icon.RemoveIcon()
                self._tray_icon.Destroy()
                self._tray_icon = None
        except Exception:
            pass
        try:
            self._cleanup_resources()
        except Exception:
            pass
        self.Destroy()
        app = wx.GetApp()
        if app:
            app.ExitMainLoop()
        sys.exit(0)

    def _cpu_bench_after_load(self) -> None:
        """After project load, import argv JSON then start CPU bench."""
        if self._startup_json_paths:
            self._cpu_bench_pending_imports = len(self._startup_json_paths)
            for path in self._startup_json_paths:
                try:
                    self.controller.import_from_path_async(
                        path,
                        parent=self,
                        on_done=lambda ok, err, p=path: self._on_import_done(ok, err, p),
                    )
                except Exception as e:
                    print(f"CPU bench import failed: {e}", file=sys.stderr)
                    self._cpu_bench_pending_imports -= 1
            if self._cpu_bench_pending_imports <= 0:
                self._start_cpu_bench()
        else:
            self._start_cpu_bench()

    def _start_cpu_bench(self) -> None:
        """Begin CPU bench phases."""
        if self._cpu_bench_active:
            return
        self._cpu_bench_active = True
        try:
            self._auto_save_timer.Stop()
        except Exception:
            pass

        from utils.cpu_bench import run_cpu_bench, PAGE_SIZE

        def add_one(n: int) -> None:
            self.controller.add_item({
                "title": f"bench-{n}",
                "description": "",
                "status": "未着手",
                "priority": "中",
                "dueDate": None,
                "isCompleted": False,
            })
            wx.YieldIfNeeded()

        def expand_or_reset() -> None:
            if not self.table.expand_visible(PAGE_SIZE):
                self.table.reset_visible()
            wx.YieldIfNeeded()

        def toggle_filters(on: bool) -> None:
            self.filter_bar.set_bench_filters(on)
            wx.YieldIfNeeded()

        def on_done() -> None:
            self._cpu_bench_finish()

        run_cpu_bench(
            phase_path=self._cpu_bench_phase,
            add_one=add_one,
            expand_or_reset=expand_or_reset,
            toggle_filters=toggle_filters,
            on_done=on_done,
        )

    def _cpu_bench_finish(self) -> None:
        """Exit after writing done phase."""
        self._allow_close = True
        try:
            if self._tray_icon:
                self._tray_icon.RemoveIcon()
                self._tray_icon.Destroy()
                self._tray_icon = None
        except Exception:
            pass
        try:
            self._cleanup_resources()
        except Exception:
            pass
        self.Destroy()
        app = wx.GetApp()
        if app:
            app.ExitMainLoop()
        sys.exit(0)

    def _on_data_changed(self) -> None:
        """Handle data change callback."""
        try:
            print("_on_data_changed: Called")
            self._refresh_table()
            print("_on_data_changed: Table refresh scheduled")
            selected_count = len(self.controller.get_selected_ids())
            self.toolbar.update_selection_count(selected_count)
            print("_on_data_changed: Selection count updated")

            self._sync_detail_frames()

            # Schedule auto-save (restart timer with debounce) — skip for bench runs
            if not self._cpu_bench and not self._ui_bench:
                self._auto_save_timer.Stop()
                self._auto_save_timer.StartOnce(2000)  # 2 seconds in milliseconds
                print("_on_data_changed: Auto-save scheduled")
        except Exception as e:
            error_msg = f"データ変更コールバックエラー: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in _on_data_changed: {error_msg}", file=sys.stderr)
            try:
                wx.LogError(error_msg)
            except Exception:
                pass

    def _sync_detail_frames(self) -> None:
        """Close detail frames whose items were deleted."""
        closed_ids = []
        for item_id, frame in list(self._detail_frames.items()):
            if not frame:
                closed_ids.append(item_id)
                continue
            if self.controller.get_item_by_id(item_id) is None:
                try:
                    frame.Destroy()
                except Exception:
                    pass
                closed_ids.append(item_id)
        for item_id in closed_ids:
            self._detail_frames.pop(item_id, None)

    def _on_auto_save_timer(self, event: wx.TimerEvent) -> None:
        """Handle auto-save timer."""
        try:
            self.controller.save_data_async()
        except Exception as e:
            wx.LogError(f"Auto-save failed: {e}")

    def _refresh_table(self) -> None:
        """Refresh table display — filter/sort off the UI thread."""
        try:
            filters = list(self.filter_bar.get_filters())
            sorts = list(self.table.get_sorts())
            items_snapshot = list(self.controller.get_items())
            selected_ids = set(self.controller.get_selected_ids())
            generation = self._refresh_generation + 1
            self._refresh_generation = generation

            def worker():
                try:
                    filtered_items = self.controller.get_filtered_items(
                        filters, items=items_snapshot
                    )
                    sorted_items = self.controller.get_sorted_items(
                        filtered_items, sorts
                    )
                except Exception as e:
                    print(f"ERROR filter/sort worker: {e}", file=sys.stderr)
                    sorted_items = items_snapshot

                def apply():
                    if generation != self._refresh_generation:
                        return
                    try:
                        self.table.set_items(sorted_items, reset_visible=True)
                        self.table.set_selected_ids(selected_ids)
                        total_count = len(items_snapshot)
                        filtered_count = len(sorted_items)
                        self.SetStatusText(
                            f"合計: {total_count}件 / 表示: {filtered_count}件"
                        )
                        self._signal_bench_refresh()
                    except Exception as e:
                        wx.LogError(f"Error applying table refresh: {e}")

                wx.CallAfter(apply)

            threading.Thread(target=worker, daemon=True).start()
        except Exception as e:
            wx.LogError(f"Error refreshing table: {e}")
            wx.LogError(traceback.format_exc())
            self.SetStatusText(f"エラー: {e}")

    def _on_add_item(self, event: wx.Event) -> None:
        """Handle add item."""
        dlg = TodoFormDialog(self, theme_name=self._theme_name)
        if dlg.ShowModal() == wx.ID_OK:
            result = dlg.get_result()
            if result:
                try:
                    self.controller.add_item(result)
                    self.controller.save_data_async()
                except ValueError as e:
                    wx.MessageBox(
                        f"アイテムの追加に失敗しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
                except Exception as e:
                    wx.MessageBox(
                        f"予期しないエラーが発生しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
        dlg.Destroy()

    def _on_edit_item(self, item: TodoItem) -> None:
        """Handle edit item."""
        dlg = TodoFormDialog(self, item, theme_name=self._theme_name)
        if dlg.ShowModal() == wx.ID_OK:
            result = dlg.get_result()
            if result:
                try:
                    self.controller.update_item(item.id, result)
                    self.controller.save_data_async()
                except Exception as e:
                    wx.MessageBox(
                        f"アイテムの更新に失敗しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
        dlg.Destroy()

    def _on_open_in_new_window(self) -> None:
        """Open the single selected item in a real secondary Frame."""
        selected_ids = list(self.controller.get_selected_ids())
        if len(selected_ids) != 1:
            return

        item_id = selected_ids[0]
        existing = self._detail_frames.get(item_id)
        if existing:
            existing.Raise()
            existing.SetFocus()
            return

        item = self.controller.get_item_by_id(item_id)
        if item is None:
            return

        def on_save(saved_id: int, updates: dict) -> None:
            try:
                self.controller.update_item(saved_id, updates)
                self.controller.save_data_async()
            except Exception as e:
                wx.MessageBox(
                    f"アイテムの更新に失敗しました: {e}",
                    "エラー",
                    wx.OK | wx.ICON_ERROR,
                    self
                )

        def on_close() -> None:
            self._detail_frames.pop(item_id, None)

        frame = TodoDetailFrame(self, item, on_save=on_save, on_close=on_close)
        self._detail_frames[item_id] = frame
        frame.Show()

    def _on_drop_json(self, path: str) -> None:
        """Import dropped JSON file."""
        try:
            self.controller.import_from_path_async(
                path,
                parent=self,
                on_done=lambda ok, err, p=path: self._on_import_done(ok, err, p),
            )
        except Exception as e:
            wx.MessageBox(
                f"インポートに失敗しました: {e}",
                "エラー",
                wx.OK | wx.ICON_ERROR
            )

    def _on_filter_change(self, event: wx.Event) -> None:
        """Handle filter change."""
        self._refresh_table()

    def _on_sort_change(self, event: wx.Event) -> None:
        """Handle sort change."""
        self._refresh_table()

    def _on_save(self, event: wx.Event) -> None:
        """Handle save (Ctrl+S)."""
        def on_done(err):
            if err is None:
                show_notification("Todo App", "保存しました", self)
                self.SetStatusText("保存しました")
            else:
                wx.MessageBox(
                    f"保存に失敗しました: {err}",
                    "エラー",
                    wx.OK | wx.ICON_ERROR
                )

        self.controller.save_data_async(on_done=on_done)

    def _on_focus_search(self, event: wx.Event) -> None:
        """Handle focus search (Ctrl+F)."""
        self.filter_bar.search_text.SetFocus()

    def _on_delete_selected(self, event: wx.Event) -> None:
        """Handle delete selected (Delete key)."""
        selected_ids = list(self.controller.get_selected_ids())
        if selected_ids:
            self.toolbar._on_delete(wx.CommandEvent())

    def _persist_and_save(self) -> None:
        """Persist window geometry and todo data."""
        try:
            size = self.GetSize()
            pos = self.GetPosition()
            self.controller.save_window_geometry(pos.x, pos.y, size.width, size.height)
        except Exception as e:
            wx.LogError(f"Save window geometry failed: {e}")

        try:
            self.controller.save_data()
        except Exception as e:
            wx.LogError(f"Save on close failed: {e}")

    def _cleanup_resources(self) -> None:
        """Dispose timers, detail frames, and table buffers."""
        try:
            self._auto_save_timer.Stop()
        except Exception:
            pass
        self._refresh_generation += 1

        for frame in list(self._detail_frames.values()):
            try:
                frame.Destroy()
            except Exception:
                pass
        self._detail_frames.clear()

        try:
            self.table.clear_items()
        except Exception:
            pass

    def _on_close(self, event: wx.CloseEvent) -> None:
        """Handle window close — hide to tray unless quitting."""
        if not self._allow_close:
            self._persist_and_save()
            self.Hide()
            event.Veto()
            return

        # Real quit from tray
        self._persist_and_save()
        self._cleanup_resources()

        if self._tray_icon:
            try:
                self._tray_icon.RemoveIcon()
                self._tray_icon.Destroy()
            except Exception:
                pass
            self._tray_icon = None

        event.Skip()
