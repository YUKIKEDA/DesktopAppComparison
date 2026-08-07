"""Todo controller - business logic."""
import sys
from typing import List, Optional, Set, Callable
from datetime import datetime

from models.todo_item import TodoItem
from models.data_service import DataService, ProjectData
from utils.constants import STATUS_OPTIONS, PRIORITY_OPTIONS


class TodoController:
    """Controller for todo items management."""
    def __init__(self):
        self._items: List[TodoItem] = []
        self._selected_ids: Set[int] = set()
        self._data_service = DataService()
        self._callbacks: List[Callable] = []

    def add_callback(self, callback: Callable) -> None:
        """Add callback for data changes."""
        self._callbacks.append(callback)

    def _notify(self) -> None:
        """Notify all callbacks of data changes."""
        for callback in self._callbacks:
            try:
                callback()
            except Exception as e:
                import traceback
                error_msg = f"コールバック実行エラー: {e}\n\n{traceback.format_exc()}"
                print(f"ERROR in _notify: {error_msg}", file=sys.stderr)
                # Continue with other callbacks even if one fails

    def load_data(self) -> None:
        """Load data from storage."""
        project_data = self._data_service.load_data()
        self._items = project_data.items
        self._notify()

    def save_data(self) -> None:
        """Save data to storage."""
        project_data = ProjectData(items=self._items)
        self._data_service.save_data(project_data)

    def get_items(self) -> List[TodoItem]:
        """Get all items."""
        return self._items

    def get_filtered_items(self, filters: List[dict]) -> List[TodoItem]:
        """Get filtered items."""
        result = list(self._items)

        for filter_config in filters:
            column_id = filter_config.get("columnId")
            filter_type = filter_config.get("type")
            value = filter_config.get("value")

            if filter_type == "text" and isinstance(value, str):
                search_term = value.lower()
                if column_id == "title":
                    result = [
                        item for item in result
                        if search_term in item.title.lower() or
                        search_term in item.description.lower()
                    ]
                elif column_id == "description":
                    result = [
                        item for item in result
                        if search_term in item.description.lower()
                    ]
            elif filter_type == "select" and isinstance(value, list):
                if column_id == "status":
                    result = [
                        item for item in result
                        if item.status in value
                    ]
                elif column_id == "priority":
                    result = [
                        item for item in result
                        if item.priority in value
                    ]

        return result

    def get_sorted_items(self, items: List[TodoItem], sorts: List[dict]) -> List[TodoItem]:
        """Get sorted items."""
        result = list(items)

        if not sorts:
            # Default sort by created_at descending
            result.sort(key=lambda x: x.created_at, reverse=True)
            return result

        # Apply sorts in reverse order (last sort is primary)
        for sort_config in reversed(sorts):
            column_id = sort_config.get("columnId")
            direction = sort_config.get("direction")

            if direction == "asc":
                reverse = False
            elif direction == "desc":
                reverse = True
            else:
                continue

            if column_id == "id":
                result.sort(key=lambda x: x.id, reverse=reverse)
            elif column_id == "title":
                result.sort(key=lambda x: x.title.lower(), reverse=reverse)
            elif column_id == "description":
                result.sort(key=lambda x: x.description.lower(), reverse=reverse)
            elif column_id == "status":
                result.sort(key=lambda x: STATUS_OPTIONS.index(x.status) if x.status in STATUS_OPTIONS else 999, reverse=reverse)
            elif column_id == "priority":
                result.sort(key=lambda x: PRIORITY_OPTIONS.index(x.priority) if x.priority in PRIORITY_OPTIONS else 999, reverse=reverse)
            elif column_id == "dueDate":
                result.sort(key=lambda x: x.due_date or "", reverse=reverse)
            elif column_id == "createdAt":
                result.sort(key=lambda x: x.created_at, reverse=reverse)
            elif column_id == "updatedAt":
                result.sort(key=lambda x: x.updated_at, reverse=reverse)

        return result

    def add_item(self, item_data: dict) -> TodoItem:
        """Add new item."""
        # Validate required fields
        if not isinstance(item_data, dict):
            raise ValueError(f"Expected dict, got {type(item_data)}")
        
        required_fields = ["title", "status", "priority"]
        missing_fields = [field for field in required_fields if field not in item_data]
        if missing_fields:
            raise ValueError(f"Missing required fields: {missing_fields}")
        
        # Calculate next ID
        max_id = max([item.id for item in self._items], default=0) if self._items else 0
        now = datetime.now().isoformat()

        new_item = TodoItem(
            id=max_id + 1,
            title=str(item_data["title"]).strip(),
            description=str(item_data.get("description", "")).strip(),
            status=str(item_data["status"]),
            priority=str(item_data["priority"]),
            due_date=item_data.get("dueDate") if item_data.get("dueDate") else None,
            created_at=now,
            updated_at=now,
            is_completed=bool(item_data.get("isCompleted", False)),
        )

        self._items.append(new_item)
        self._notify()
        return new_item

    def update_item(self, item_id: int, updates: dict) -> Optional[TodoItem]:
        """Update item."""
        for item in self._items:
            if item.id == item_id:
                item.update(**updates)
                self._notify()
                return item
        return None

    def delete_items(self, item_ids: List[int]) -> None:
        """Delete items."""
        self._items = [item for item in self._items if item.id not in item_ids]
        self._selected_ids -= set(item_ids)
        self._notify()

    def get_selected_ids(self) -> Set[int]:
        """Get selected item IDs."""
        return self._selected_ids

    def toggle_selection(self, item_id: int) -> None:
        """Toggle item selection."""
        if item_id in self._selected_ids:
            self._selected_ids.remove(item_id)
        else:
            self._selected_ids.add(item_id)
        self._notify()

    def select_all(self, item_ids: List[int]) -> None:
        """Select all items."""
        self._selected_ids.update(item_ids)
        self._notify()

    def deselect_all(self) -> None:
        """Deselect all items."""
        self._selected_ids.clear()
        self._notify()

    def export_data(self, parent=None) -> bool:
        """Export data."""
        project_data = ProjectData(items=self._items)
        return self._data_service.export_data(project_data, parent)

    def import_data(self, parent=None) -> bool:
        """Import data."""
        project_data = self._data_service.import_data(parent)
        if project_data:
            self._apply_imported_data(project_data)
            return True
        return False

    def import_from_path(self, path: str, parent=None) -> bool:
        """Import data from a file path."""
        project_data = self._data_service.import_from_path(path, parent=parent)
        if project_data:
            self._apply_imported_data(project_data)
            return True
        return False

    def _apply_imported_data(self, project_data: ProjectData) -> None:
        """Apply imported project data."""
        self._items = project_data.items
        self._selected_ids.clear()
        self._notify()

    def get_item_by_id(self, item_id: int) -> Optional[TodoItem]:
        """Get item by id."""
        for item in self._items:
            if item.id == item_id:
                return item
        return None

    def load_window_geometry(self) -> Optional[dict]:
        """Load saved window geometry."""
        return self._data_service.load_window_geometry()

    def save_window_geometry(self, x: int, y: int, width: int, height: int) -> None:
        """Save window geometry."""
        self._data_service.save_window_geometry(x, y, width, height)

    def open_data_folder(self) -> None:
        """Open data folder."""
        self._data_service.open_data_folder()

