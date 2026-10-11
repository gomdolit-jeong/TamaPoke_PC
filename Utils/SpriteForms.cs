using System;
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

        // 폼이 여러 개면 10% 확률로 특수 폼 하나를 고릅니다. (이로치/암컷 파일은 후보에서 제외)
        public static string PickBaseForm(int speciesId, string[] allForms, Random rand)
        {
            string baseFileName = BaseFileName(speciesId);

            if (allForms.Length > 1 && rand.Next(100) < SPECIAL_FORM_CHANCE)
            {
                var specialForms = allForms.Where(f =>
                {
                    string name = Path.GetFileName(f);
                    return name != baseFileName && !name.ToLower().Contains("shiny") && !name.ToLower().Contains("female");
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
