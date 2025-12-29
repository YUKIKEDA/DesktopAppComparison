using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Converters;

public class EditingItemTitleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TodoItem item && item.Id > 0)
        {
            return "アイテムを編集";
        }
        return "新しいアイテムを追加";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

