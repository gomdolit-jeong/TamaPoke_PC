using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TamaPoke.Converters
{
    // 상태 바 색 (포켓몬 HP 바처럼): 60 이상 초록, 30 이상 노랑, 30 미만 빨강
    public class ProgressBarColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush Green = Freeze(System.Windows.Media.Color.FromRgb(0x4C, 0xC0, 0x5E));
        private static readonly SolidColorBrush Yellow = Freeze(System.Windows.Media.Color.FromRgb(0xF2, 0xC0, 0x30));
        private static readonly SolidColorBrush Red = Freeze(System.Windows.Media.Color.FromRgb(0xE8, 0x50, 0x3C));

        private static SolidColorBrush Freeze(System.Windows.Media.Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = value is IConvertible ? System.Convert.ToDouble(value, CultureInfo.InvariantCulture) : 100;
            return val >= 60 ? Green : val >= 30 ? Yellow : Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}