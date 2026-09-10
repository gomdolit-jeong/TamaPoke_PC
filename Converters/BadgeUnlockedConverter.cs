using System;
using System.Globalization;
using System.Windows.Data;

namespace TamaPoke.Converters
{
    // 🌟 IMultiValueConverter를 구현하도록 변경합니다.
    public class BadgeUnlockedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;

            // 첫 번째 값: 보유 중인 뱃지 데이터 (int 또는 비트 연산 값 등)
            if (values[0] is int gymBadges && values[1] is int globalIndex)
            {
                // 예시: 비트 연산이나 개수 비교를 통해 해금 여부 판별
                // (기존에 구현하셨던 로직에 맞춰서 값을 비교해 주시면 됩니다!)
                int requiredBadges = globalIndex;
                return gymBadges >= requiredBadges;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}