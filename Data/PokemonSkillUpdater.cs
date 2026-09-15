using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TamaPoke.Models; // SkillDex 모델에 접근하기 위함

namespace TamaPoke.Utils
{
    public class PokemonSkillUpdater
    {
        // 네트워크 연결을 효율적으로 재사용하기 위해 HttpClient를 static으로 하나만 둡니다.
        private static readonly HttpClient client = new HttpClient();

        // PokéAPI 응답 파싱용 내부 클래스들
        private class PokeApiResult { public List<ApiMoveSlot> moves { get; set; } = new(); }
        private class ApiMoveSlot { public ApiMove move { get; set; } = new(); public List<ApiVersionGroupDetail> version_group_details { get; set; } = new(); }
        private class ApiMove { public string name { get; set; } = ""; }
        private class ApiVersionGroupDetail { public int level_learned_at { get; set; } public ApiMethod move_learn_method { get; set; } = new(); public ApiVersionGroup version_group { get; set; } = new(); }
        private class ApiMethod { public string name { get; set; } = ""; }
        private class ApiVersionGroup { public string name { get; set; } = ""; }

        // 영문 스킬 이름 -> 유저님의 커스텀 ID 매핑 딕셔너리 (90개 스킬)
        private static readonly Dictionary<string, int> EnglishToCustomIdMap = new()
        {
            { "tackle", 1 }, { "scratch", 2 }, { "pound", 3 }, { "fury-attack", 4 }, { "quick-attack", 5 },
            { "swift", 6 }, { "body-slam", 7 }, { "double-edge", 8 }, { "hyper-beam", 9 }, { "peck", 10 },
            { "ember", 11 }, { "fire-punch", 12 }, { "flamethrower", 13 }, { "fire-blast", 14 }, { "bubble", 15 },
            { "water-gun", 16 }, { "waterfall", 17 }, { "surf", 18 }, { "hydro-pump", 19 }, { "spark", 20 },
            { "thundershock", 21 }, { "thunder-punch", 22 }, { "thunderbolt", 23 }, { "thunder", 24 }, { "absorb", 25 },
            { "vine-whip", 26 }, { "razor-leaf", 27 }, { "mega-drain", 28 }, { "solar-beam", 29 }, { "aurora-beam", 30 },
            { "ice-punch", 31 }, { "ice-beam", 32 }, { "blizzard", 33 }, { "karate-chop", 34 }, { "seismic-toss", 35 },
            { "submission", 36 }, { "jump-kick", 37 }, { "poison-sting", 38 }, { "acid", 39 }, { "sludge", 40 },
            { "sludge-bomb", 41 }, { "bone-club", 42 }, { "dig", 43 }, { "earthquake", 44 }, { "wing-attack", 45 },
            { "drill-peck", 46 }, { "fly", 47 }, { "psywave", 48 }, { "confusion", 49 }, { "psybeam", 50 },
            { "psychic", 51 }, { "bug-bite", 52 }, { "pin-missile", 53 }, { "leech-life", 54 }, { "megahorn", 55 },
            { "bug-buzz", 56 }, { "x-scissor", 57 }, { "rock-smash", 58 }, { "rock-throw", 59 }, { "rock-slide", 60 },
            { "ancient-power", 61 }, { "lick", 62 }, { "night-shade", 63 }, { "shadow-ball", 64 }, { "dragon-rage", 65 },
            { "dragon-claw", 66 }, { "outrage", 67 }, { "bite", 68 }, { "crunch", 69 }, { "iron-head", 70 },
            { "flash-cannon", 71 }, { "dazzling-gleam", 72 }, { "play-rough", 73 }, { "moonblast", 74 }, { "swords-dance", 75 },
            { "agility", 76 }, { "barrier", 77 }, { "amnesia", 78 }, { "nasty-plot", 79 }, { "dragon-dance", 80 },
            { "bulk-up", 81 }, { "growl", 82 }, { "leer", 83 }, { "screech", 84 }, { "string-shot", 85 },
            { "recover", 86 }, { "soft-boiled", 87 }, { "struggle", 88 }, { "dark-pulse", 89 }
        };

        // 🌟 trayNotifier 액션을 추가하여 외부(UI)의 트레이 아이콘을 호출할 수 있게 만듭니다.
        public static async Task GenerateOfflineSkillJsonAsync(IProgress<string>? progress = null, Action<string, string>? trayNotifier = null)
        {
            var allLearnsets = new List<PokemonLearnset>();

            // 🌟 1. 작업 시작 시 시스템 트레이에 알림 띄우기
            trayNotifier?.Invoke("스킬 업데이트 시작", "서버에서 최신 스킬 데이터를 가져오는 중입니다...");
            progress?.Report("🚀 API에서 최신 스킬 데이터를 가져오기 시작합니다...");

            for (int id = 1; id <= 1025; id++)
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
                            if (EnglishToCustomIdMap.TryGetValue(m.move.name, out int customId))
                            {
                                var svLevelUpDetail = m.version_group_details.FirstOrDefault(v => v.move_learn_method.name == "level-up" && v.version_group.name == "scarlet-violet");
                                if (svLevelUpDetail == null) svLevelUpDetail = m.version_group_details.FirstOrDefault(v => v.move_learn_method.name == "level-up" && v.version_group.name == "sword-shield");

                                if (svLevelUpDetail != null)
                                {
                                    validMoves.Add(new LearnMove { Level = svLevelUpDetail.level_learned_at, MoveId = customId });
                                }
                            }
                        }

                        if (validMoves.Count > 0)
                        {
                            validMoves = validMoves.OrderBy(v => v.Level).ToList();
                            allLearnsets.Add(new PokemonLearnset { SpeciesId = id, Moves = validMoves });
                        }
                    }

                    if (id % 100 == 0)
                    {
                        progress?.Report($"[업데이트 중] 도감 번호 {id}/1025 파싱 완료...");
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"[에러] ID {id} 파싱 실패: {ex.Message}");
                }
            }

            // 🌟 2. 현재 실행 중인 프로그램 위치의 Data 폴더 경로를 명확하게 지정
            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath); // 폴더가 없으면 생성
            }

            string savePath = Path.Combine(dataFolderPath, "pokemon_skills.json");

            // 결과물을 JSON 파일로 저장
            string finalJson = JsonSerializer.Serialize(allLearnsets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(savePath, finalJson);

            // 메모리에 즉시 새 데이터 로드
            SkillDex.LoadSkillData();

            // 🌟 3. 작업 완료 시 시스템 트레이에 알림 띄우기
            progress?.Report("✨ 완료! pokemon_skills.json 파일이 성공적으로 생성/업데이트되었습니다.");
            trayNotifier?.Invoke("스킬 업데이트 완료", "모든 포켓몬의 최신 스킬 데이터가 Data 폴더에 적용되었습니다!");
        }
    }
}