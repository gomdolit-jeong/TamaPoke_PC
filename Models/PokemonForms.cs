using System.Collections.Generic;
using System.Linq;

namespace TamaPoke.Models
{
    // 지역 폼(알로라/가라르/히스이/팔데아 등)과 이로치 관련 정보
    public static class PokemonForms
    {
        // 특수 폼은 기본 종족과 타입이 다릅니다. (스프라이트 파일명에 포함된 키워드로 판별)
        private static readonly List<(int Id, string Keyword, PokemonType Type1, PokemonType Type2)> FormTypeOverrides = new()
        {
            (19, "alola", PokemonType.Dark, PokemonType.Normal),
            (20, "alola", PokemonType.Dark, PokemonType.Normal),
            (26, "alola", PokemonType.Electric, PokemonType.Psychic),
            (27, "alola", PokemonType.Ice, PokemonType.Steel),
            (28, "alola", PokemonType.Ice, PokemonType.Steel),
            (37, "alola", PokemonType.Ice, PokemonType.None),
            (38, "alola", PokemonType.Ice, PokemonType.Fairy),
            (50, "alola", PokemonType.Ground, PokemonType.Steel),
            (51, "alola", PokemonType.Ground, PokemonType.Steel),
            (52, "alola", PokemonType.Dark, PokemonType.None),
            (53, "alola", PokemonType.Dark, PokemonType.None),
            (74, "alola", PokemonType.Rock, PokemonType.Electric),
            (75, "alola", PokemonType.Rock, PokemonType.Electric),
            (76, "alola", PokemonType.Rock, PokemonType.Electric),
            (88, "alola", PokemonType.Poison, PokemonType.Dark),
            (89, "alola", PokemonType.Poison, PokemonType.Dark),
            (103, "alola", PokemonType.Grass, PokemonType.Dragon),
            (105, "alola", PokemonType.Fire, PokemonType.Ghost),

            (52, "galar", PokemonType.Steel, PokemonType.None),
            (77, "galar", PokemonType.Psychic, PokemonType.None),
            (78, "galar", PokemonType.Psychic, PokemonType.Fairy),
            (79, "galar", PokemonType.Poison, PokemonType.Psychic),
            (80, "galar", PokemonType.Poison, PokemonType.Psychic),
            (199, "galar", PokemonType.Poison, PokemonType.Psychic),
            (83, "galar", PokemonType.Fighting, PokemonType.None),
            (110, "galar", PokemonType.Poison, PokemonType.Fairy),
            (122, "galar", PokemonType.Ice, PokemonType.Psychic),
            (144, "galar", PokemonType.Psychic, PokemonType.Flying),
            (145, "galar", PokemonType.Fighting, PokemonType.Flying),
            (146, "galar", PokemonType.Dark, PokemonType.Flying),
            (222, "galar", PokemonType.Ghost, PokemonType.None),
            (263, "galar", PokemonType.Dark, PokemonType.Normal),
            (264, "galar", PokemonType.Dark, PokemonType.Normal),
            (554, "galar", PokemonType.Ice, PokemonType.None),
            (555, "zen", PokemonType.Ice, PokemonType.Fire),
            (555, "galar", PokemonType.Ice, PokemonType.None),
            (562, "galar", PokemonType.Ground, PokemonType.Ghost),
            (618, "galar", PokemonType.Ground, PokemonType.Steel),

            (58, "hisui", PokemonType.Fire, PokemonType.Rock),
            (59, "hisui", PokemonType.Fire, PokemonType.Rock),
            (100, "hisui", PokemonType.Electric, PokemonType.Grass),
            (101, "hisui", PokemonType.Electric, PokemonType.Grass),
            (157, "hisui", PokemonType.Fire, PokemonType.Ghost),
            (211, "hisui", PokemonType.Dark, PokemonType.Poison),
            (215, "hisui", PokemonType.Fighting, PokemonType.Poison),
            (503, "hisui", PokemonType.Water, PokemonType.Dark),
            (549, "hisui", PokemonType.Grass, PokemonType.Fighting),
            (550, "hisui", PokemonType.Water, PokemonType.None),
            (570, "hisui", PokemonType.Normal, PokemonType.Ghost),
            (571, "hisui", PokemonType.Normal, PokemonType.Ghost),
            (628, "hisui", PokemonType.Psychic, PokemonType.Flying),
            (705, "hisui", PokemonType.Steel, PokemonType.Dragon),
            (706, "hisui", PokemonType.Steel, PokemonType.Dragon),
            (713, "hisui", PokemonType.Ice, PokemonType.Rock),
            (724, "hisui", PokemonType.Grass, PokemonType.Fighting),

            (194, "paldea", PokemonType.Poison, PokemonType.Ground),

            (128, "blaze", PokemonType.Fighting, PokemonType.Fire),
            (128, "_0002", PokemonType.Fighting, PokemonType.Fire),
            (128, "aqua", PokemonType.Fighting, PokemonType.Water),
            (128, "_0003", PokemonType.Fighting, PokemonType.Water),
            (128, "combat", PokemonType.Fighting, PokemonType.None),
            (128, "_0001", PokemonType.Fighting, PokemonType.None),
            (128, "paldea", PokemonType.Fighting, PokemonType.None)
        };

        public static (PokemonType Type1, PokemonType Type2) GetTypes(int speciesId, string? spriteFileName)
        {
            if (!string.IsNullOrEmpty(spriteFileName))
            {
                string lowerFile = spriteFileName.ToLower();
                var overrideData = FormTypeOverrides.FirstOrDefault(x => x.Id == speciesId && lowerFile.Contains(x.Keyword));
                if (overrideData.Id != 0) return (overrideData.Type1, overrideData.Type2);
            }

            var info = PokemonDex.Get(speciesId);
            return (info?.Type1 ?? PokemonType.Normal, info?.Type2 ?? PokemonType.None);
        }

        // 프로필 등에 표시할 폼 설명 (예: "✨ 색이 다른 포켓몬 (알로라의 모습)")
        public static string GetDescription(int speciesId, string? spriteFileName, bool isShiny)
        {
            string formText = "";
            string shinyText = isShiny ? "✨ 색이 다른 포켓몬" : "";

            if (!string.IsNullOrEmpty(spriteFileName))
            {
                string lowerFile = spriteFileName.ToLower();

                if (lowerFile.Contains("shiny")) shinyText = "✨ 색이 다른 포켓몬";

                var formKeywords = new Dictionary<string, string>
                {
                    { "combat", "팔데아의 모습 (투쟁종)" },
                    { "_0001", "팔데아의 모습 (투쟁종)" },
                    { "blaze", "팔데아의 모습 (블레이즈종)" },
                    { "_0002", "팔데아의 모습 (블레이즈종)" },
                    { "aqua", "팔데아의 모습 (워터종)" },
                    { "_0003", "팔데아의 모습 (워터종)" },
                    { "alola", "알로라의 모습" },
                    { "galar", "가라르의 모습" },
                    { "hisui", "히스이의 모습" },
                    { "paldea", "팔데아의 모습" },
                    { "mega", "메가진화" }
                };

                foreach (var pair in formKeywords)
                {
                    if (lowerFile.Contains(pair.Key))
                    {
                        if ((pair.Key == "_0001" || pair.Key == "_0002" || pair.Key == "_0003") && speciesId != 128) continue;
                        formText = pair.Value;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(formText) && !lowerFile.Contains("_0000") && !lowerFile.Contains("egg") && !lowerFile.Contains("shiny") && lowerFile.Contains("_"))
                {
                    formText = "특수한 모습";
                }
            }

            if (!string.IsNullOrEmpty(shinyText) && !string.IsNullOrEmpty(formText)) return $"{shinyText} ({formText})";
            else if (!string.IsNullOrEmpty(shinyText)) return shinyText;
            else if (!string.IsNullOrEmpty(formText)) return formText;

            return "";
        }
    }
}
