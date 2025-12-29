"""TodoItem data model."""
from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional


@dataclass
class TodoItem:
    """Todo item data model."""
    id: int
    title: str
    description: str
    status: str  # "未着手", "進行中", "完了"
    priority: str  # "低", "中", "高"
    due_date: Optional[str] = None  # ISO 8601 format
    created_at: str = field(default_factory=lambda: datetime.now().isoformat())
    updated_at: str = field(default_factory=lambda: datetime.now().isoformat())
    is_completed: bool = False

    def to_dict(self) -> dict:
        """Convert to dictionary for JSON serialization."""
        return {
            "id": self.id,
            "title": self.title,
            "description": self.description,
            "status": self.status,
            "priority": self.priority,
            "dueDate": self.due_date,
            "createdAt": self.created_at,
            "updatedAt": self.updated_at,
            "isCompleted": self.is_completed,
        }

    @classmethod
    def from_dict(cls, data: dict) -> "TodoItem":
        """Create from dictionary."""
        # Validate required fields
        if not isinstance(data, dict):
            raise ValueError(f"Expected dict, got {type(data)}")
        
        required_fields = ["id", "title", "status", "priority"]
        missing_fields = [field for field in required_fields if field not in data]
        if missing_fields:
            raise ValueError(f"Missing required fields: {missing_fields}")
        
        return cls(
            id=int(data["id"]),
            title=str(data["title"]),
            description=str(data.get("description", "")),
            status=str(data["status"]),
            priority=str(data["priority"]),
            due_date=data.get("dueDate") if data.get("dueDate") else None,
            created_at=str(data.get("createdAt", datetime.now().isoformat())),
            updated_at=str(data.get("updatedAt", datetime.now().isoformat())),
            is_completed=bool(data.get("isCompleted", False)),
        )

    def update(self, **kwargs) -> None:
        """Update item fields."""
        for key, value in kwargs.items():
            if hasattr(self, key):
                setattr(self, key, value)
        self.updated_at = datetime.now().isoformat()

