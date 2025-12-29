using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ToDoApp.Avalonia.Converters;

public class DateTimeToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "-";
        }

        DateTime dateTime;
        if (value is DateTime dt)
        {
            dateTime = dt;
        }
        else
        {
            var nullableDateTime = value as DateTime?;
            if (nullableDateTime.HasValue)
            {
                dateTime = nullableDateTime.Value;
            }
            else
            {
                return "-";
            }
        }

        var format = parameter as string;
        if (string.IsNullOrEmpty(format))
        {
            format = dateTime.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm";
        }
        return dateTime.ToString(format);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // DataGridは読み取り専用なので、ConvertBackは使用されない
        // ただし、エラーを避けるために実装
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return targetType == typeof(DateTime?) ? (DateTime?)null : default(DateTime);
        }

        var format = parameter as string;
        if (string.IsNullOrEmpty(format))
        {
            format = "yyyy-MM-dd HH:mm";
        }

        if (DateTime.TryParseExact(value.ToString(), format, culture, DateTimeStyles.None, out var result))
        {
            if (targetType == typeof(DateTime?))
            {
                return (DateTime?)result;
            }
            return result;
        }

        return targetType == typeof(DateTime?) ? (DateTime?)null : default(DateTime);
    }
}

