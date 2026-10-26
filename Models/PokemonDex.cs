using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public enum PokemonType
    {
        None, Normal, Fire, Water, Electric, Grass, Ice, Fighting,
        Poison, Ground, Flying, Psychic, Bug, Rock, Ghost, Dragon,
        Dark, Steel, Fairy
    }

    public class PokemonInfo
    {
        public int Id { get; set; }
        // 이름: 한국어(기존 JSON의 "DisplayName" 필드)와 영어. 화면에는 현재 언어 이름을 씁니다.
        [JsonPropertyName("DisplayName")] public string NameKo { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        [JsonIgnore] public string DisplayName => Loc.Instance.IsEnglish && !string.IsNullOrEmpty(NameEn) ? NameEn : NameKo;
        public PokemonType Type1 { get; set; }
        public PokemonType Type2 { get; set; }
        public int BaseHp { get; set; }
        public int BaseAtk { get; set; }
        public int BaseDef { get; set; }
        public int BaseSpeed { get; set; }
        public int BaseSpA { get; set; }
        public int BaseSpD { get; set; }
        public int EvolveTo { get; set; }
        public int EvolveLevel { get; set; }
        public int GenderRate { get; set; } // -1: 무성, 0~8: 암컷 비율(8분율)
    }

    public static class PokemonDex
    {
        public static List<PokemonInfo> AllPokemons { get; private set; } = new List<PokemonInfo>();

        // 도감 번호로 빠르게 찾기 위한 색인 (LoadPokemonData에서 갱신)
        private static Dictionary<int, PokemonInfo> _byId = new();

        // 포켓몬 데이터를 정상적으로 읽었는지 (파일이 없거나 깨졌으면 false)
        public static bool IsLoaded => AllPokemons.Count > 0;

        public static void LoadPokemonData()
        {
            try
            {
                string filePath = AppPaths.DataPath("pokemon_data.json");

                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);

                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    };

                    AllPokemons = JsonSerializer.Deserialize<List<PokemonInfo>>(jsonString, options) ?? new List<PokemonInfo>();
                    _byId = AllPokemons.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
                }
                else
                {
                    Console.WriteLine($"[에러] 포켓몬 데이터 파일을 찾을 수 없습니다: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에러] 데이터 로드 중 예외 발생: {ex.Message}");
            }
        }

        public static PokemonInfo? Get(int speciesId) => _byId.TryGetValue(speciesId, out var info) ? info : null;

        public static string GetName(int speciesId)
        {
            if (speciesId < 0) return Loc.T("pokemon.egg");
            return Get(speciesId)?.DisplayName ?? $"MON #{speciesId:D4}";
        }

        // 종족의 성비에 따라 성별을 정합니다. (무성 종족은 IsGenderless = true)
        public static (bool IsGenderless, bool IsFemale) RollGender(int speciesId, Random rand)
        {
            int rate = Get(speciesId)?.GenderRate ?? -1;
            if (rate == -1) return (true, false);
            if (rate == 0) return (false, false);  // 100% 수컷
            if (rate == 8) return (false, true);   // 100% 암컷
            return (false, rand.Next(8) < rate);
        }
    }
}
