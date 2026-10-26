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
            public List<ApiStatChange> stat_changes { get; set; } = new();
            public ApiNamedResource target { get; set; } = new();
        }

        private class ApiMoveMeta
        {
            public ApiNamedResource ailment { get; set; } = new();
            public int ailment_chance { get; set; }
            public ApiNamedResource category { get; set; } = new();
            public int stat_chance { get; set; }
        }

        private class ApiStatChange { public int change { get; set; } public ApiNamedResource stat { get; set; } = new(); }

        private static readonly Dictionary<string, BattleStat> ApiStats = new()
        {
            ["attack"] = BattleStat.Attack, ["defense"] = BattleStat.Defense,
            ["special-attack"] = BattleStat.SpAttack, ["special-defense"] = BattleStat.SpDefense,
            ["speed"] = BattleStat.Speed, ["accuracy"] = BattleStat.Accuracy, ["evasion"] = BattleStat.Evasion,
        };

        // 능력치 변화는 세 종류만 지원합니다: 공격 후 내 능력치(damage-raise), 공격 후 상대 능력치(damage-lower), 변화 기술(net-good-stats)
        // (뻐기기처럼 혼란과 함께 상대 능력치를 올리는 기술 등은 제외)
        private static (List<StatChange>? Changes, StatTarget Target, int Chance) ParseStatChanges(ApiMoveDetail detail)
        {
            string category = detail.meta?.category.name ?? "";
            if (category is not ("net-good-stats" or "damage-raise" or "damage-lower")) return (null, StatTarget.Opponent, 0);

            var changes = detail.stat_changes.Where(c => ApiStats.ContainsKey(c.stat.name))
                .Select(c => new StatChange { Stat = ApiStats[c.stat.name], Change = c.change }).ToList();
            if (changes.Count == 0) return (null, StatTarget.Opponent, 0);

            StatTarget target = category switch
            {
                "damage-raise" => StatTarget.User,
                "damage-lower" => StatTarget.Opponent,
                _ => detail.target.name is "user" or "user-and-allies" or "users-field" or "user-or-ally" ? StatTarget.User : StatTarget.Opponent,
            };
            int chance = category == "net-good-stats" || detail.meta!.stat_chance == 0 ? 100 : detail.meta.stat_chance;
            return (changes, target, chance);
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
                                            progress?.Report(Loc.F("update.skill.newMove", m.move.name));
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

                    progress?.Report(Loc.F("update.skill.progress", id, TamaPoke.Models.GameConstants.MAX_POKEMON_ID));
                }
                catch (Exception) { /* 무시 */ }
            }

            // 네트워크 오류 등으로 받아온 데이터가 기존보다 적으면 덮어쓰지 않습니다.
            if (allLearnsets.Count == 0 || allLearnsets.Count < SkillDex.PokemonLearnsets.Count || moveDatabase.Count < SkillDex.MoveDatabase.Count)
            {
                progress?.Report(Loc.F("update.keepExisting", allLearnsets.Count, SkillDex.PokemonLearnsets.Count));
                return;
            }

            string dataFolderPath = AppPaths.DataFolder;
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
            progress?.Report(Loc.T("update.skill.done"));
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

                string? enName = detail.names.FirstOrDefault(n => n.language.name == "en")?.name;
                string koName = detail.names.FirstOrDefault(n => n.language.name == "ko")?.name ?? enName ?? $"#{detail.id}";

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

                var (statChanges, statTarget, statChance) = ParseStatChanges(detail);

                return new SkillInfo
                {
                    Id = detail.id,
                    NameKo = koName,
                    NameEn = enName,
                    Type = parsedType,
                    Category = parsedCategory,
                    Power = detail.power ?? 0,
                    Accuracy = detail.accuracy ?? 0,
                    Ailment = parsedAilment,
                    AilmentChance = parsedAilmentChance,
                    StatChanges = statChanges,
                    StatTarget = statTarget,
                    StatChance = statChance
                };
            }
            catch
            {
                return null;
            }
        }
    }
}