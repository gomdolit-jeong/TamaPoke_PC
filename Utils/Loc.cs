using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Binding = System.Windows.Data.Binding;
using BindingMode = System.Windows.Data.BindingMode;
using System.Windows.Markup;

namespace TamaPoke.Utils
{
    // 다국어 문구: Lang/{언어}.json (실행 파일에 포함된 리소스)
    // - XAML:  Text="{u:T idle.feed}"            → 언어가 바뀌면 즉시 갱신
    // - 코드:  Loc.T("mood.happy"), Loc.F("battle.damage", damage)
    // 번역이 없는 키는 한국어 → 키 이름 순으로 대체합니다. (화면에 키가 보이면 번역 누락)
    public sealed class Loc : INotifyPropertyChanged
    {
        public const string Korean = "ko";
        public const string English = "en";
        public static readonly (string Code, string Name)[] Supported = { (Korean, "한국어"), (English, "English") };

        public static Loc Instance { get; } = new();

        // 언어가 바뀐 뒤 (코드에서 만든 문구를 다시 계산해야 하는 곳이 구독)
        public static event Action? LanguageChanged;

        private Dictionary<string, string> _strings = new();
        private Dictionary<string, string> _fallback = new();

        public string Language { get; private set; } = Korean;
        public bool IsEnglish => Language == English;

        public string this[string key] => Get(key);

        public static string T(string key) => Instance.Get(key);
        public static string F(string key, params object?[] args) => string.Format(T(key), args);

        // 저장된 설정 값이 없거나 지원하지 않는 언어면 윈도우 언어를 따라갑니다. (한국어 윈도우 → 한국어, 그 외 → 영어)
        public static string ResolveLanguage(string? saved)
        {
            if (saved != null && Supported.Any(s => s.Code == saved)) return saved;
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == Korean ? Korean : English;
        }

        public void SetLanguage(string language)
        {
            _fallback = LoadStrings(Korean);
            _strings = language == Korean ? _fallback : LoadStrings(language);
            Language = language;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnglish)));
            LanguageChanged?.Invoke();
        }

        private string Get(string key)
        {
            if (_strings.TryGetValue(key, out var text)) return text;
            if (_fallback.TryGetValue(key, out text)) return text;
            return key;
        }

        private static Dictionary<string, string> LoadStrings(string language)
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Lang.{language}.json");
                if (stream == null) return new();
                return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Loc] {language} 문구 로드 실패: {ex.Message}");
                return new();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    // XAML 단축 표기: {u:T 키} → Loc.Instance[키]에 단방향 바인딩
    [MarkupExtensionReturnType(typeof(object))]
    public class TExtension : MarkupExtension
    {
        public TExtension() { }
        public TExtension(string key) { Key = key; }

        [ConstructorArgument("key")]
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]") { Source = Loc.Instance, Mode = BindingMode.OneWay };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
