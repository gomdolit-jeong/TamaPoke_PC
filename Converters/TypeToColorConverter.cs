using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TamaPoke.Converters
{
    // 🌟 스킬 타입(데이터)을 색상(UI)으로 변환해주는 클래스입니다.
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 값이 없으면 기본 회색을 반환합니다.
            string typeStr = value?.ToString() ?? "";
            string hexColor = "#78909C";

            // 타입 이름에 따라 포켓몬 공식 색상과 유사한 헥스 코드를 지정합니다.
            // (영어와 한글을 모두 지원하도록 작성했습니다!)
            switch (typeStr.ToLower())
            {
                case "normal": case "노말": hexColor = "#A8A878"; break;
                case "fire": case "불꽃": hexColor = "#F08030"; break;
                case "water": case "물": hexColor = "#6890F0"; break;
                case "electric": case "전기": hexColor = "#F8D030"; break;
                case "grass": case "풀": hexColor = "#78C850"; break;
                case "ice": case "얼음": hexColor = "#98D8D8"; break;
                case "fighting": case "격투": hexColor = "#C03028"; break;
                case "poison": case "독": hexColor = "#A040A0"; break;
                case "ground": case "땅": hexColor = "#E0C068"; break;
                case "flying": case "비행": hexColor = "#A890F0"; break;
                case "psychic": case "에스퍼": hexColor = "#F85888"; break;
                case "bug": case "벌레": hexColor = "#A8B820"; break;
                case "rock": case "바위": hexColor = "#B8A038"; break;
                case "ghost": case "고스트": hexColor = "#705898"; break;
                case "dragon": case "드래곤": hexColor = "#7038F8"; break;
                case "dark": case "악": hexColor = "#705848"; break;
                case "steel": case "강철": hexColor = "#B8B8D0"; break;
                case "fairy": case "페어리": hexColor = "#EE99AC"; break;
            }

            // 헥스 문자열을 WPF가 이해할 수 있는 색상 붓(SolidColorBrush)으로 변환하여 돌려줍니다.
            return new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // UI 색상을 다시 데이터로 돌리는 기능은 필요 없으므로 구현하지 않습니다.
            throw new NotImplementedException();
        }
    }
}