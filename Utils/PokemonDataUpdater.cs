using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TamaPoke.Models; // PokemonInfo 및 PokemonType을 사용하기 위함

namespace TamaPoke.Utils
{
    public class PokemonDataUpdater
    {
        // 네트워크 연결을 효율적으로 재사용하기 위해 HttpClient를 static으로 하나만 둡니다.
        private static readonly HttpClient client = new HttpClient();

        // PokéAPI 응답 파싱용 내부 클래스들
        private class ApiPokemon { public List<ApiStat> stats { get; set; } = new(); public List<ApiTypeSlot> types { get; set; } = new(); }
        private class ApiStat { public int base_stat { get; set; } public ApiNamedResource stat { get; set; } = new(); }
        private class ApiTypeSlot { public int slot { get; set; } public ApiNamedResource type { get; set; } = new(); }
        private class ApiNamedResource { public string name { get; set; } = ""; }

        private class ApiSpecies { public List<ApiName> names { get; set; } = new(); }
        private class ApiName { public string name { get; set; } = ""; public ApiNamedResource language { get; set; } = new(); }

        // ====================================================================
        // 🌟 1. 메인 오케스트레이션 함수 (전체 흐름 제어)
        // ====================================================================
        public static async Task GenerateOfflineDataJsonAsync(IProgress<string>? progress = null, Action<string, string>? trayNotifier = null)
        {
            var allPokemonData = new List<PokemonInfo>();

            trayNotifier?.Invoke("포켓몬 데이터 업데이트 시작", "서버에서 최신 기본 정보와 스탯을 가져오는 중입니다...");
            progress?.Report("🚀 API에서 최신 포켓몬 데이터를 가져오기 시작합니다...");

            for (int id = 1; id <= 1025; id++)
            {
                try
                {
                    // 🌟 기능별로 분리된 헬퍼 함수들을 호출합니다.
                    var basicInfo = await FetchPokemonBasicInfoAsync(id);
                    var speciesInfo = await FetchPokemonSpeciesInfoAsync(id);

                    if (basicInfo != null && speciesInfo != null)
                    {
                        var newPokemon = new PokemonInfo
                        {
                            Id = id,
                            DisplayName = GetKoreanName(speciesInfo) ?? $"포켓몬 {id}",
                            Type1 = MapTypeToEnum(basicInfo.types.FirstOrDefault(t => t.slot == 1)?.type.name),
                            Type2 = MapTypeToEnum(basicInfo.types.FirstOrDefault(t => t.slot == 2)?.type.name),
                            BaseHp = GetStat(basicInfo, "hp"),
                            BaseAtk = GetStat(basicInfo, "attack"),
                            BaseDef = GetStat(basicInfo, "defense"),
                            BaseSpA = GetStat(basicInfo, "special-attack"),
                            BaseSpD = GetStat(basicInfo, "special-defense"),
                            BaseSpeed = GetStat(basicInfo, "speed"),

                            // 진화 정보는 기존 PokemonState.DexTable을 사용하므로 기본값 0으로 유지합니다.
                            EvolveTo = 0,
                            EvolveLevel = 0
                        };

                        allPokemonData.Add(newPokemon);
                    }

                    if (id % 100 == 0)
                    {
                        progress?.Report($"[업데이트 중] 도감 번호 {id}/1025 포켓몬 스탯 파싱 완료...");
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"[에러] ID {id} 파싱 실패: {ex.Message}");
                }
            }

            // 🌟 파일 저장 헬퍼 함수 호출
            SaveToJson(allPokemonData);

            progress?.Report("✨ 완료! pokemon_data.json 파일이 성공적으로 생성/업데이트되었습니다.");
            trayNotifier?.Invoke("포켓몬 데이터 업데이트 완료", "모든 포켓몬의 최신 기본 스탯이 Data 폴더에 적용되었습니다!");

            // 메모리에 즉시 새 데이터 로드
            PokemonDex.LoadPokemonData();
        }

        // ====================================================================
        // 🌟 2. 기능별 헬퍼 함수들 (데이터 파싱 및 저장)
        // ====================================================================

        // 스탯과 타입 정보 가져오기
        private static async Task<ApiPokemon?> FetchPokemonBasicInfoAsync(int id)
        {
            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon/{id}");
            if (!response.IsSuccessStatusCode) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiPokemon>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 한국어 이름 정보 가져오기
        private static async Task<ApiSpecies?> FetchPokemonSpeciesInfoAsync(int id)
        {
            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon-species/{id}");
            if (!response.IsSuccessStatusCode) return null;
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiSpecies>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // 한국어 이름 추출
        private static string? GetKoreanName(ApiSpecies species)
        {
            return species.names.FirstOrDefault(n => n.language.name == "ko")?.name;
        }

        // 특정 스탯 수치 추출
        private static int GetStat(ApiPokemon pokemon, string statName)
        {
            return pokemon.stats.FirstOrDefault(s => s.stat.name == statName)?.base_stat ?? 50;
        }

        // API의 문자열 타입을 PokemonType Enum으로 완벽 변환
        private static PokemonType MapTypeToEnum(string? typeName)
        {
            return typeName?.ToLower() switch
            {
                "normal" => PokemonType.Normal,
                "fire" => PokemonType.Fire,
                "water" => PokemonType.Water,
                "electric" => PokemonType.Electric,
                "grass" => PokemonType.Grass,
                "ice" => PokemonType.Ice,
                "fighting" => PokemonType.Fighting,
                "poison" => PokemonType.Poison,
                "ground" => PokemonType.Ground,
                "flying" => PokemonType.Flying,
                "psychic" => PokemonType.Psychic,
                "bug" => PokemonType.Bug,
                "rock" => PokemonType.Rock,
                "ghost" => PokemonType.Ghost,
                "dragon" => PokemonType.Dragon,
                "dark" => PokemonType.Dark,
                "steel" => PokemonType.Steel,
                "fairy" => PokemonType.Fairy,
                _ => PokemonType.None
            };
        }

        // Data 폴더에 JSON으로 안전하게 저장
        private static void SaveToJson(List<PokemonInfo> data)
        {
            // 실행 경로 내의 Data 폴더를 지정합니다.
            string dataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
            }

            string savePath = Path.Combine(dataFolderPath, "pokemon_data.json");
            string finalJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(savePath, finalJson);
        }
    }
}