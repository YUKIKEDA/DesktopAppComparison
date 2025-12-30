using System;
using Microsoft.UI.Xaml.Data;

namespace ToDoApp.WinUI.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dateTime)
            {
                var format = parameter as string ?? "yyyy-MM-dd HH:mm";
                return dateTime.ToString(format);
            }
            if (value is DateTime date)
            {
                var format = parameter as string ?? "yyyy-MM-dd HH:mm";
                return date.ToString(format);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is string str && DateTimeOffset.TryParse(str, out var result))
            {
                return result;
            }
            return DateTimeOffset.Now;
        }
    }
}

