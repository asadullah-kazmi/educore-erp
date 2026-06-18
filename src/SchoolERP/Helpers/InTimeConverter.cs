using System;
using System.Globalization;
using System.Windows.Data;

namespace SchoolERP.Helpers
{
    public class InTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime time)
            {
                return time.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
