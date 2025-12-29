using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Converters;

public class SaveButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TodoItem item && item.Id > 0)
        {
            return "更新";
        }
        return "追加";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

