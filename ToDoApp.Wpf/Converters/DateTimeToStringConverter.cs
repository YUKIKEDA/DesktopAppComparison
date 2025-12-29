using System.Globalization;
using System.Windows.Data;

namespace ToDoApp.Wpf.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "-";
            }

            DateTime dateTime;
            if (value is DateTime dt)
            {
                dateTime = dt;
            }
            else
            {
                var nullableDateTime = value as DateTime?;
                if (nullableDateTime.HasValue)
                {
                    dateTime = nullableDateTime.Value;
                }
                else
                {
                    return "-";
                }
            }

            var format = parameter as string;
            if (string.IsNullOrEmpty(format))
            {
                format = dateTime.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm";
            }
            return dateTime.ToString(format);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

