using System.Globalization;
using System.Windows.Data;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Converters
{
    public class SaveButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TodoItem item && item.Id > 0)
            {
                return "更新";
            }
            return "追加";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

