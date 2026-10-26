using System;
using System.Globalization;
using System.Windows.Data;
using TamaPoke.Utils;

namespace TamaPoke.Converters
{
    // 값을 Lang 문구에 끼워 넣습니다. 예: {Binding GymBadges, Converter={x:Static conv:LocFormatConverter.Instance}, ConverterParameter=profile.badges}
    // ("획득한 뱃지: {0} / 64" / "Badges: {0} / 64"). 언어가 바뀌면 RefreshLocalizedTexts로 다시 계산됩니다.
    public class LocFormatConverter : IValueConverter
    {
        public static LocFormatConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            Loc.F(parameter as string ?? string.Empty, value);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
