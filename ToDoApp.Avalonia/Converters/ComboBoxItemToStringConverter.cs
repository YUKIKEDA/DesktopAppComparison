using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ToDoApp.Avalonia.Converters;

public class ComboBoxItemToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ComboBoxItem item)
        {
            return item.Content?.ToString() ?? string.Empty;
        }
        if (value is string str)
        {
            return str;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBackは使用しない（OneWayバインディング）
        return value;
    }
}

