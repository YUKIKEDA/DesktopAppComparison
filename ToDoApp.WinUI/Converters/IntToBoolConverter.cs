using System;
using Microsoft.UI.Xaml.Data;

namespace ToDoApp.WinUI.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 1 : 0;
            }
            return 0;
        }
    }
}

