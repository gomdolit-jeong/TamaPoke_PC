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

        // 설정을 불러오는 메서드 (파일이 없으면 기본 설정 객체를 생성하고 저장)
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