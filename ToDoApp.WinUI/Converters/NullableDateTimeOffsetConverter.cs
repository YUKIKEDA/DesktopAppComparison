using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ToDoApp.WinUI.Converters
{
    public class NullableDateTimeOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset;
            }
            // nullの場合はUnsetValueを返す（DatePickerがnullを適切に処理できるように）
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset;
            }
            // nullの場合はnullを返す（DateTimeOffset?型なので）
            return null;
        }
    }
}

