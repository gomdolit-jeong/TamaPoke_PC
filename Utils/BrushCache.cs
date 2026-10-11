using System.Collections.Generic;
using Brush = System.Windows.Media.Brush;
using BrushConverter = System.Windows.Media.BrushConverter;

namespace TamaPoke.Utils
{
    // "#RRGGBB" 색 문자열을 Freeze된 브러시로 바꿔 재사용합니다. (속성 getter마다 새 브러시를 만들지 않도록)
    public static class BrushCache
    {
        private static readonly Dictionary<string, Brush> _cache = new();
        private static readonly BrushConverter _converter = new();

        public static Brush Get(string hex)
        {
            if (!_cache.TryGetValue(hex, out var brush))
            {
                brush = (Brush)_converter.ConvertFrom(hex)!;
                brush.Freeze();
                _cache[hex] = brush;
            }
            return brush;
        }
    }
}
