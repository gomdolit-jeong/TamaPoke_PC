using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // 🌟 FirstOrDefault 사용을 위해 추가
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public string DisplayName { get; set; } = string.Empty;
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
    }

    public static class PokemonDex
    {
        public static List<PokemonInfo> AllPokemons { get; private set; } = new List<PokemonInfo>();

        public static void LoadPokemonData()
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "pokemon_data.json");

                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);

                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    };

                    AllPokemons = JsonSerializer.Deserialize<List<PokemonInfo>>(jsonString, options) ?? new List<PokemonInfo>();
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

        // 🌟 누락되었던 GetName 헬퍼 메서드 추가
        public static string GetName(int speciesId)
        {
            if (speciesId < 0) return "알";
            var poke = AllPokemons.FirstOrDefault(p => p.Id == speciesId);
            return poke?.DisplayName ?? $"MON #{speciesId:D4}";
        }
    }
}