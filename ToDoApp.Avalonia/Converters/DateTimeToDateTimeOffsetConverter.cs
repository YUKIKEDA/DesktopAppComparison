using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ToDoApp.Avalonia.Converters;

public class DateTimeToDateTimeOffsetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        if (value is DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }

        var nullableDateTime = value as DateTime?;
        if (nullableDateTime.HasValue)
        {
            return new DateTimeOffset(nullableDateTime.Value);
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (targetType == typeof(DateTime?))
            {
                return dateTimeOffset.DateTime;
            }
            return dateTimeOffset.DateTime;
        }

        return null;
    }
}

