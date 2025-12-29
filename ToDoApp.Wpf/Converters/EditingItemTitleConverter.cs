using System.Globalization;
using System.Windows.Data;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Converters
{
    public class EditingItemTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TodoItem item && item.Id > 0)
            {
                return "アイテムを編集";
            }
            return "新しいアイテムを追加";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

