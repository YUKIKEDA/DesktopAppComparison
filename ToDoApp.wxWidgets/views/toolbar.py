"""Toolbar component."""
import json
import wx

from controllers.todo_controller import TodoController
from utils.theme import ThemeColors, style_brand_button
from utils.platform_integration import copy_text_to_clipboard, show_notification


class Toolbar(wx.Panel):
    """Toolbar panel."""
    def __init__(self, parent, controller: TodoController):
        super().__init__(parent)
        self.controller = controller
        self._on_open_in_new_window = None
        self._on_theme_toggle = None
        self._theme_name = "light"

        self._create_ui()
        self._bind_events()

    def set_on_open_in_new_window(self, callback) -> None:
        """Set callback for opening selected item in a new window."""
        self._on_open_in_new_window = callback

    def set_on_theme_toggle(self, callback) -> None:
        """Set callback for theme toggle."""
        self._on_theme_toggle = callback

    def _create_ui(self) -> None:
        """Create toolbar UI with wrapping layout."""
        sizer = wx.WrapSizer(wx.HORIZONTAL)

        self.add_btn = wx.Button(self, label="+ 新しいアイテム")
        sizer.Add(self.add_btn, flag=wx.ALL, border=5)

        self.delete_btn = wx.Button(self, label="削除 (0)")
        self.delete_btn.Enable(False)
        sizer.Add(self.delete_btn, flag=wx.ALL, border=5)

        self.copy_btn = wx.Button(self, label="コピー")
        self.copy_btn.Enable(False)
        sizer.Add(self.copy_btn, flag=wx.ALL, border=5)

        self.open_window_btn = wx.Button(self, label="別ウィンドウで開く")
        self.open_window_btn.Enable(False)
        sizer.Add(self.open_window_btn, flag=wx.ALL, border=5)

        self.export_btn = wx.Button(self, label="エクスポート")
        sizer.Add(self.export_btn, flag=wx.ALL, border=5)

        self.import_btn = wx.Button(self, label="インポート")
        sizer.Add(self.import_btn, flag=wx.ALL, border=5)

        self.open_folder_btn = wx.Button(self, label="データフォルダを開く")
        sizer.Add(self.open_folder_btn, flag=wx.ALL, border=5)

        self.theme_btn = wx.Button(self, label="テーマ: ライト")
        sizer.Add(self.theme_btn, flag=wx.ALL, border=5)

        self.SetSizer(sizer)

    def _bind_events(self) -> None:
        """Bind events."""
        self.add_btn.Bind(wx.EVT_BUTTON, self._on_add)
        self.delete_btn.Bind(wx.EVT_BUTTON, self._on_delete)
        self.copy_btn.Bind(wx.EVT_BUTTON, self._on_copy)
        self.open_window_btn.Bind(wx.EVT_BUTTON, self._on_open_window)
        self.export_btn.Bind(wx.EVT_BUTTON, self._on_export)
        self.import_btn.Bind(wx.EVT_BUTTON, self._on_import)
        self.open_folder_btn.Bind(wx.EVT_BUTTON, self._on_open_folder)
        self.theme_btn.Bind(wx.EVT_BUTTON, self._on_theme)

    def apply_theme(self, colors: ThemeColors) -> None:
        """Apply theme colours to toolbar controls."""
        self._theme_name = colors.name
        self.SetBackgroundColour(colors.surface)
        self.SetForegroundColour(colors.text)
        style_brand_button(self.add_btn, colors.brand_blue, colors.on_brand)
        style_brand_button(self.delete_btn, colors.brand_red, colors.on_brand)
        for btn in (
            self.copy_btn,
            self.open_window_btn,
            self.export_btn,
            self.import_btn,
            self.open_folder_btn,
            self.theme_btn,
        ):
            btn.SetBackgroundColour(colors.surface_alt)
            btn.SetForegroundColour(colors.text)
        label = "テーマ: ダーク" if colors.name == "dark" else "テーマ: ライト"
        self.theme_btn.SetLabel(label)
        self.Refresh()

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
            self.controller.save_data_async()

        dlg.Destroy()

    def _on_copy(self, event: wx.CommandEvent) -> None:
        """Copy selected items as JSON to clipboard."""
        selected_ids = self.controller.get_selected_ids()
        if not selected_ids:
            return
        items = [
            item.to_dict()
            for item in self.controller.get_items()
            if item.id in selected_ids
        ]
        text = json.dumps(items, ensure_ascii=False, indent=2)
        if copy_text_to_clipboard(text):
            frame = self.GetTopLevelParent()
            if frame and hasattr(frame, "SetStatusText"):
                frame.SetStatusText(f"{len(items)}件をクリップボードにコピーしました")
        else:
            wx.MessageBox("クリップボードへのコピーに失敗しました", "エラー", wx.OK | wx.ICON_ERROR)

    def _on_open_window(self, event: wx.CommandEvent) -> None:
        """Handle open-in-new-window button click."""
        if self._on_open_in_new_window:
            self._on_open_in_new_window()

    def _on_export(self, event: wx.CommandEvent) -> None:
        """Handle export button click."""
        def on_done(ok, err):
            if err is not None:
                wx.MessageBox(
                    f"エクスポートに失敗しました: {err}",
                    "エラー",
                    wx.OK | wx.ICON_ERROR
                )
            elif ok:
                frame = self.GetTopLevelParent()
                if frame and hasattr(frame, "SetStatusText"):
                    frame.SetStatusText("エクスポートしました")

        self.controller.export_data_async(self, on_done=on_done)

    def _on_import(self, event: wx.CommandEvent) -> None:
        """Handle import button click."""
        try:
            def on_done(ok, err):
                if err is not None:
                    wx.MessageBox(
                        f"インポートに失敗しました: {err}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
                elif ok:
                    self.controller.save_data_async()
                    show_notification("Todo App", "インポートしました", self)
                    frame = self.GetTopLevelParent()
                    if frame and hasattr(frame, "SetStatusText"):
                        frame.SetStatusText("インポートしました")

            self.controller.import_data_async(self, on_done=on_done)
        except Exception as e:
            wx.MessageBox(
                f"インポートに失敗しました: {e}",
                "エラー",
                wx.OK | wx.ICON_ERROR
            )

    def _on_open_folder(self, event: wx.CommandEvent) -> None:
        """Handle open folder button click."""
        self.controller.open_data_folder()

    def _on_theme(self, event: wx.CommandEvent) -> None:
        """Handle theme toggle button click."""
        if self._on_theme_toggle:
            self._on_theme_toggle()

    def update_selection_count(self, count: int) -> None:
        """Update selection count display."""
        self.delete_btn.SetLabel(f"削除 ({count})")
        self.delete_btn.Enable(count > 0)
        self.copy_btn.Enable(count > 0)
        self.open_window_btn.Enable(count == 1)
