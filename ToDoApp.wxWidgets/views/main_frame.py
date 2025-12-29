"""Main application frame."""
import wx
import sys
import traceback

from controllers.todo_controller import TodoController
from views.toolbar import Toolbar
from views.filter_bar import FilterBar
from views.todo_table import TodoTable
from views.todo_form_dialog import TodoFormDialog
from models.todo_item import TodoItem


class MainFrame(wx.Frame):
    """Main application frame."""
    def __init__(self):
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

            print("MainFrame.__init__: Creating controller...")
            self.controller = TodoController()
            print("MainFrame.__init__: Controller created")
            
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

            print("MainFrame.__init__: Loading data...")
            self._load_data()
            print("MainFrame.__init__: Data loaded")

            print("MainFrame.__init__: Setting up accelerators...")
            # Setup accelerators for keyboard shortcuts
            self._setup_accelerators()
            print("MainFrame.__init__: Accelerators set up")
            
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
            except:
                pass
            raise

    def _create_ui(self) -> None:
        """Create main UI."""
        try:
            print("_create_ui: Creating panel...")
            panel = wx.Panel(self)
            main_sizer = wx.BoxSizer(wx.VERTICAL)

            print("_create_ui: Creating toolbar...")
            # Toolbar
            self.toolbar = Toolbar(panel, self.controller)
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

            print("_create_ui: Creating status bar...")
            # Status bar
            self.CreateStatusBar()
            self.SetStatusText("準備完了")
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
        """Load data on startup."""
        try:
            print("_load_data: Loading data from controller...")
            self.controller.load_data()
            print("_load_data: Data loaded, refreshing table...")
            self._refresh_table()
            print("_load_data: Table refreshed")
        except Exception as e:
            error_msg = f"データの読み込みに失敗しました: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in _load_data: {error_msg}", file=sys.stderr)
            try:
                wx.LogError(error_msg)
                wx.MessageBox(
                    f"データの読み込みに失敗しました: {e}\n空の状態で開始します。",
                    "警告",
                    wx.OK | wx.ICON_WARNING
                )
            except:
                pass
            # Continue with empty data
            try:
                self._refresh_table()
            except Exception as e2:
                print(f"ERROR refreshing table after load failure: {e2}", file=sys.stderr)

    def _on_data_changed(self) -> None:
        """Handle data change callback."""
        try:
            print("_on_data_changed: Called")
            self._refresh_table()
            print("_on_data_changed: Table refreshed")
            selected_count = len(self.controller.get_selected_ids())
            self.toolbar.update_selection_count(selected_count)
            print("_on_data_changed: Selection count updated")
            
            # Schedule auto-save (restart timer with debounce)
            self._auto_save_timer.Stop()
            self._auto_save_timer.StartOnce(2000)  # 2 seconds in milliseconds
            print("_on_data_changed: Auto-save scheduled")
        except Exception as e:
            error_msg = f"データ変更コールバックエラー: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in _on_data_changed: {error_msg}", file=sys.stderr)
            try:
                wx.LogError(error_msg)
            except:
                pass

    def _on_auto_save_timer(self, event: wx.TimerEvent) -> None:
        """Handle auto-save timer."""
        try:
            self.controller.save_data()
        except Exception as e:
            wx.LogError(f"Auto-save failed: {e}")

    def _refresh_table(self) -> None:
        """Refresh table display."""
        try:
            # Get filters
            filters = self.filter_bar.get_filters()

            # Get filtered items
            filtered_items = self.controller.get_filtered_items(filters)

            # Get sorts
            sorts = self.table.get_sorts()

            # Get sorted items
            sorted_items = self.controller.get_sorted_items(filtered_items, sorts)

            # Update table
            self.table.set_items(sorted_items)
            self.table.set_selected_ids(self.controller.get_selected_ids())

            # Update status bar
            total_count = len(self.controller.get_items())
            filtered_count = len(sorted_items)
            self.SetStatusText(f"合計: {total_count}件 / 表示: {filtered_count}件")
        except Exception as e:
            wx.LogError(f"Error refreshing table: {e}")
            import traceback
            wx.LogError(traceback.format_exc())
            self.SetStatusText(f"エラー: {e}")

    def _on_add_item(self, event: wx.Event) -> None:
        """Handle add item."""
        dlg = TodoFormDialog(self)
        if dlg.ShowModal() == wx.ID_OK:
            result = dlg.get_result()
            if result:
                try:
                    self.controller.add_item(result)
                    self.controller.save_data()
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
        dlg = TodoFormDialog(self, item)
        if dlg.ShowModal() == wx.ID_OK:
            result = dlg.get_result()
            if result:
                try:
                    self.controller.update_item(item.id, result)
                    self.controller.save_data()
                except Exception as e:
                    wx.MessageBox(
                        f"アイテムの更新に失敗しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
        dlg.Destroy()

    def _on_filter_change(self, event: wx.Event) -> None:
        """Handle filter change."""
        self._refresh_table()

    def _on_sort_change(self, event: wx.Event) -> None:
        """Handle sort change."""
        self._refresh_table()

    def _on_save(self, event: wx.Event) -> None:
        """Handle save (Ctrl+S)."""
        self.controller.save_data()
        self.SetStatusText("保存しました")

    def _on_focus_search(self, event: wx.Event) -> None:
        """Handle focus search (Ctrl+F)."""
        self.filter_bar.search_text.SetFocus()

    def _on_delete_selected(self, event: wx.Event) -> None:
        """Handle delete selected (Delete key)."""
        selected_ids = list(self.controller.get_selected_ids())
        if selected_ids:
            self.toolbar._on_delete(wx.CommandEvent())

    def _on_close(self, event: wx.CloseEvent) -> None:
        """Handle window close."""
        # Stop auto-save timer
        self._auto_save_timer.Stop()
        
        # Save on close
        try:
            self.controller.save_data()
        except Exception as e:
            wx.LogError(f"Save on close failed: {e}")
        
        event.Skip()

