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
    public class PokemonDataUpdater
    {
        private static readonly HttpClient client = new HttpClient();

        private class ApiPokemon { public List<ApiStat> stats { get; set; } = new(); public List<ApiTypeSlot> types { get; set; } = new(); }
        private class ApiStat { public int base_stat { get; set; } public ApiNamedResource stat { get; set; } = new(); }
        private class ApiTypeSlot { public int slot { get; set; } public ApiNamedResource type { get; set; } = new(); }
        private class ApiNamedResource { public string name { get; set; } = ""; public string url { get; set; } = ""; }

        private class ApiSpecies
        {
            public int gender_rate { get; set; }
            public List<ApiName> names { get; set; } = new();
            public ApiNamedResource evolution_chain { get; set; } = new();
        }
        private class ApiName { public string name { get; set; } = ""; public ApiNamedResource language { get; set; } = new(); }

        private class ApiEvolutionChainResponse { public ApiChainLink chain { get; set; } = new(); }
        private class ApiChainLink
        {
            public ApiNamedResource species { get; set; } = new();
            public List<ApiEvolutionDetail> evolution_details { get; set; } = new();
            public List<ApiChainLink> evolves_to { get; set; } = new();
        }
        private class ApiEvolutionDetail { public int? min_level { get; set; } }

        public static async Task GenerateOfflineDataJsonAsync(IProgress<string>? progress = null, Action<string, string>? trayNotifier = null)
        {
            var allPokemonData = new List<PokemonInfo>();

            progress?.Report("진화 체인 데이터를 수집하는 중입니다... 잠시만 기다려주세요!");
            Dictionary<int, (int EvolveTo, int EvolveLevel)> evolutionMap = await FetchAllEvolutionMappingsAsync(progress);

            for (int id = 1; id <= GameConstants.MAX_POKEMON_ID; id++)
            {
                try
                {
                    var basicInfo = await FetchPokemonBasicInfoAsync(id);
                    var speciesInfo = await FetchPokemonSpeciesInfoAsync(id);

                    if (basicInfo != null && speciesInfo != null)
                    {
                        int evolveTo = 0;
                        int evolveLevel = 0;

                        if (evolutionMap.TryGetValue(id, out var evoInfo))
                        {
                            evolveTo = evoInfo.EvolveTo;
                            evolveLevel = evoInfo.EvolveLevel;
                        }

                        var newPokemon = new PokemonInfo
                        {
                            Id = id,
                            DisplayName = GetKoreanName(speciesInfo) ?? $"포켓몬 {id}",
                            Type1 = MapTypeToEnum(basicInfo.types.FirstOrDefault(t => t.slot == 1)?.type.name),
                            Type2 = MapTypeToEnum(basicInfo.types.FirstOrDefault(t => t.slot == 2)?.type.name),
                            BaseHp = GetStat(basicInfo, "hp"),
                            BaseAtk = GetStat(basicInfo, "attack"),
                            BaseDef = GetStat(basicInfo, "defense"),
                            BaseSpeed = GetStat(basicInfo, "speed"),
                            BaseSpA = GetStat(basicInfo, "special-attack"),
                            BaseSpD = GetStat(basicInfo, "special-defense"),
                            EvolveTo = evolveTo,
                            EvolveLevel = evolveLevel,
                            GenderRate = speciesInfo != null ? speciesInfo.gender_rate : -1
                        };

                        allPokemonData.Add(newPokemon);
                    }

                    progress?.Report($"도감 번호 {id}/{GameConstants.MAX_POKEMON_ID} 기본 데이터 처리 완료...");
                }
                catch (Exception) { }
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

            string savePath = Path.Combine(dataFolderPath, "pokemon_data.json");
            string finalJson = JsonSerializer.Serialize(allPokemonData, jsonOptions);
            File.WriteAllText(savePath, finalJson);

            PokemonDex.LoadPokemonData();
            progress?.Report("포켓몬 기본 데이터(pokemon_data.json) 진화 정보 포함 갱신 완료!");
        }

        private static async Task<Dictionary<int, (int, int)>> FetchAllEvolutionMappingsAsync(IProgress<string>? progress)
        {
            var map = new Dictionary<int, (int, int)>();

            for (int chainId = 1; chainId <= GameConstants.MAX_EVOLUTION_CHAIN_ID; chainId++)
            {
                try
                {
                    if (chainId % 20 == 0 || chainId == 1)
                    {
                        progress?.Report($"[진화 수집 중] 진화 체인 분석 중... ({chainId}/{GameConstants.MAX_EVOLUTION_CHAIN_ID})");
                    }

                    var response = await client.GetAsync($"https://pokeapi.co/api/v2/evolution-chain/{chainId}");
                    if (!response.IsSuccessStatusCode) continue;

                    string json = await response.Content.ReadAsStringAsync();
                    var chainData = JsonSerializer.Deserialize<ApiEvolutionChainResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (chainData?.chain != null)
                    {
                        ParseChainRecursively(chainData.chain, map);
                    }
                }
                catch { }
            }

            return map;
        }

        private static void ParseChainRecursively(ApiChainLink link, Dictionary<int, (int, int)> map)
        {
            int currentId = ExtractIdFromUrl(link.species.url);

            if (link.evolves_to != null && link.evolves_to.Count > 0)
            {
                var nextLink = link.evolves_to[0];
                int nextId = ExtractIdFromUrl(nextLink.species.url);

                int level = 36;
                if (nextLink.evolution_details != null && nextLink.evolution_details.Count > 0)
                {
                    level = nextLink.evolution_details[0].min_level ?? 36;
                }

                map[currentId] = (nextId, level);

                foreach (var next in link.evolves_to)
                {
                    ParseChainRecursively(next, map);
                }
            }
        }

        private static int ExtractIdFromUrl(string url)
        {
            var segments = url.TrimEnd('/').Split('/');
            if (int.TryParse(segments.Last(), out int id)) return id;
            return 0;
        }

        private static async Task<ApiPokemon?> FetchPokemonBasicInfoAsync(int id)
        {
            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<ApiPokemon>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static async Task<ApiSpecies?> FetchPokemonSpeciesInfoAsync(int id)
        {
            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon-species/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<ApiSpecies>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static string? GetKoreanName(ApiSpecies species) => species.names.FirstOrDefault(n => n.language.name == "ko")?.name;

        private static int GetStat(ApiPokemon pokemon, string statName) => pokemon.stats.FirstOrDefault(s => s.stat.name == statName)?.base_stat ?? 50;

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
    }
}