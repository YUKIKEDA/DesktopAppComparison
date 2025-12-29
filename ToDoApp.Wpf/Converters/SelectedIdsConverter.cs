using System.Globalization;
using System.Windows.Data;
using ToDoApp.Wpf.Models;

namespace ToDoApp.Wpf.Converters
{
    public class SelectedIdsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[0] is HashSet<int> selectedIds && values[1] is TodoItem item)
            {
                return selectedIds.Contains(item.Id);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

