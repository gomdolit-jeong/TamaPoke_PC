using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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

        // 🌟 스킬 상세 정보 파싱용 클래스 추가
        private class ApiMoveDetail
        {
            public int id { get; set; }
            public int? power { get; set; }
            public int? accuracy { get; set; }
            public ApiNamedResource type { get; set; } = new();
            public ApiNamedResource damage_class { get; set; } = new();
            public List<ApiName> names { get; set; } = new();

            // 🌟 상태이상 정보가 들어있는 meta 필드 추가
            public ApiMoveMeta? meta { get; set; }
        }

        // 🌟 meta 내부 파싱용 클래스
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
            var moveDatabase = new Dictionary<int, SkillInfo>(); // 🌟 수집한 스킬 상세 정보를 담을 사전

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
                        var validMoves = new List<LearnMove>();

                        foreach (var m in pokeData.moves)
                        {
                            var svLevelUpDetail = m.version_group_details.FirstOrDefault(v => v.move_learn_method.name == "level-up" && v.version_group.name == "scarlet-violet");
                            if (svLevelUpDetail == null) svLevelUpDetail = m.version_group_details.FirstOrDefault(v => v.move_learn_method.name == "level-up" && v.version_group.name == "sword-shield");

                            if (svLevelUpDetail != null)
                            {
                                // 🌟 1. 기술의 ID를 URL에서 추출합니다.
                                int moveId = int.Parse(m.move.url.TrimEnd('/').Split('/').Last());
                                validMoves.Add(new LearnMove { Level = svLevelUpDetail.level_learned_at, MoveId = moveId });

                                // 🌟 2. 처음 보는 기술이라면 PokéAPI에서 상세 정보를 다운로드합니다!
                                if (!moveDatabase.ContainsKey(moveId))
                                {
                                    progress?.Report($"새로운 스킬 발견! 상세 정보 다운로드 중... [{m.move.name}]");
                                    var skillInfo = await FetchMoveDetailAsync(m.move.url);
                                    if (skillInfo != null) moveDatabase[moveId] = skillInfo;
                                }
                            }
                        }

                        if (validMoves.Count > 0)
                        {
                            validMoves = validMoves.OrderBy(v => v.Level).ToList();
                            allLearnsets.Add(new PokemonLearnset { SpeciesId = id, Moves = validMoves });
                        }
                    }

                    progress?.Report($"도감 번호 {id}/{TamaPoke.Models.GameConstants.MAX_POKEMON_ID} 스킬 데이터 처리 완료...");
                }
                catch (Exception) { /* 무시 */ }
            }

            // 파일 저장 로직
            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolderPath)) Directory.CreateDirectory(dataFolderPath);

            // 1. 레벨업 데이터 저장
            string learnsetPath = Path.Combine(dataFolderPath, "pokemon_skills.json");
            File.WriteAllText(learnsetPath, JsonSerializer.Serialize(allLearnsets, new JsonSerializerOptions { WriteIndented = true }));

            // 2. 🌟 스킬 도감(사전) 데이터 저장
            string movedbPath = Path.Combine(dataFolderPath, "pokemon_movedb.json");
            File.WriteAllText(movedbPath, JsonSerializer.Serialize(moveDatabase.Values.ToList(), new JsonSerializerOptions { WriteIndented = true }));

            SkillDex.LoadSkillData();
            progress?.Report("✨ 포켓몬 스킬 트리 및 전체 스킬 도감 DB 구축 완료!");
        }

        // PokéAPI에서 기술의 상세 정보(위력, 명중률, 한글 이름)를 가져오는 헬퍼 메서드
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

                // 🌟 API의 영문 상태이상을 우리 게임의 Enum으로 완벽하게 자동 번역합니다!
                SkillAilment parsedAilment = SkillAilment.None;
                int parsedAilmentChance = 0;

                if (detail.meta != null && !string.IsNullOrEmpty(detail.meta.ailment.name) && detail.meta.ailment.name != "none")
                {
                    parsedAilmentChance = detail.meta.ailment_chance;
                    // 확률이 0으로 오면 기본 100% 효과라는 뜻입니다.
                    if (parsedAilmentChance == 0) parsedAilmentChance = 100;

                    parsedAilment = detail.meta.ailment.name switch
                    {
                        "paralysis" => SkillAilment.Paralysis,
                        "sleep" => SkillAilment.Sleep,
                        "freeze" => SkillAilment.Freeze,
                        "burn" => SkillAilment.Burn,
                        "poison" => SkillAilment.Poison, // API의 poison은 일반 독으로 매핑합니다.
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
                    Ailment = parsedAilment,            // 🌟 상태이상 저장!
                    AilmentChance = parsedAilmentChance // 🌟 확률 저장!
                };
            }
            catch
            {
                return null;
            }
        }
    }
}