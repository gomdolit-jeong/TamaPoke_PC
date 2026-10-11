using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TamaPoke.Models;

namespace TamaPoke.Converters
{
    // 상태이상(SkillAilment)을 배틀 화면 배지 표시값으로 변환합니다.
    // ConverterParameter: Text(배지 글자), Fx(스프라이트 위 이모지), Bg/Fg/Border(배지 색), Visibility
    public class AilmentStyleConverter : IValueConverter
    {
        private static readonly BrushConverter _brush = new();

        private static (string Text, string Fx, string Bg, string Fg, string Border) Style(SkillAilment a) => a switch
        {
            SkillAilment.Burn => ("화상", "🔥", "#FAECE7", "#712B13", "#D85A30"),
            SkillAilment.Poison => ("독", "☠️", "#EEEDFE", "#3C3489", "#7F77DD"),
            SkillAilment.BadPoison => ("맹독", "💀", "#FBEAF0", "#72243E", "#D4537E"),
            SkillAilment.Paralysis => ("마비", "⚡", "#FAEEDA", "#633806", "#BA7517"),
            SkillAilment.Sleep => ("잠듦", "💤", "#F1EFE8", "#444441", "#888780"),
            SkillAilment.Freeze => ("얼음", "❄️", "#E6F1FB", "#0C447C", "#378ADD"),
            _ => ("", "", "#00000000", "#00000000", "#00000000")
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ailment = value is SkillAilment a ? a : SkillAilment.None;
            var s = Style(ailment);

            return (parameter as string) switch
            {
                "Text" => s.Text,
                "Fx" => s.Fx,
                "Bg" => _brush.ConvertFrom(s.Bg)!,
                "Fg" => _brush.ConvertFrom(s.Fg)!,
                "Border" => _brush.ConvertFrom(s.Border)!,
                "Visibility" => ailment == SkillAilment.None ? Visibility.Collapsed : Visibility.Visible,
                _ => DependencyProperty.UnsetValue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
