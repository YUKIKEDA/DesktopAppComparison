"""Data persistence service."""
import json
import os
import sys
from pathlib import Path
from typing import List, Optional

import wx

from .todo_item import TodoItem


class ProjectData:
    """Project data container."""
    def __init__(self, items: List[TodoItem] = None):
        self.items = items or []

    def to_dict(self) -> dict:
        """Convert to dictionary for JSON serialization."""
        return {
            "items": [item.to_dict() for item in self.items]
        }

    @classmethod
    def from_dict(cls, data: dict) -> "ProjectData":
        """Create from dictionary."""
        if not isinstance(data, dict):
            raise ValueError(f"Expected dict, got {type(data)}")
        
        items = []
        items_data = data.get("items", [])
        if not isinstance(items_data, list):
            raise ValueError(f"Expected list for 'items', got {type(items_data)}")
        
        for idx, item_data in enumerate(items_data):
            try:
                if not isinstance(item_data, dict):
                    print(f"Skipping invalid item at index {idx}: expected dict, got {type(item_data)}", file=sys.stderr)
                    continue
                items.append(TodoItem.from_dict(item_data))
            except (ValueError, KeyError, TypeError) as e:
                print(f"Failed to load item at index {idx}: {e}", file=sys.stderr)
                continue
        
        return cls(items=items)


class DataService:
    """Data persistence service."""
    def __init__(self):
        self._data_dir = self._get_data_dir()
        self._data_file = self._data_dir / "project.json"
        self._window_file = self._data_dir / "window.json"
        self._theme_file = self._data_dir / "theme.json"
        self._ensure_data_dir()

    @property
    def data_dir(self) -> Path:
        """Public data directory path."""
        return self._data_dir

    @property
    def window_settings_path(self) -> Path:
        """Path to window.json."""
        return self._window_file

    def _get_data_dir(self) -> Path:
        """Get data directory path."""
        # Use wxPython's standard data directory
        if wx.GetApp():
            app_name = wx.GetApp().GetAppName()
            if not app_name:
                app_name = "TodoApp.wxWidgets"
        else:
            app_name = "TodoApp.wxWidgets"
        
        # Use user data directory
        if os.name == "nt":  # Windows
            base_dir = Path(os.environ.get("APPDATA", Path.home()))
        else:  # Unix-like
            base_dir = Path.home() / ".local" / "share"
        
        return base_dir / app_name / "data"

    def _ensure_data_dir(self) -> None:
        """Ensure data directory exists."""
        self._data_dir.mkdir(parents=True, exist_ok=True)

    def load_data(self, show_errors: bool = False) -> ProjectData:
        """Load data from file. Avoid UI dialogs on worker threads."""
        try:
            print(f"DataService.load_data: Loading from {self._data_file}")
            if self._data_file.exists():
                print(f"DataService.load_data: File exists, reading...")
                with open(self._data_file, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    print(f"DataService.load_data: JSON loaded, type: {type(data)}")
                    if not isinstance(data, dict):
                        print(f"Invalid data format: expected dict, got {type(data)}", file=sys.stderr)
                        return ProjectData()
                    print(f"DataService.load_data: Creating ProjectData from dict...")
                    project_data = ProjectData.from_dict(data)
                    print(f"DataService.load_data: ProjectData created with {len(project_data.items)} items")
                    return project_data
            else:
                print(f"DataService.load_data: File does not exist, returning empty ProjectData")
        except json.JSONDecodeError as e:
            error_msg = f"Failed to parse JSON: {e}"
            print(f"ERROR in load_data (JSON): {error_msg}", file=sys.stderr)
            if show_errors:
                try:
                    wx.LogError(error_msg)
                    wx.MessageBox(
                        f"データファイルの形式が正しくありません: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
                except Exception:
                    pass
        except Exception as e:
            import traceback
            error_msg = f"Failed to load data: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in load_data: {error_msg}", file=sys.stderr)
            if show_errors:
                try:
                    wx.LogError(error_msg)
                    wx.MessageBox(
                        f"データの読み込みに失敗しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
                except Exception:
                    pass

        return ProjectData()

    def save_data(self, data: ProjectData, show_errors: bool = False) -> None:
        """Save data to file. Avoid UI dialogs on worker threads."""
        try:
            self._ensure_data_dir()
            # Create temporary file first, then rename (atomic write)
            temp_file = self._data_file.with_suffix(".tmp")
            with open(temp_file, "w", encoding="utf-8") as f:
                json.dump(data.to_dict(), f, ensure_ascii=False, indent=2)
            # Atomic rename
            temp_file.replace(self._data_file)
        except Exception as e:
            print(f"Failed to save data: {e}", file=sys.stderr)
            if show_errors:
                try:
                    wx.LogError(f"Failed to save data: {e}")
                    wx.MessageBox(
                        f"データの保存に失敗しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR
                    )
                except Exception:
                    pass
            raise

    def export_data(self, data: ProjectData, parent: Optional[wx.Window] = None) -> bool:
        """Export data to a file chosen by user (sync helper)."""
        path = self.choose_export_path(parent)
        if not path:
            return False
        return self.write_json_file(path, data)

    def choose_export_path(self, parent: Optional[wx.Window] = None) -> Optional[str]:
        """Show save dialog on the UI thread; returns chosen path or None."""
        with wx.FileDialog(
            parent,
            "データをエクスポート",
            wildcard="JSON files (*.json)|*.json|All files (*.*)|*.*",
            style=wx.FD_SAVE | wx.FD_OVERWRITE_PROMPT
        ) as fileDialog:
            if fileDialog.ShowModal() == wx.ID_CANCEL:
                return None
            return fileDialog.GetPath()

    def choose_import_path(self, parent: Optional[wx.Window] = None) -> Optional[str]:
        """Show open dialog on the UI thread; returns chosen path or None."""
        with wx.FileDialog(
            parent,
            "データをインポート",
            wildcard="JSON files (*.json)|*.json|All files (*.*)|*.*",
            style=wx.FD_OPEN | wx.FD_FILE_MUST_EXIST
        ) as fileDialog:
            if fileDialog.ShowModal() == wx.ID_CANCEL:
                return None
            return fileDialog.GetPath()

    def write_json_file(self, path: str, data: ProjectData) -> bool:
        """Write project data JSON to path (safe for worker threads)."""
        try:
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data.to_dict(), f, ensure_ascii=False, indent=2)
            return True
        except Exception as e:
            print(f"Failed to write JSON: {e}", file=sys.stderr)
            raise

    def import_data(self, parent: Optional[wx.Window] = None) -> Optional[ProjectData]:
        """Import data from a file chosen by user."""
        path = self.choose_import_path(parent)
        if not path:
            return None
        return self.import_from_path(path, parent=parent)

    def import_from_path(
        self,
        path: str,
        parent: Optional[wx.Window] = None,
        show_errors: bool = True,
    ) -> Optional[ProjectData]:
        """Import data from a given file path (shared parse with import_data)."""
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
                if not isinstance(data, dict):
                    raise ValueError(f"Expected dict, got {type(data)}")
                return ProjectData.from_dict(data)
        except Exception as e:
            print(f"Failed to import data from path: {e}", file=sys.stderr)
            if show_errors:
                # Only show UI errors when called from the UI thread.
                try:
                    wx.LogError(f"Failed to import data from path: {e}")
                    wx.MessageBox(
                        f"インポートに失敗しました: {e}",
                        "エラー",
                        wx.OK | wx.ICON_ERROR,
                        parent
                    )
                except Exception:
                    pass
            return None

    def load_window_geometry(self) -> Optional[dict]:
        """Load window position/size from window.json."""
        try:
            self._ensure_data_dir()
            if not self._window_file.exists():
                return None
            with open(self._window_file, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not isinstance(data, dict):
                return None
            width = int(data.get("width", 0))
            height = int(data.get("height", 0))
            if width < 100 or height < 100:
                return None
            return {
                "x": int(data.get("x", 100)),
                "y": int(data.get("y", 100)),
                "width": width,
                "height": height,
            }
        except Exception as e:
            wx.LogError(f"Failed to load window geometry: {e}")
            return None

    def save_window_geometry(self, x: int, y: int, width: int, height: int) -> None:
        """Save window position/size to window.json."""
        try:
            self._ensure_data_dir()
            payload = {
                "x": int(x),
                "y": int(y),
                "width": int(width),
                "height": int(height),
            }
            temp_file = self._window_file.with_suffix(".tmp")
            with open(temp_file, "w", encoding="utf-8") as f:
                json.dump(payload, f, ensure_ascii=False, indent=2)
            temp_file.replace(self._window_file)
        except Exception as e:
            wx.LogError(f"Failed to save window geometry: {e}")

    def load_theme(self) -> str:
        """Load theme preference from theme.json. Defaults to light."""
        try:
            self._ensure_data_dir()
            if not self._theme_file.exists():
                return "light"
            with open(self._theme_file, "r", encoding="utf-8") as f:
                data = json.load(f)
            if not isinstance(data, dict):
                return "light"
            theme = data.get("theme")
            if theme in ("light", "dark"):
                return theme
            return "light"
        except Exception as e:
            wx.LogError(f"Failed to load theme: {e}")
            return "light"

    def save_theme(self, theme: str) -> None:
        """Persist theme preference next to project.json."""
        try:
            self._ensure_data_dir()
            normalized = "dark" if theme == "dark" else "light"
            payload = {"theme": normalized}
            temp_file = self._theme_file.with_suffix(".tmp")
            with open(temp_file, "w", encoding="utf-8") as f:
                json.dump(payload, f, ensure_ascii=False, indent=2)
            temp_file.replace(self._theme_file)
        except Exception as e:
            wx.LogError(f"Failed to save theme: {e}")

    def open_data_folder(self) -> None:
        """Open data folder in file manager."""
        try:
            self._ensure_data_dir()
            if os.name == "nt":  # Windows
                os.startfile(str(self._data_dir))
            elif os.name == "posix":  # Unix-like
                import subprocess
                subprocess.run(["xdg-open", str(self._data_dir)])
        except Exception as e:
            wx.LogError(f"Failed to open data folder: {e}")

