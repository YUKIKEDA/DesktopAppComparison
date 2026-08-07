"""Detail frame for viewing/editing a todo item in a separate window."""
import wx
import wx.adv
from datetime import datetime
from typing import Optional, Callable

from models.todo_item import TodoItem
from utils.constants import STATUS_OPTIONS, PRIORITY_OPTIONS
from utils.date_utils import parse_iso_datetime


class TodoDetailFrame(wx.Frame):
    """Non-modal frame for todo item detail/edit."""

    def __init__(
        self,
        parent,
        item: TodoItem,
        on_save: Callable[[int, dict], None],
        on_close: Optional[Callable[[], None]] = None,
    ):
        super().__init__(
            parent,
            title=item.title or "アイテム詳細",
            size=(560, 640),
            style=wx.DEFAULT_FRAME_STYLE
        )
        self.item_id = item.id
        self._on_save = on_save
        self._on_close_callback = on_close

        self._create_ui()
        self._bind_events()
        self.load_item(item)
        self.CentreOnParent()

    def _create_ui(self) -> None:
        panel = wx.Panel(self)
        sizer = wx.BoxSizer(wx.VERTICAL)

        title_label = wx.StaticText(panel, label="タイトル *")
        sizer.Add(title_label, flag=wx.ALL, border=5)

        self.title_text = wx.TextCtrl(panel, size=(400, -1))
        sizer.Add(self.title_text, flag=wx.ALL | wx.EXPAND, border=5)

        desc_label = wx.StaticText(panel, label="説明")
        sizer.Add(desc_label, flag=wx.ALL, border=5)

        self.desc_text = wx.TextCtrl(panel, style=wx.TE_MULTILINE, size=(400, 100))
        sizer.Add(self.desc_text, flag=wx.ALL | wx.EXPAND, border=5)

        status_priority_sizer = wx.BoxSizer(wx.HORIZONTAL)

        status_label = wx.StaticText(panel, label="ステータス")
        status_priority_sizer.Add(status_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.status_combo = wx.ComboBox(panel, choices=STATUS_OPTIONS, style=wx.CB_READONLY)
        status_priority_sizer.Add(self.status_combo, proportion=1, flag=wx.ALL, border=5)

        priority_label = wx.StaticText(panel, label="優先度")
        status_priority_sizer.Add(priority_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.priority_combo = wx.ComboBox(panel, choices=PRIORITY_OPTIONS, style=wx.CB_READONLY)
        status_priority_sizer.Add(self.priority_combo, proportion=1, flag=wx.ALL, border=5)

        sizer.Add(status_priority_sizer, flag=wx.ALL | wx.EXPAND, border=5)

        due_date_label = wx.StaticText(panel, label="期限")
        sizer.Add(due_date_label, flag=wx.ALL, border=5)

        due_date_sizer = wx.BoxSizer(wx.HORIZONTAL)
        self.due_date_picker = wx.adv.DatePickerCtrl(
            panel, style=wx.adv.DP_DROPDOWN | wx.adv.DP_ALLOWNONE
        )
        self.due_date_picker.SetValue(wx.DateTime())
        due_date_sizer.Add(self.due_date_picker, flag=wx.ALL, border=5)

        self.due_time_picker = wx.adv.TimePickerCtrl(panel, style=wx.adv.TP_DEFAULT)
        due_date_sizer.Add(self.due_time_picker, flag=wx.ALL, border=5)
        sizer.Add(due_date_sizer, flag=wx.ALL, border=5)

        button_sizer = wx.BoxSizer(wx.HORIZONTAL)
        button_sizer.AddStretchSpacer()

        self.close_btn = wx.Button(panel, label="閉じる")
        button_sizer.Add(self.close_btn, flag=wx.ALL, border=5)

        self.save_btn = wx.Button(panel, label="更新")
        button_sizer.Add(self.save_btn, flag=wx.ALL, border=5)

        sizer.Add(button_sizer, flag=wx.ALL | wx.EXPAND, border=5)

        panel.SetSizer(sizer)
        main_sizer = wx.BoxSizer(wx.VERTICAL)
        main_sizer.Add(panel, proportion=1, flag=wx.EXPAND | wx.ALL, border=10)
        self.SetSizer(main_sizer)

    def _bind_events(self) -> None:
        self.save_btn.Bind(wx.EVT_BUTTON, self._on_save_click)
        self.close_btn.Bind(wx.EVT_BUTTON, self._on_close_click)
        self.Bind(wx.EVT_CLOSE, self._on_close)

    def load_item(self, item: TodoItem) -> None:
        """Refresh form fields from item."""
        self.item_id = item.id
        self.SetTitle(item.title or "アイテム詳細")
        self.title_text.SetValue(item.title)
        self.desc_text.SetValue(item.description)

        try:
            self.status_combo.SetSelection(STATUS_OPTIONS.index(item.status))
        except ValueError:
            self.status_combo.SetSelection(0)

        try:
            self.priority_combo.SetSelection(PRIORITY_OPTIONS.index(item.priority))
        except ValueError:
            self.priority_combo.SetSelection(1)

        if item.due_date:
            dt = parse_iso_datetime(item.due_date)
            if dt:
                try:
                    wx_dt = wx.DateTime.FromDMY(
                        dt.day, dt.month - 1, dt.year,
                        dt.hour, dt.minute, dt.second
                    )
                    self.due_date_picker.SetValue(wx_dt)
                    self.due_time_picker.SetValue(wx_dt)
                    return
                except (ValueError, AttributeError):
                    pass
        self.due_date_picker.SetValue(wx.DateTime())

    def _collect_result(self) -> Optional[dict]:
        title = self.title_text.GetValue().strip()
        if not title:
            wx.MessageBox("タイトルは必須です", "エラー", wx.OK | wx.ICON_ERROR, self)
            self.title_text.SetFocus()
            return None

        if len(title) > 200:
            wx.MessageBox("タイトルは200文字以内です", "エラー", wx.OK | wx.ICON_ERROR, self)
            self.title_text.SetFocus()
            return None

        description = self.desc_text.GetValue()
        if len(description) > 500:
            wx.MessageBox("説明は500文字以内です", "エラー", wx.OK | wx.ICON_ERROR, self)
            self.desc_text.SetFocus()
            return None

        status = STATUS_OPTIONS[self.status_combo.GetSelection()]
        priority = PRIORITY_OPTIONS[self.priority_combo.GetSelection()]

        due_date = None
        wx_dt = self.due_date_picker.GetValue()
        if wx_dt.IsValid():
            wx_time = self.due_time_picker.GetValue()
            if wx_time.IsValid():
                wx_dt.SetHour(wx_time.GetHour())
                wx_dt.SetMinute(wx_time.GetMinute())
                wx_dt.SetSecond(wx_time.GetSecond())

            dt = datetime(
                wx_dt.GetYear(),
                wx_dt.GetMonth() + 1,
                wx_dt.GetDay(),
                wx_dt.GetHour(),
                wx_dt.GetMinute(),
                wx_dt.GetSecond()
            )
            due_date = dt.isoformat()

        return {
            "title": title,
            "description": description,
            "status": status,
            "priority": priority,
            "dueDate": due_date,
        }

    def _on_save_click(self, event: wx.CommandEvent) -> None:
        result = self._collect_result()
        if result is None:
            return
        self._on_save(self.item_id, result)
        self.Close()

    def _on_close_click(self, event: wx.CommandEvent) -> None:
        self.Close()

    def _on_close(self, event: wx.CloseEvent) -> None:
        if self._on_close_callback:
            self._on_close_callback()
        event.Skip()
