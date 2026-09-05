using System;
using System.Globalization;
using System.Windows.Data;

namespace TamaPoke.Converters
{
    // 🌟 획득한 배지 개수(GymBadges)와 요구하는 배지 번호(ConverterParameter)를 비교합니다.
    public class BadgeUnlockedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value: 현재 GymBadges 값
            // parameter: XAML에서 넘겨준 배지 번호 (예: "1", "2", "32" 등)
            if (value is int gymBadges && parameter != null)
            {
                if (int.TryParse(parameter.ToString(), out int requiredBadge))
                {
                    // 현재 배지 개수가 요구 배지 번호보다 크거나 같으면 True를 반환합니다.
                    return gymBadges >= requiredBadge;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}