using System;
using System.Globalization;
using System.Windows.Data;

namespace SchoolERP.Helpers
{
    public class FingerprintIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id && id != 0)
            {
                return id.ToString();
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
