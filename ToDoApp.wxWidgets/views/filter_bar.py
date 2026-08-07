"""Filter bar component."""
import wx

from controllers.todo_controller import TodoController
from utils.constants import STATUS_OPTIONS, PRIORITY_OPTIONS
from utils.theme import ThemeColors


class FilterBar(wx.Panel):
    """Filter bar panel."""
    def __init__(self, parent, controller: TodoController):
        super().__init__(parent)
        self.controller = controller
        self._filters: list = []

        self._create_ui()
        self._bind_events()

    def _create_ui(self) -> None:
        """Create filter bar UI with wrapping layout."""
        sizer = wx.WrapSizer(wx.HORIZONTAL)

        self.search_label = wx.StaticText(self, label="検索:")
        sizer.Add(self.search_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.search_text = wx.TextCtrl(self, style=wx.TE_PROCESS_ENTER, size=(220, -1))
        self.search_text.SetHint("タイトル・説明で検索...")
        sizer.Add(self.search_text, flag=wx.ALL | wx.EXPAND, border=5)

        self.status_label = wx.StaticText(self, label="ステータス:")
        sizer.Add(self.status_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.status_combo = wx.ComboBox(
            self,
            choices=["すべてのステータス"] + STATUS_OPTIONS,
            style=wx.CB_READONLY
        )
        self.status_combo.SetSelection(0)
        sizer.Add(self.status_combo, flag=wx.ALL, border=5)

        self.priority_label = wx.StaticText(self, label="優先度:")
        sizer.Add(self.priority_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.priority_combo = wx.ComboBox(
            self,
            choices=["すべての優先度"] + PRIORITY_OPTIONS,
            style=wx.CB_READONLY
        )
        self.priority_combo.SetSelection(0)
        sizer.Add(self.priority_combo, flag=wx.ALL, border=5)

        self.clear_btn = wx.Button(self, label="クリア")
        self.clear_btn.Enable(False)
        sizer.Add(self.clear_btn, flag=wx.ALL, border=5)

        self.SetSizer(sizer)

    def apply_theme(self, colors: ThemeColors) -> None:
        """Apply theme colours."""
        self.SetBackgroundColour(colors.surface_alt)
        self.SetForegroundColour(colors.text)
        for label in (self.search_label, self.status_label, self.priority_label):
            label.SetForegroundColour(colors.text)
            label.SetBackgroundColour(colors.surface_alt)
        self.search_text.SetBackgroundColour(colors.surface)
        self.search_text.SetForegroundColour(colors.text)
        self.status_combo.SetBackgroundColour(colors.surface)
        self.status_combo.SetForegroundColour(colors.text)
        self.priority_combo.SetBackgroundColour(colors.surface)
        self.priority_combo.SetForegroundColour(colors.text)
        self.clear_btn.SetBackgroundColour(colors.surface)
        self.clear_btn.SetForegroundColour(colors.text)
        self.Refresh()

    def _bind_events(self) -> None:
        """Bind events."""
        self.search_text.Bind(wx.EVT_TEXT, self._on_filter_change)
        self.search_text.Bind(wx.EVT_TEXT_ENTER, self._on_filter_change)
        self.status_combo.Bind(wx.EVT_COMBOBOX, self._on_filter_change)
        self.priority_combo.Bind(wx.EVT_COMBOBOX, self._on_filter_change)
        self.clear_btn.Bind(wx.EVT_BUTTON, self._on_clear)

    def _on_filter_change(self, event: wx.Event) -> None:
        """Handle filter change."""
        self._update_filters()
        self._update_clear_button()
        wx.PostEvent(self.GetParent(), wx.CommandEvent(wx.EVT_BUTTON.typeId, -2))

    def _on_clear(self, event: wx.CommandEvent) -> None:
        """Handle clear button click."""
        self.search_text.SetValue("")
        self.status_combo.SetSelection(0)
        self.priority_combo.SetSelection(0)
        self._update_filters()
        self._update_clear_button()
        wx.PostEvent(self.GetParent(), wx.CommandEvent(wx.EVT_BUTTON.typeId, -2))

    def _update_filters(self) -> None:
        """Update filter configuration."""
        self._filters = []

        search_text = self.search_text.GetValue().strip()
        if search_text:
            self._filters.append({
                "columnId": "title",
                "type": "text",
                "value": search_text
            })

        status_idx = self.status_combo.GetSelection()
        if status_idx > 0:
            status = STATUS_OPTIONS[status_idx - 1]
            self._filters.append({
                "columnId": "status",
                "type": "select",
                "value": [status]
            })

        priority_idx = self.priority_combo.GetSelection()
        if priority_idx > 0:
            priority = PRIORITY_OPTIONS[priority_idx - 1]
            self._filters.append({
                "columnId": "priority",
                "type": "select",
                "value": [priority]
            })

    def _update_clear_button(self) -> None:
        """Update clear button state."""
        has_filters = (
            self.search_text.GetValue().strip() != "" or
            self.status_combo.GetSelection() > 0 or
            self.priority_combo.GetSelection() > 0
        )
        self.clear_btn.Enable(has_filters)

    def get_filters(self) -> list:
        """Get current filters."""
        return self._filters
