"""Todo table component with virtual list."""
import wx
import wx.lib.mixins.listctrl as listmix
import sys
from typing import List, Optional, Callable, Set

from models.todo_item import TodoItem
from controllers.todo_controller import TodoController
from utils.constants import (
    COL_ID, COL_TITLE, COL_DESCRIPTION, COL_STATUS, COL_PRIORITY,
    COL_DUE_DATE, COL_CREATED_AT, COL_UPDATED_AT
)
from utils.date_utils import parse_iso_datetime, format_datetime_for_display


class TodoTable(wx.ListCtrl, listmix.ListCtrlAutoWidthMixin):
    """Virtual list control for todo items."""
    def __init__(self, parent, controller: TodoController):
        style = wx.LC_REPORT | wx.LC_VIRTUAL | wx.LC_HRULES | wx.LC_VRULES
        super().__init__(parent, style=style)

        listmix.ListCtrlAutoWidthMixin.__init__(self)

        self.controller = controller
        self._items: List[TodoItem] = []
        self._selected_ids: Set[int] = set()
        self._sorts: List[dict] = []
        self._on_edit: Optional[Callable] = None

        # Initialize debug counter
        self._debug_call_count = 0

        self._setup_columns()
        self._bind_events()

    def _setup_columns(self) -> None:
        """Setup table columns."""
        self.InsertColumn(0, "✓", width=40)  # Checkbox column
        self.InsertColumn(1, "ID", width=80)
        self.InsertColumn(2, "タイトル", width=200)
        self.InsertColumn(3, "説明", width=300)
        self.InsertColumn(4, "ステータス", width=120)
        self.InsertColumn(5, "優先度", width=100)
        self.InsertColumn(6, "期限", width=120)
        self.InsertColumn(7, "作成日時", width=160)
        self.InsertColumn(8, "更新日時", width=160)
        self.InsertColumn(9, "操作", width=100)

    def _bind_events(self) -> None:
        """Bind events."""
        self.Bind(wx.EVT_LIST_ITEM_SELECTED, self._on_item_selected)
        self.Bind(wx.EVT_LIST_ITEM_DESELECTED, self._on_item_deselected)
        self.Bind(wx.EVT_LIST_ITEM_ACTIVATED, self._on_item_activated)
        self.Bind(wx.EVT_LIST_COL_CLICK, self._on_column_click)
        self.Bind(wx.EVT_LIST_ITEM_RIGHT_CLICK, self._on_item_right_click)
        self.Bind(wx.EVT_LEFT_DOWN, self._on_left_down)

    def set_items(self, items: List[TodoItem]) -> None:
        """Set items to display."""
        try:
            print(f"set_items: Setting {len(items)} items")
            # Reset debug counter
            if hasattr(self, '_debug_call_count'):
                delattr(self, '_debug_call_count')
            
            self._items = list(items) if items else []  # Ensure it's a list
            print(f"set_items: Items stored, length={len(self._items)}")
            
            # Use CallAfter to ensure UI is ready before setting item count
            wx.CallAfter(self._do_set_item_count, len(self._items))
        except Exception as e:
            import traceback
            error_msg = f"set_itemsエラー: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in set_items: {error_msg}", file=sys.stderr)
            wx.LogError(error_msg)
    
    def _do_set_item_count(self, count: int) -> None:
        """Set item count and refresh (called via CallAfter)."""
        try:
            print(f"_do_set_item_count: Setting count to {count}")
            self.SetItemCount(count)
            print(f"_do_set_item_count: Item count set, calling RefreshItems")
            # Use RefreshItems instead of Refresh for virtual lists
            if count > 0:
                self.RefreshItems(0, count - 1)
            print("_do_set_item_count: RefreshItems called")
        except Exception as e:
            import traceback
            error_msg = f"_do_set_item_countエラー: {e}\n\n{traceback.format_exc()}"
            print(f"ERROR in _do_set_item_count: {error_msg}", file=sys.stderr)
            wx.LogError(error_msg)

    def set_selected_ids(self, selected_ids: set) -> None:
        """Set selected item IDs."""
        self._selected_ids = selected_ids
        self.Refresh()

    def set_sorts(self, sorts: List[dict]) -> None:
        """Set sort configuration."""
        self._sorts = sorts
        self._update_column_headers()
        self.Refresh()

    def set_on_edit(self, callback: Callable) -> None:
        """Set edit callback."""
        self._on_edit = callback

    def OnGetItemText(self, item: int, column: int) -> str:
        """Get item text for virtual list."""
        try:
            # Validate item index
            if item < 0 or item >= len(self._items):
                return ""

            todo_item = self._items[item]
            if not todo_item:
                return ""

            try:
                if column == 0:  # Checkbox column - show checkbox state
                    item_id = getattr(todo_item, 'id', None)
                    if item_id is not None and hasattr(self, '_selected_ids') and item_id in self._selected_ids:
                        return "☑"
                    else:
                        return "☐"
                elif column == 1:  # ID
                    return str(getattr(todo_item, 'id', ''))
                elif column == 2:  # Title
                    title = getattr(todo_item, 'title', None)
                    return str(title) if title else ""
                elif column == 3:  # Description
                    desc = getattr(todo_item, 'description', None)
                    return str(desc) if desc else "-"
                elif column == 4:  # Status
                    status = getattr(todo_item, 'status', None)
                    return str(status) if status else ""
                elif column == 5:  # Priority
                    priority = getattr(todo_item, 'priority', None)
                    return str(priority) if priority else ""
                elif column == 6:  # Due date
                    due_date = getattr(todo_item, 'due_date', None)
                    if due_date:
                        try:
                            dt = parse_iso_datetime(str(due_date))
                            if dt:
                                return format_datetime_for_display(dt, include_time=False)
                        except Exception:
                            pass
                    return "-"
                elif column == 7:  # Created at
                    created_at = getattr(todo_item, 'created_at', None)
                    if created_at:
                        try:
                            dt = parse_iso_datetime(str(created_at))
                            if dt:
                                return format_datetime_for_display(dt, include_time=True)
                        except Exception:
                            pass
                        return str(created_at)
                    return ""
                elif column == 8:  # Updated at
                    updated_at = getattr(todo_item, 'updated_at', None)
                    if updated_at:
                        try:
                            dt = parse_iso_datetime(str(updated_at))
                            if dt:
                                return format_datetime_for_display(dt, include_time=True)
                        except Exception:
                            pass
                        return str(updated_at)
                    return ""
                elif column == 9:  # Actions
                    return "編集"

                return ""
            except Exception:
                return ""
        except Exception:
            return ""

    def _on_left_down(self, event: wx.MouseEvent) -> None:
        """Handle left mouse button down - for checkbox clicking."""
        try:
            point = event.GetPosition()
            hit_result = self.HitTest(point)
            if not hit_result:
                event.Skip()
                return
            
            item_idx = hit_result[0]
            
            if item_idx >= 0 and item_idx < len(self._items):
                col0_width = self.GetColumnWidth(0)
                x_pos = point.x
                
                if x_pos <= col0_width:  # Clicked in checkbox column
                    todo_item = self._items[item_idx]
                    if todo_item and hasattr(todo_item, 'id'):
                        self.controller.toggle_selection(todo_item.id)
                        wx.CallAfter(self.RefreshItem, item_idx)
                    return
        
            event.Skip()
        except Exception:
            event.Skip()

    def _on_item_selected(self, event: wx.ListEvent) -> None:
        """Handle item selection."""
        pass

    def _on_item_deselected(self, event: wx.ListEvent) -> None:
        """Handle item deselection."""
        pass

    def _on_item_activated(self, event: wx.ListEvent) -> None:
        """Handle item double-click (edit)."""
        item_idx = event.GetIndex()
        if item_idx < len(self._items) and self._on_edit:
            self._on_edit(self._items[item_idx])

    def _on_column_click(self, event: wx.ListEvent) -> None:
        """Handle column header click (sort)."""
        column = event.GetColumn()
        if column == 0:  # Checkbox column - select all
            if len(self._items) == 0:
                return

            all_selected = all(item.id in self._selected_ids for item in self._items)
            if all_selected:
                self.controller.deselect_all()
            else:
                item_ids = [item.id for item in self._items]
                self.controller.select_all(item_ids)
            return

        # Map column to column ID
        column_map = {
            1: COL_ID,
            2: COL_TITLE,
            3: COL_DESCRIPTION,
            4: COL_STATUS,
            5: COL_PRIORITY,
            6: COL_DUE_DATE,
            7: COL_CREATED_AT,
            8: COL_UPDATED_AT,
        }

        column_id = column_map.get(column)
        if not column_id:
            return

        # Toggle sort
        existing_sort = next(
            (s for s in self._sorts if s.get("columnId") == column_id),
            None
        )

        if existing_sort:
            direction = existing_sort.get("direction")
            if direction == "asc":
                existing_sort["direction"] = "desc"
            elif direction == "desc":
                # Remove sort
                self._sorts.remove(existing_sort)
            else:
                existing_sort["direction"] = "asc"
        else:
            self._sorts.append({
                "columnId": column_id,
                "direction": "asc"
            })

        # Update column headers with sort indicators
        self._update_column_headers()
        wx.PostEvent(self.GetParent(), wx.CommandEvent(wx.EVT_BUTTON.typeId, -3))

    def _update_column_headers(self) -> None:
        """Update column headers with sort indicators."""
        column_names = ["✓", "ID", "タイトル", "説明", "ステータス", "優先度", "期限", "作成日時", "更新日時", "操作"]
        column_map = {
            1: COL_ID,
            2: COL_TITLE,
            3: COL_DESCRIPTION,
            4: COL_STATUS,
            5: COL_PRIORITY,
            6: COL_DUE_DATE,
            7: COL_CREATED_AT,
            8: COL_UPDATED_AT,
        }

        for col_idx, col_name in enumerate(column_names):
            column_id = column_map.get(col_idx)
            if column_id:
                sort_config = next(
                    (s for s in self._sorts if s.get("columnId") == column_id),
                    None
                )
                if sort_config:
                    direction = sort_config.get("direction")
                    if direction == "asc":
                        display_name = f"{col_name} ↑"
                    elif direction == "desc":
                        display_name = f"{col_name} ↓"
                    else:
                        display_name = col_name
                else:
                    display_name = col_name
            else:
                display_name = col_name

            # Update column header text
            col = self.GetColumn(col_idx)
            col.SetText(display_name)
            self.SetColumn(col_idx, col)

    def _on_item_right_click(self, event: wx.ListEvent) -> None:
        """Handle right-click on item."""
        item_idx = event.GetIndex()
        if item_idx >= len(self._items):
            return

        todo_item = self._items[item_idx]

        menu = wx.Menu()
        edit_item = menu.Append(wx.ID_EDIT, "編集")
        delete_item = menu.Append(wx.ID_DELETE, "削除")

        self.Bind(wx.EVT_MENU, lambda e: self._on_edit(todo_item) if self._on_edit else None, edit_item)
        self.Bind(
            wx.EVT_MENU,
            lambda e: self._delete_item(todo_item),
            delete_item
        )

        self.PopupMenu(menu)
        menu.Destroy()

    def _delete_item(self, item: TodoItem) -> None:
        """Delete item."""
        dlg = wx.MessageDialog(
            self,
            f"アイテム「{item.title}」を削除しますか？",
            "確認",
            wx.YES_NO | wx.ICON_QUESTION
        )

        if dlg.ShowModal() == wx.ID_YES:
            self.controller.delete_items([item.id])
            self.controller.save_data()

        dlg.Destroy()

    def get_sorts(self) -> List[dict]:
        """Get current sort configuration."""
        return self._sorts

