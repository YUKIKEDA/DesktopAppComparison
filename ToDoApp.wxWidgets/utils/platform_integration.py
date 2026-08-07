"""System tray (TaskBarIcon) helper."""
import wx
import wx.adv


class TodoTaskBarIcon(wx.adv.TaskBarIcon):
    """Tray icon with 表示 / 終了 menu."""

    def __init__(self, frame: wx.Frame):
        super().__init__()
        self.frame = frame
        self._quitting = False

        icon = wx.ArtProvider.GetIcon(wx.ART_INFORMATION, wx.ART_MENU, (16, 16))
        if not icon.IsOk():
            # Fallback empty icon
            bmp = wx.Bitmap(16, 16)
            icon = wx.Icon()
            icon.CopyFromBitmap(bmp)
        self.SetIcon(icon, "Todo App")

        self.Bind(wx.adv.EVT_TASKBAR_LEFT_DCLICK, self.on_show)
        self.Bind(wx.adv.EVT_TASKBAR_LEFT_UP, self.on_show)

    def CreatePopupMenu(self):
        menu = wx.Menu()
        show_item = menu.Append(wx.ID_ANY, "表示")
        quit_item = menu.Append(wx.ID_EXIT, "終了")
        self.Bind(wx.EVT_MENU, self.on_show, show_item)
        self.Bind(wx.EVT_MENU, self.on_quit, quit_item)
        return menu

    def on_show(self, event=None):
        if self.frame:
            self.frame.Show()
            self.frame.Raise()
            self.frame.Iconize(False)

    def on_quit(self, event=None):
        self._quitting = True
        if self.frame:
            # Allow real close
            self.frame._allow_close = True
            self.frame.Close(force=True)

    @property
    def is_quitting(self) -> bool:
        return self._quitting


def show_notification(title: str, message: str, parent=None) -> None:
    """Show OS notification via wx.adv.NotificationMessage."""
    try:
        notify = wx.adv.NotificationMessage(title, message, parent)
        flags = wx.ICON_INFORMATION
        if hasattr(wx.adv.NotificationMessage, "SetFlags"):
            notify.SetFlags(flags)
        notify.Show()
    except Exception as e:
        print(f"Notification failed: {e}")


def copy_text_to_clipboard(text: str) -> bool:
    """Copy text to the system clipboard."""
    if not wx.TheClipboard.Open():
        return False
    try:
        wx.TheClipboard.SetData(wx.TextDataObject(text))
        return True
    finally:
        wx.TheClipboard.Close()
