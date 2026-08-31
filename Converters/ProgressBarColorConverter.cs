using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TamaPoke.Converters
{
    public class ProgressBarColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                // 수치가 30 미만이면 빨간색, 그 이상이면 초록색
                return val < 30 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Green);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}