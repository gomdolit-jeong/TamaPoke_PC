using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TamaPoke.Utils
{
    // 스프라이트 파일(폼) 결정 규칙: 부화한 포켓몬과 야생 포켓몬이 같은 규칙을 씁니다.
    // 파일명 예: p0025.bin, p0025_Shiny.bin, p0025_Female.bin, p0025_Shiny_Female.bin, p0019_Alola.bin
    public static class SpriteForms
    {
        private const int SPECIAL_FORM_CHANCE = 10; // 특수 폼(알로라 등)이 나올 확률(%)

        public static string BaseFileName(int speciesId) => $"p{speciesId:D4}.bin";

        // 배틀 전용 폼(메가/원시회귀/거다이맥스)과 섀도 폼은 태어나거나 야생에서 나오지 않습니다.
        private static readonly string[] BattleOnlyFormTokens = { "mega", "primal", "gigantamax", "shadow" };

        public static bool IsBattleOnlyForm(string fileName) =>
            Path.GetFileNameWithoutExtension(fileName).Split('_').Skip(1).Any(t => BattleOnlyFormTokens.Contains(t.ToLowerInvariant()));

        // 기본 이미지(pNNNN.bin)가 있는 도감 번호. 이미지 업데이트 뒤 ResetCache()로 다시 읽습니다.
        private static HashSet<int>? _baseSpriteIds;
        private static HashSet<int> BaseSpriteIds => _baseSpriteIds ??= LoadBaseSpriteIds();

        private static HashSet<int> LoadBaseSpriteIds()
        {
            var ids = new HashSet<int>();
            if (!Directory.Exists(AppPaths.SpriteFolder)) return ids;
            foreach (var file in Directory.EnumerateFiles(AppPaths.SpriteFolder, "p*.bin"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (name.Length == 5 && int.TryParse(name.AsSpan(1), out int id) && id > 0) ids.Add(id);
            }
            return ids;
        }

        public static void ResetCache() => _baseSpriteIds = null;

        // 부화/진화/야생으로 쓸 수 있는 포켓몬인지: 기본 이미지가 있어야 합니다. (없으면 알 이미지로 보임)
        // 이미지를 아직 하나도 받지 않은 경우(첫 실행)는 거르지 않습니다.
        public static bool IsUsable(int speciesId) => BaseSpriteIds.Count == 0 || BaseSpriteIds.Contains(speciesId);

        // 폼이 여러 개면 10% 확률로 특수 폼 하나를 고릅니다. (이로치/암컷/배틀 전용 폼은 후보에서 제외)
        public static string PickBaseForm(int speciesId, string[] allForms, Random rand)
        {
            string baseFileName = BaseFileName(speciesId);

            if (allForms.Length > 1 && rand.Next(100) < SPECIAL_FORM_CHANCE)
            {
                var specialForms = allForms.Where(f =>
                {
                    string name = Path.GetFileName(f);
                    return name != baseFileName && !name.ToLower().Contains("shiny") && !name.ToLower().Contains("female") && !IsBattleOnlyForm(name);
                }).ToArray();

                if (specialForms.Length > 0) return Path.GetFileName(specialForms[rand.Next(specialForms.Length)]);
            }
            return baseFileName;
        }

        // 기준 폼에 이로치/암컷 변형 파일이 있으면 그 파일로 바꿉니다. 없으면 기준 폼 그대로
        public static string ApplyVariants(string[] allForms, string baseFileName, bool shiny, bool female)
        {
            string stem = Path.GetFileNameWithoutExtension(baseFileName);
            bool tryShiny = shiny && !stem.ToLower().Contains("shiny");
            bool tryFemale = female && !stem.ToLower().Contains("female");

            string? Find(string suffix) =>
                allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(stem + suffix, StringComparison.OrdinalIgnoreCase));

            string? match = null;
            if (tryShiny && tryFemale) match = Find("_Shiny_Female") ?? Find("_Female_Shiny") ?? Find("_Shiny");
            else if (tryFemale) match = Find("_Female");
            else if (tryShiny) match = Find("_Shiny");

            return match != null ? Path.GetFileName(match) : baseFileName;
        }
    }
}
