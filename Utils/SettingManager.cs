using System;
using System.IO;
using System.Text.Json;
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    public static class SettingsManager
    {
        // 설정 파일 경로 (실행 파일과 같은 폴더의 settings.json)
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private static GameSettings? _current;

        // 게임 로직이 참조하는 현재 설정입니다. Save()가 호출되면 즉시 갱신됩니다.
        public static GameSettings Current => _current ??= Load();

        // 설정을 파일에서 새로 불러오는 메서드 (파일이 없으면 기본 설정 객체를 생성하고 저장)
        // 설정 창처럼 '편집용 사본'이 필요한 곳에서 사용합니다.
        public static GameSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string jsonString = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<GameSettings>(jsonString);
                    if (settings != null) return settings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"설정 불러오기 실패: {ex.Message}");
            }

            // 파일이 없거나 오류가 나면 기본값으로 생성 후 저장
            var defaultSettings = new GameSettings();
            Save(defaultSettings);
            return defaultSettings;
        }

        // 설정을 파일에 저장하는 메서드
        public static void Save(GameSettings settings)
        {
            _current = settings;

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"설정 저장 실패: {ex.Message}");
            }
        }
    }
}
