"""Toolbar component."""
import wx

from controllers.todo_controller import TodoController


class Toolbar(wx.Panel):
    """Toolbar panel."""
    def __init__(self, parent, controller: TodoController):
        super().__init__(parent)
        self.controller = controller
        self._on_open_in_new_window = None

        self._create_ui()
        self._bind_events()

    def set_on_open_in_new_window(self, callback) -> None:
        """Set callback for opening selected item in a new window."""
        self._on_open_in_new_window = callback

    def _create_ui(self) -> None:
        """Create toolbar UI."""
        sizer = wx.BoxSizer(wx.HORIZONTAL)

        # Add button
        self.add_btn = wx.Button(self, label="+ 新しいアイテム")
        sizer.Add(self.add_btn, flag=wx.ALL, border=5)

        # Delete button
        self.delete_btn = wx.Button(self, label="削除 (0)")
        self.delete_btn.Enable(False)
        sizer.Add(self.delete_btn, flag=wx.ALL, border=5)

        # Open in new window button
        self.open_window_btn = wx.Button(self, label="別ウィンドウで開く")
        self.open_window_btn.Enable(False)
        sizer.Add(self.open_window_btn, flag=wx.ALL, border=5)

        # Spacer
        sizer.AddStretchSpacer()

        # Export button
        self.export_btn = wx.Button(self, label="エクスポート")
        sizer.Add(self.export_btn, flag=wx.ALL, border=5)

        # Import button
        self.import_btn = wx.Button(self, label="インポート")
        sizer.Add(self.import_btn, flag=wx.ALL, border=5)

        # Open data folder button
        self.open_folder_btn = wx.Button(self, label="データフォルダを開く")
        sizer.Add(self.open_folder_btn, flag=wx.ALL, border=5)

        self.SetSizer(sizer)

    def _bind_events(self) -> None:
        """Bind events."""
        self.add_btn.Bind(wx.EVT_BUTTON, self._on_add)
        self.delete_btn.Bind(wx.EVT_BUTTON, self._on_delete)
        self.open_window_btn.Bind(wx.EVT_BUTTON, self._on_open_window)
        self.export_btn.Bind(wx.EVT_BUTTON, self._on_export)
        self.import_btn.Bind(wx.EVT_BUTTON, self._on_import)
        self.open_folder_btn.Bind(wx.EVT_BUTTON, self._on_open_folder)

    def _on_add(self, event: wx.CommandEvent) -> None:
        """Handle add button click."""
        wx.PostEvent(self.GetParent(), wx.CommandEvent(wx.EVT_BUTTON.typeId, -1))

    def _on_delete(self, event: wx.CommandEvent) -> None:
        """Handle delete button click."""
        selected_ids = list(self.controller.get_selected_ids())
        if not selected_ids:
            return

        count = len(selected_ids)
        msg = f"{count}件のアイテムを削除しますか？"
        dlg = wx.MessageDialog(
            self,
            msg,
            "確認",
            wx.YES_NO | wx.ICON_QUESTION
        )

        if dlg.ShowModal() == wx.ID_YES:
            self.controller.delete_items(selected_ids)
            self.controller.save_data()

        dlg.Destroy()

    def _on_open_window(self, event: wx.CommandEvent) -> None:
        """Handle open-in-new-window button click."""
        if self._on_open_in_new_window:
            self._on_open_in_new_window()

    def _on_export(self, event: wx.CommandEvent) -> None:
        """Handle export button click."""
        self.controller.export_data(self)

    def _on_import(self, event: wx.CommandEvent) -> None:
        """Handle import button click."""
        try:
            if self.controller.import_data(self):
                self.controller.save_data()
        except Exception as e:
            wx.MessageBox(
                f"インポートに失敗しました: {e}",
                "エラー",
                wx.OK | wx.ICON_ERROR
            )

    def _on_open_folder(self, event: wx.CommandEvent) -> None:
        """Handle open folder button click."""
        self.controller.open_data_folder()

    def update_selection_count(self, count: int) -> None:
        """Update selection count display."""
        self.delete_btn.SetLabel(f"削除 ({count})")
        self.delete_btn.Enable(count > 0)
        self.open_window_btn.Enable(count == 1)
