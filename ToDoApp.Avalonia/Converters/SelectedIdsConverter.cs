using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using ToDoApp.Avalonia.Models;

namespace ToDoApp.Avalonia.Converters;

public class SelectedIdsConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
            return false;

        if (values[0] is HashSet<int> selectedIds && values[1] is TodoItem item)
        {
            return selectedIds.Contains(item.Id);
        }
        return false;
    }

    public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

