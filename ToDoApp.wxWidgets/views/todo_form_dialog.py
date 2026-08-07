"""Todo form dialog."""
import wx
import wx.adv
from datetime import datetime
from typing import Optional

from models.todo_item import TodoItem
from utils.constants import STATUS_OPTIONS, PRIORITY_OPTIONS
from utils.date_utils import parse_iso_datetime
from utils.theme import get_theme, style_brand_button


class TodoFormDialog(wx.Dialog):
    """Dialog for adding/editing todo items."""
    def __init__(self, parent, item: Optional[TodoItem] = None, theme_name: str = "light"):
        title = "アイテムを編集" if item else "新しいアイテムを追加"
        super().__init__(parent, title=title, size=(550, 500))

        self.item = item
        self.result = None
        self._theme_name = theme_name
        self._fade_timer = None
        self._fade_alpha = 0

        self._create_ui()
        self._bind_events()
        self._apply_theme()

        if item:
            self._load_item(item)
        
        # Fit dialog to content and set minimum size
        self.Fit()
        self.SetMinSize((500, 450))

    def ShowModal(self):
        """Show modal with best-effort fade-in (~175ms) when transparency is available."""
        try:
            if self.CanSetTransparent():
                self._fade_alpha = 0
                self.SetTransparent(0)
                self._fade_timer = wx.Timer(self)
                self.Bind(wx.EVT_TIMER, self._on_fade_tick, self._fade_timer)
                self._fade_timer.Start(20)
        except Exception:
            pass
        return super().ShowModal()

    def _on_fade_tick(self, event: wx.TimerEvent) -> None:
        """Step fade-in opacity."""
        self._fade_alpha = min(255, self._fade_alpha + 30)
        try:
            self.SetTransparent(self._fade_alpha)
        except Exception:
            self._fade_alpha = 255
        if self._fade_alpha >= 255 and self._fade_timer:
            self._fade_timer.Stop()
            self._fade_timer = None

    def _create_ui(self) -> None:
        """Create form UI."""
        panel = wx.Panel(self)
        sizer = wx.BoxSizer(wx.VERTICAL)

        # Title
        title_label = wx.StaticText(panel, label="タイトル *")
        sizer.Add(title_label, flag=wx.ALL, border=5)

        self.title_text = wx.TextCtrl(panel, size=(400, -1))
        sizer.Add(self.title_text, flag=wx.ALL | wx.EXPAND, border=5)

        # Description
        desc_label = wx.StaticText(panel, label="説明")
        sizer.Add(desc_label, flag=wx.ALL, border=5)

        self.desc_text = wx.TextCtrl(panel, style=wx.TE_MULTILINE, size=(400, 100))
        sizer.Add(self.desc_text, flag=wx.ALL | wx.EXPAND, border=5)

        # Status and Priority (side by side)
        status_priority_sizer = wx.BoxSizer(wx.HORIZONTAL)

        # Status
        status_label = wx.StaticText(panel, label="ステータス")
        status_priority_sizer.Add(status_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.status_combo = wx.ComboBox(
            panel,
            choices=STATUS_OPTIONS,
            style=wx.CB_READONLY
        )
        self.status_combo.SetSelection(0)
        status_priority_sizer.Add(self.status_combo, proportion=1, flag=wx.ALL, border=5)

        # Priority
        priority_label = wx.StaticText(panel, label="優先度")
        status_priority_sizer.Add(priority_label, flag=wx.ALL | wx.ALIGN_CENTER_VERTICAL, border=5)

        self.priority_combo = wx.ComboBox(
            panel,
            choices=PRIORITY_OPTIONS,
            style=wx.CB_READONLY
        )
        self.priority_combo.SetSelection(1)  # Default to "中"
        status_priority_sizer.Add(self.priority_combo, proportion=1, flag=wx.ALL, border=5)

        sizer.Add(status_priority_sizer, flag=wx.ALL | wx.EXPAND, border=5)

        # Due date
        due_date_label = wx.StaticText(panel, label="期限")
        sizer.Add(due_date_label, flag=wx.ALL, border=5)

        due_date_sizer = wx.BoxSizer(wx.HORIZONTAL)
        self.due_date_picker = wx.adv.DatePickerCtrl(panel, style=wx.adv.DP_DROPDOWN | wx.adv.DP_ALLOWNONE)
        # Initialize with invalid date (empty)
        invalid_dt = wx.DateTime()
        self.due_date_picker.SetValue(invalid_dt)
        due_date_sizer.Add(self.due_date_picker, flag=wx.ALL, border=5)

        self.due_time_picker = wx.adv.TimePickerCtrl(panel, style=wx.adv.TP_DEFAULT)
        due_date_sizer.Add(self.due_time_picker, flag=wx.ALL, border=5)

        sizer.Add(due_date_sizer, flag=wx.ALL, border=5)

        # Buttons
        button_sizer = wx.BoxSizer(wx.HORIZONTAL)
        button_sizer.AddStretchSpacer()

        self.cancel_btn = wx.Button(panel, label="キャンセル", id=wx.ID_CANCEL)
        button_sizer.Add(self.cancel_btn, flag=wx.ALL, border=5)

        self.save_btn = wx.Button(panel, label="追加" if not self.item else "更新", id=wx.ID_OK)
        button_sizer.Add(self.save_btn, flag=wx.ALL, border=5)

        sizer.Add(button_sizer, flag=wx.ALL | wx.EXPAND, border=5)

        panel.SetSizer(sizer)
        main_sizer = wx.BoxSizer(wx.VERTICAL)
        main_sizer.Add(panel, proportion=1, flag=wx.EXPAND | wx.ALL, border=10)
        self.SetSizer(main_sizer)
        self._panel = panel
        
        # Ensure buttons are visible by adding some bottom padding
        main_sizer.AddSpacer(10)

        # Set focus to title
        self.title_text.SetFocus()

    def _apply_theme(self) -> None:
        """Apply brand colours for light/dark theme."""
        colors = get_theme(self._theme_name)
        self.SetBackgroundColour(colors.bg)
        if hasattr(self, "_panel") and self._panel:
            self._panel.SetBackgroundColour(colors.surface)
            self._panel.SetForegroundColour(colors.text)
            for child in self._panel.GetChildren():
                if isinstance(child, wx.StaticText):
                    child.SetForegroundColour(colors.text)
                    child.SetBackgroundColour(colors.surface)
                elif isinstance(child, (wx.TextCtrl, wx.ComboBox)):
                    child.SetBackgroundColour(colors.surface_alt)
                    child.SetForegroundColour(colors.text)
        style_brand_button(self.save_btn, colors.brand_blue, colors.on_brand)
        self.cancel_btn.SetBackgroundColour(colors.surface_alt)
        self.cancel_btn.SetForegroundColour(colors.text)
        self.Refresh()

    def _bind_events(self) -> None:
        """Bind events."""
        self.Bind(wx.EVT_BUTTON, self._on_save, self.save_btn)
        self.Bind(wx.EVT_BUTTON, self._on_cancel, self.cancel_btn)

    def _load_item(self, item: TodoItem) -> None:
        """Load item data into form."""
        self.title_text.SetValue(item.title)
        self.desc_text.SetValue(item.description)

        try:
            status_idx = STATUS_OPTIONS.index(item.status)
            self.status_combo.SetSelection(status_idx)
        except ValueError:
            pass

        try:
            priority_idx = PRIORITY_OPTIONS.index(item.priority)
            self.priority_combo.SetSelection(priority_idx)
        except ValueError:
            pass

        # Load due date
        if item.due_date:
            dt = parse_iso_datetime(item.due_date)
            if dt:
                try:
                    # Convert to wx.DateTime
                    wx_dt = wx.DateTime.FromDMY(
                        dt.day, dt.month - 1, dt.year,  # month is 0-based in wx.DateTime
                        dt.hour, dt.minute, dt.second
                    )
                    self.due_date_picker.SetValue(wx_dt)
                    self.due_time_picker.SetValue(wx_dt)
                except (ValueError, AttributeError):
                    # Set invalid date (empty)
                    invalid_dt = wx.DateTime()
                    self.due_date_picker.SetValue(invalid_dt)
            else:
                # Set invalid date (empty)
                invalid_dt = wx.DateTime()
                self.due_date_picker.SetValue(invalid_dt)

    def _on_save(self, event: wx.CommandEvent) -> None:
        """Handle save button click."""
        title = self.title_text.GetValue().strip()
        if not title:
            wx.MessageBox("タイトルは必須です", "エラー", wx.OK | wx.ICON_ERROR)
            self.title_text.SetFocus()
            return

        if len(title) > 200:
            wx.MessageBox("タイトルは200文字以内です", "エラー", wx.OK | wx.ICON_ERROR)
            self.title_text.SetFocus()
            return

        description = self.desc_text.GetValue()
        if len(description) > 500:
            wx.MessageBox("説明は500文字以内です", "エラー", wx.OK | wx.ICON_ERROR)
            self.desc_text.SetFocus()
            return

        status = STATUS_OPTIONS[self.status_combo.GetSelection()]
        priority = PRIORITY_OPTIONS[self.priority_combo.GetSelection()]

        # Get due date
        due_date = None
        wx_dt = self.due_date_picker.GetValue()
        if wx_dt.IsValid():
            # Get time from time picker
            wx_time = self.due_time_picker.GetValue()
            if wx_time.IsValid():
                # Combine date and time
                wx_dt.SetHour(wx_time.GetHour())
                wx_dt.SetMinute(wx_time.GetMinute())
                wx_dt.SetSecond(wx_time.GetSecond())
            
            # Convert wx.DateTime to Python datetime
            dt = datetime(
                wx_dt.GetYear(),
                wx_dt.GetMonth() + 1,  # wx.DateTime month is 0-based
                wx_dt.GetDay(),
                wx_dt.GetHour(),
                wx_dt.GetMinute(),
                wx_dt.GetSecond()
            )
            due_date = dt.isoformat()

        self.result = {
            "title": title,
            "description": description,
            "status": status,
            "priority": priority,
            "dueDate": due_date,
        }

        if self._fade_timer:
            self._fade_timer.Stop()
            self._fade_timer = None
        self.EndModal(wx.ID_OK)

    def _on_cancel(self, event: wx.CommandEvent) -> None:
        """Handle cancel button click."""
        if self._fade_timer:
            self._fade_timer.Stop()
            self._fade_timer = None
        self.EndModal(wx.ID_CANCEL)

    def get_result(self) -> Optional[dict]:
        """Get form result."""
        return self.result

