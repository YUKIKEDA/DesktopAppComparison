"""Date and time utility functions."""
from datetime import datetime
from typing import Optional


def parse_iso_datetime(date_str: str) -> Optional[datetime]:
    """Parse ISO 8601 datetime string with various formats.
    
    Supports:
    - With timezone: "2025-12-29T23:45:13Z" or "2025-12-29T23:45:13+00:00"
    - Without timezone: "2025-12-29T23:45:13"
    - With microseconds: "2025-12-29T23:45:26.570803"
    - Date only: "2025-12-29"
    """
    if not date_str or not isinstance(date_str, str):
        return None
    
    try:
        # Try standard fromisoformat first
        if "T" in date_str:
            # Handle timezone
            if date_str.endswith("Z"):
                date_str = date_str.replace("Z", "+00:00")
            return datetime.fromisoformat(date_str)
        else:
            # Date only
            return datetime.fromisoformat(date_str)
    except (ValueError, AttributeError):
        # Try alternative parsing methods
        try:
            # Try parsing with strptime for common formats
            formats = [
                "%Y-%m-%dT%H:%M:%S",
                "%Y-%m-%dT%H:%M:%S.%f",
                "%Y-%m-%dT%H:%M:%SZ",
                "%Y-%m-%dT%H:%M:%S.%fZ",
                "%Y-%m-%d",
            ]
            for fmt in formats:
                try:
                    return datetime.strptime(date_str, fmt)
                except ValueError:
                    continue
        except Exception:
            pass
    
    return None


def format_datetime_for_display(dt: datetime, include_time: bool = True) -> str:
    """Format datetime for display."""
    if include_time:
        return dt.strftime("%Y-%m-%d %H:%M")
    else:
        return dt.strftime("%Y-%m-%d")


def safe_parse_datetime(date_str: Optional[str], default: Optional[str] = None) -> Optional[str]:
    """Safely parse datetime string and return formatted string or default."""
    if not date_str:
        return default
    
    dt = parse_iso_datetime(date_str)
    if dt:
        return dt.isoformat()
    
    return default

