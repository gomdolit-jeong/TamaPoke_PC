using System;
using System.IO;

namespace TamaPoke.Utils
{
    // 실행 폴더 기준 리소스/데이터 경로를 한 곳에서 관리합니다.
    public static class AppPaths
    {
        public static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

        public static string ResourceFolder => Path.Combine(BaseDir, "Assets", "Resource");
        public static string SpriteFolder => Path.Combine(ResourceFolder, "PokemonSprites");
        public static string BackgroundFolder => Path.Combine(ResourceFolder, "Backgrounds");
        public static string BadgeFolder => Path.Combine(ResourceFolder, "Badges");
        public static string DataFolder => Path.Combine(BaseDir, "Data");

        public static string SpritePath(string fileName) => Path.Combine(SpriteFolder, fileName);
        public static string DataPath(string fileName) => Path.Combine(DataFolder, fileName);

        // 해당 도감 번호의 모든 스프라이트 파일 (기본/이로치/암컷/특수 폼). 폴더가 없으면 빈 배열
        public static string[] GetSpriteForms(int speciesId) =>
            Directory.Exists(SpriteFolder) ? Directory.GetFiles(SpriteFolder, $"p{speciesId:D4}*.bin") : Array.Empty<string>();
    }
}
