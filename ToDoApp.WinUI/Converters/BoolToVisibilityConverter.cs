using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ToDoApp.WinUI.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                var invert = parameter as string == "Invert";
                return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                var invert = parameter as string == "Invert";
                return (visibility == Visibility.Visible) ^ invert;
            }
            return false;
        }
    }
}

