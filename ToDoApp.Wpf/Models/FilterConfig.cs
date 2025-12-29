namespace ToDoApp.Wpf.Models
{
    public class FilterConfig
    {
        public string ColumnId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "text", "date", "select"
        public object? Value { get; set; } // string, string[], or DateRange
    }

    public class DateRange
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}

