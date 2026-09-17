using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Encodings.Web; // 🌟 한글 저장을 위한 네임스페이스
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    public class PokemonSkillUpdater
    {
        private static readonly HttpClient client = new HttpClient();

        // API 파싱용 내부 클래스들
        private class PokeApiResult { public List<ApiMoveSlot> moves { get; set; } = new(); }
        private class ApiMoveSlot { public ApiMove move { get; set; } = new(); public List<ApiVersionGroupDetail> version_group_details { get; set; } = new(); }
        private class ApiMove { public string name { get; set; } = ""; public string url { get; set; } = ""; }
        private class ApiVersionGroupDetail { public int level_learned_at { get; set; } public ApiMethod move_learn_method { get; set; } = new(); public ApiVersionGroup version_group { get; set; } = new(); }
        private class ApiMethod { public string name { get; set; } = ""; }
        private class ApiVersionGroup { public string name { get; set; } = ""; }

        private class ApiMoveDetail
        {
            public int id { get; set; }
            public int? power { get; set; }
            public int? accuracy { get; set; }
            public ApiNamedResource type { get; set; } = new();
            public ApiNamedResource damage_class { get; set; } = new();
            public List<ApiName> names { get; set; } = new();
            public ApiMoveMeta? meta { get; set; }
        }

        private class ApiMoveMeta
        {
            public ApiNamedResource ailment { get; set; } = new();
            public int ailment_chance { get; set; }
        }

        private class ApiNamedResource { public string name { get; set; } = ""; }
        private class ApiName { public string name { get; set; } = ""; public ApiNamedResource language { get; set; } = new(); }

        public static async Task GenerateOfflineSkillJsonAsync(IProgress<string>? progress = null, Action<string, string>? trayNotifier = null)
        {
            var allLearnsets = new List<PokemonLearnset>();
            var moveDatabase = new Dictionary<int, SkillInfo>();

            for (int id = 1; id <= GameConstants.MAX_POKEMON_ID; id++)
            {
                try
                {
                    string url = $"https://pokeapi.co/api/v2/pokemon/{id}";
                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) continue;

                    string json = await response.Content.ReadAsStringAsync();
                    var pokeData = JsonSerializer.Deserialize<PokeApiResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (pokeData?.moves != null)
                    {
                        var movesByVersion = new Dictionary<string, List<LearnMove>>();

                        string[] targetVersions = {
                            "scarlet-violet",
                            "sword-shield",
                            "brilliant-diamond-and-shining-pearl",
                            "ultra-sun-ultra-moon",
                            "sun-moon"
                        };

                        foreach (var m in pokeData.moves)
                        {
                            var levelUpDetails = m.version_group_details
                                .Where(v => v.move_learn_method.name == "level-up");

                            foreach (var detail in levelUpDetails)
                            {
                                string vgName = detail.version_group.name;

                                if (targetVersions.Contains(vgName))
                                {
                                    if (!movesByVersion.ContainsKey(vgName))
                                        movesByVersion[vgName] = new List<LearnMove>();

                                    int moveId = int.Parse(m.move.url.TrimEnd('/').Split('/').Last());

                                    if (!movesByVersion[vgName].Any(x => x.MoveId == moveId))
                                    {
                                        movesByVersion[vgName].Add(new LearnMove { Level = detail.level_learned_at, MoveId = moveId });

                                        if (!moveDatabase.ContainsKey(moveId))
                                        {
                                            progress?.Report($"새로운 스킬 발견! 상세 정보 다운로드 중... [{m.move.name}]");
                                            var skillInfo = await FetchMoveDetailAsync(m.move.url);
                                            if (skillInfo != null) moveDatabase[moveId] = skillInfo;
                                        }
                                    }
                                }
                            }
                        }

                        if (movesByVersion.Count > 0)
                        {
                            foreach (var key in movesByVersion.Keys.ToList())
                            {
                                movesByVersion[key] = movesByVersion[key].OrderBy(v => v.Level).ToList();
                            }

                            allLearnsets.Add(new PokemonLearnset { SpeciesId = id, MovesByVersion = movesByVersion });
                        }
                    }

                    progress?.Report($"도감 번호 {id}/{TamaPoke.Models.GameConstants.MAX_POKEMON_ID} 스킬 데이터 처리 완료...");
                }
                catch (Exception) { /* 무시 */ }
            }

            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            // 🌟 한글이 깨지지 않고 예쁘게 저장되도록 옵션을 설정합니다!
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                // 전체 경로를 직접 명시하여 컴파일러가 절대 헷갈리지 않게 만듭니다.
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
            };

            string learnsetPath = Path.Combine(dataFolderPath, "pokemon_skills.json");
            File.WriteAllText(learnsetPath, JsonSerializer.Serialize(allLearnsets, jsonOptions));

            string movedbPath = Path.Combine(dataFolderPath, "pokemon_movedb.json");
            File.WriteAllText(movedbPath, JsonSerializer.Serialize(moveDatabase.Values.ToList(), jsonOptions));

            SkillDex.LoadSkillData();
            progress?.Report("✨ 포켓몬 스킬 트리 및 전체 스킬 도감 DB 구축 완료!");
        }

        private static async Task<SkillInfo?> FetchMoveDetailAsync(string moveUrl)
        {
            try
            {
                var response = await client.GetAsync(moveUrl);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                var detail = JsonSerializer.Deserialize<ApiMoveDetail>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (detail == null) return null;

                string koName = detail.names.FirstOrDefault(n => n.language.name == "ko")?.name ?? detail.names.FirstOrDefault(n => n.language.name == "en")?.name ?? "알 수 없음";

                Enum.TryParse(detail.type.name, true, out PokemonType parsedType);

                SkillCategory parsedCategory = SkillCategory.Status;
                if (detail.damage_class.name == "physical") parsedCategory = SkillCategory.Physical;
                else if (detail.damage_class.name == "special") parsedCategory = SkillCategory.Special;

                SkillAilment parsedAilment = SkillAilment.None;
                int parsedAilmentChance = 0;

                if (detail.meta != null && !string.IsNullOrEmpty(detail.meta.ailment.name) && detail.meta.ailment.name != "none")
                {
                    parsedAilmentChance = detail.meta.ailment_chance;
                    if (parsedAilmentChance == 0) parsedAilmentChance = 100;

                    parsedAilment = detail.meta.ailment.name switch
                    {
                        "paralysis" => SkillAilment.Paralysis,
                        "sleep" => SkillAilment.Sleep,
                        "freeze" => SkillAilment.Freeze,
                        "burn" => SkillAilment.Burn,
                        "poison" => SkillAilment.Poison,
                        _ => SkillAilment.None
                    };
                }

                return new SkillInfo
                {
                    Id = detail.id,
                    Name = koName,
                    Type = parsedType,
                    Category = parsedCategory,
                    Power = detail.power ?? 0,
                    Accuracy = detail.accuracy ?? 0,
                    Ailment = parsedAilment,
                    AilmentChance = parsedAilmentChance
                };
            }
            catch
            {
                return null;
            }
        }
    }
}