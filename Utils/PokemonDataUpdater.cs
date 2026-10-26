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

        private class ApiPokemon { public List<ApiStat> stats { get; set; } = new(); public List<ApiTypeSlot> types { get; set; } = new(); public ApiNamedResource species { get; set; } = new(); }
        private class ApiPokemonList { public List<ApiNamedResource> results { get; set; } = new(); }
        private class ApiItemCategory { public List<ApiNamedResource> items { get; set; } = new(); }
        private class ApiItem { public List<ApiName> names { get; set; } = new(); }
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

            progress?.Report(Loc.T("update.data.evolution"));
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
                            NameKo = GetName(speciesInfo, "ko") ?? GetName(speciesInfo, "en") ?? $"#{id}",
                            NameEn = GetName(speciesInfo, "en"),
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

                    progress?.Report(Loc.F("update.data.progress", id, GameConstants.MAX_POKEMON_ID));
                }
                catch (Exception) { }
            }

            // 네트워크 오류 등으로 받아온 데이터가 기존보다 적으면 덮어쓰지 않습니다.
            if (allPokemonData.Count == 0 || allPokemonData.Count < PokemonDex.AllPokemons.Count)
            {
                progress?.Report(Loc.F("update.keepExisting", allPokemonData.Count, PokemonDex.AllPokemons.Count));
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

            string savePath = Path.Combine(dataFolderPath, "pokemon_data.json");
            string finalJson = JsonSerializer.Serialize(allPokemonData, jsonOptions);
            File.WriteAllText(savePath, finalJson);

            PokemonDex.LoadPokemonData();
            progress?.Report(Loc.T("update.data.done"));

            await GenerateMegaFormsJsonAsync(progress);
        }

        // PokeAPI 폼 이름 중 이미지 파일 접미사가 규칙(charizard-mega-x → Mega_X)과 다른 것. null이면 제외
        private static readonly Dictionary<string, string?> MegaFormOverrides = new()
        {
            ["meowstic-male-mega"] = "Mega",
            ["meowstic-female-mega"] = null,   // 암컷은 Mega 폼의 _Female 이미지로 표시
            ["tatsugiri-curly-mega"] = "Mega",
        };

        // 메가진화/원시회귀 폼 목록을 받아 Data/mega_forms.json에 저장합니다.
        public static async Task GenerateMegaFormsJsonAsync(IProgress<string>? progress = null)
        {
            progress?.Report(Loc.T("update.mega.start"));
            var forms = new List<MegaFormInfo>();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var stones = await FetchMegaStonesAsync(jsonOptions);

                var listJson = await client.GetStringAsync("https://pokeapi.co/api/v2/pokemon?limit=3000");
                var names = (JsonSerializer.Deserialize<ApiPokemonList>(listJson, jsonOptions)?.results ?? new())
                    .Select(r => r.name)
                    .Where(n => n.Contains("-mega") || n.EndsWith("-primal"))
                    .ToList();

                foreach (string apiName in names)
                {
                    try
                    {
                        var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon/{apiName}");
                        if (!response.IsSuccessStatusCode) continue;
                        var info = JsonSerializer.Deserialize<ApiPokemon>(await response.Content.ReadAsStringAsync(), jsonOptions);
                        if (info == null) continue;

                        string? form = MegaFormOverrides.TryGetValue(apiName, out var custom) ? custom : ToSpriteSuffix(apiName, info.species.name);
                        if (form == null) continue;

                        string stoneId = MatchStone(info.species.name, form, stones.Keys) ?? string.Empty;
                        stones.TryGetValue(stoneId, out var stoneNames);

                        forms.Add(new MegaFormInfo
                        {
                            SpeciesId = ExtractIdFromUrl(info.species.url),
                            Form = form,
                            ApiName = apiName,
                            Type1 = MapTypeToEnum(info.types.FirstOrDefault(t => t.slot == 1)?.type.name),
                            Type2 = MapTypeToEnum(info.types.FirstOrDefault(t => t.slot == 2)?.type.name),
                            BaseHp = GetStat(info, "hp"),
                            BaseAtk = GetStat(info, "attack"),
                            BaseDef = GetStat(info, "defense"),
                            BaseSpeed = GetStat(info, "speed"),
                            BaseSpA = GetStat(info, "special-attack"),
                            BaseSpD = GetStat(info, "special-defense"),
                            StoneId = stoneId,
                            StoneNameKo = stoneNames.Ko,
                            StoneNameEn = stoneNames.En,
                        });
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }

            // 네트워크 오류 등으로 받아온 목록이 기존보다 적으면 덮어쓰지 않습니다.
            if (forms.Count == 0 || forms.Count < MegaDex.AllForms.Count)
            {
                progress?.Report(Loc.F("update.keepExisting", forms.Count, MegaDex.AllForms.Count));
                return;
            }

            var saveOptions = new JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
            File.WriteAllText(Path.Combine(AppPaths.DataFolder, "mega_forms.json"), JsonSerializer.Serialize(forms.OrderBy(f => f.SpeciesId).ThenBy(f => f.Form).ToList(), saveOptions));

            MegaDex.LoadMegaData();
            progress?.Report(Loc.F("update.mega.done", forms.Count));
        }

        // 메가스톤 목록(PokeAPI 아이템 이름 → 한/영 이름). 원시회귀용 쪽빛구슬/주홍구슬도 포함
        private static async Task<Dictionary<string, (string? Ko, string? En)>> FetchMegaStonesAsync(JsonSerializerOptions jsonOptions)
        {
            var stones = new Dictionary<string, (string? Ko, string? En)>();
            var category = JsonSerializer.Deserialize<ApiItemCategory>(await client.GetStringAsync("https://pokeapi.co/api/v2/item-category/mega-stones"), jsonOptions);
            var ids = (category?.items ?? new()).Select(i => i.name).Concat(new[] { "blue-orb", "red-orb" });

            foreach (string id in ids)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<ApiItem>(await client.GetStringAsync($"https://pokeapi.co/api/v2/item/{id}"), jsonOptions);
                    stones[id] = (item?.names.FirstOrDefault(n => n.language.name == "ko")?.name, item?.names.FirstOrDefault(n => n.language.name == "en")?.name);
                }
                catch (Exception) { }
            }
            return stones;
        }

        // 메가 폼에 맞는 스톤: X/Y/Z가 같고 종 이름과 앞부분이 가장 많이 겹치는 스톤 (charizard + Mega_X → charizardite-x)
        // 원시회귀는 가이오가=쪽빛구슬, 그란돈=주홍구슬. 맞는 스톤이 없으면 null (레쿠쟈)
        private static string? MatchStone(string speciesName, string form, IEnumerable<string> stoneIds)
        {
            if (form.StartsWith("Primal")) return speciesName switch { "kyogre" => "blue-orb", "groudon" => "red-orb", _ => null };

            string letter = form.Split('_').SkipWhile(t => t != "Mega").Skip(1).FirstOrDefault()?.ToLowerInvariant() ?? "";
            string? best = null;
            int bestScore = 0;
            foreach (string id in stoneIds)
            {
                if (id.EndsWith("-orb")) continue;
                bool hasLetter = id.Length > 2 && id[^2] == '-';
                if ((hasLetter ? id[^1].ToString() : "") != letter) continue;

                string stoneBase = hasLetter ? id[..^2] : id;
                int score = 0;
                while (score < Math.Min(stoneBase.Length, speciesName.Length) && stoneBase[score] == speciesName[score]) score++;
                if (score > bestScore) { best = id; bestScore = score; }
            }
            return bestScore >= 4 ? best : null;
        }

        // charizard-mega-x → Mega_X, kyogre-primal → Primal, tatsugiri-stretchy-mega → Stretchy_Mega
        private static string ToSpriteSuffix(string apiName, string speciesName)
        {
            string rest = apiName.StartsWith(speciesName + "-") ? apiName.Substring(speciesName.Length + 1) : apiName;
            return string.Join("_", rest.Split('-').Select(t => char.ToUpperInvariant(t[0]) + t.Substring(1)));
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
                        progress?.Report(Loc.F("update.data.chain", chainId, GameConstants.MAX_EVOLUTION_CHAIN_ID));
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

        private static string? GetName(ApiSpecies species, string language) => species.names.FirstOrDefault(n => n.language.name == language)?.name;

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