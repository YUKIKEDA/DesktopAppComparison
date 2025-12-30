using System;
using Microsoft.UI.Xaml.Data;

namespace ToDoApp.WinUI.Converters
{
    public class NullableDateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dateTime)
            {
                var format = parameter as string ?? "yyyy-MM-dd";
                return dateTime.ToString(format);
            }
            if (value is DateTime date)
            {
                var format = parameter as string ?? "yyyy-MM-dd";
                return date.ToString(format);
            }
            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str) && str != "-")
            {
                if (DateTimeOffset.TryParse(str, out var result))
                {
                    return result;
                }
            }
            return null;
        }
    }
}

