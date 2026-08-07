"""Main application entry point."""
import wx
import sys
import os
import traceback

# Add project root to path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# Enable logging to console
wx.Log.EnableLogging(True)


def show_error(title: str, message: str):
    """Show error message in both console and dialog."""
    print(f"ERROR: {title}", file=sys.stderr)
    print(message, file=sys.stderr)
    try:
        app = wx.App.Get()
        if app:
            wx.MessageBox(message, title, wx.OK | wx.ICON_ERROR)
    except Exception:
        pass


try:
    from views.main_frame import MainFrame
except Exception as e:
    show_error("インポートエラー", f"MainFrameのインポートに失敗しました:\n{str(e)}\n\n{traceback.format_exc()}")
    sys.exit(1)


def _json_paths_from_argv(argv):
    """Return existing .json paths from argv (file association / open-with)."""
    paths = []
    for arg in argv[1:]:
        if arg.lower().endswith(".json") and os.path.isfile(arg):
            paths.append(os.path.abspath(arg))
    return paths


class TodoApp(wx.App):
    """Main application class."""
    def OnInit(self):
        """Initialize application."""
        print("Initializing application...")
        try:
            print("Setting app name...")
            self.SetAppName("TodoApp.wxWidgets")

            json_paths = _json_paths_from_argv(sys.argv)
            print("Creating main frame...")
            frame = MainFrame(startup_json_paths=json_paths)
            print("Main frame created successfully")

            print("Showing frame...")
            frame.Show()
            print("Frame shown successfully")

            return True
        except Exception as e:
            error_msg = f"アプリケーションの起動に失敗しました:\n\n{str(e)}\n\n{traceback.format_exc()}"
            print(error_msg, file=sys.stderr)
            show_error("致命的なエラー", error_msg)
            return False


def main():
    """Main entry point."""
    print("Starting application...")
    try:
        app = TodoApp()
        print("App created, starting main loop...")
        app.MainLoop()
    except Exception as e:
        error_msg = f"アプリケーションの実行中にエラーが発生しました:\n\n{str(e)}\n\n{traceback.format_exc()}"
        print(error_msg, file=sys.stderr)
        show_error("実行時エラー", error_msg)
        sys.exit(1)


if __name__ == "__main__":
    main()
