using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
// Tpk2Decoder가 있는 실제 네임스페이스를 참조합니다.
using TamaPoke.Utils.Service;

namespace TamaPoke.Converters
{
    public class IdToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id && id > 0)
            {
                // 🌟 현재 프로젝트 폴더 구조에 맞게 경로를 동적으로 지정합니다.
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string monPath = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"p{id:D3}.bin");

                if (File.Exists(monPath))
                {
                    var frames = Tpk2Decoder.LoadAnimation(monPath, 0);

                    if (frames != null && frames.Length > 0)
                    {
                        return frames[0];
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}