using System;

namespace ToDoApp.WinUI.Models
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "未着手";
        public string Priority { get; set; } = "中";
        public DateTimeOffset? DueDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsCompleted { get; set; }
    }
}

