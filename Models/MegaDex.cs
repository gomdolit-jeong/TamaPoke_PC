using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 메가진화/원시회귀 폼 정보. 데이터 업데이트 때 PokeAPI에서 받아 Data/mega_forms.json에 저장합니다.
    public class MegaFormInfo
    {
        public int SpeciesId { get; set; }
        public string Form { get; set; } = string.Empty;     // 이미지 파일 접미사: Mega, Mega_X, Primal ... (p0006_Mega_X.bin)
        public string ApiName { get; set; } = string.Empty;  // PokeAPI 이름: charizard-mega-x
        public PokemonType Type1 { get; set; }
        public PokemonType Type2 { get; set; }
        public int BaseHp { get; set; }
        public int BaseAtk { get; set; }
        public int BaseDef { get; set; }
        public int BaseSpeed { get; set; }
        public int BaseSpA { get; set; }
        public int BaseSpD { get; set; }

        // 필요한 메가스톤 (PokeAPI 아이템 이름: charizardite-x, 원시회귀는 blue-orb/red-orb). 비어 있으면 스톤 없이 가능 (레쿠쟈)
        public string StoneId { get; set; } = string.Empty;
        public string? StoneNameKo { get; set; }
        public string? StoneNameEn { get; set; }

        [JsonIgnore] public string StoneName =>
            (Loc.Instance.IsEnglish ? StoneNameEn ?? StoneNameKo : StoneNameKo ?? StoneNameEn) ?? StoneId;

        [JsonIgnore] public string SpriteFileName => $"p{SpeciesId:D4}_{Form}.bin";
        [JsonIgnore] public bool IsPrimal => Form.StartsWith("Primal", StringComparison.OrdinalIgnoreCase);

        // 이미지가 있어야 실제로 메가진화할 수 있습니다. (이미지 업데이트로 새로 생기면 자동으로 가능해짐)
        [JsonIgnore] public bool HasSprite => File.Exists(AppPaths.SpritePath(SpriteFileName));

        // 표시 이름: 메가리자몽X / Mega Charizard X, 원시가이오가 / Primal Kyogre
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                string species = PokemonDex.GetName(SpeciesId);
                if (IsPrimal) return Loc.F("form.primal.name", species);
                string letter = Form.Split('_').SkipWhile(t => !t.Equals("Mega", StringComparison.OrdinalIgnoreCase)).Skip(1).FirstOrDefault() ?? "";
                return Loc.F("form.mega.name", species, letter).Trim();
            }
        }
    }

    public static class MegaDex
    {
        public static List<MegaFormInfo> AllForms { get; private set; } = new();

        public static void LoadMegaData()
        {
            try
            {
                string filePath = AppPaths.DataPath("mega_forms.json");
                if (!File.Exists(filePath)) return;

                var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true };
                AllForms = JsonSerializer.Deserialize<List<MegaFormInfo>>(File.ReadAllText(filePath), options) ?? new List<MegaFormInfo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[에러] 메가진화 데이터 로드 중 예외 발생: {ex.Message}");
            }
        }

        // 해당 포켓몬의 메가 폼 전체 (이미지 유무와 무관)
        public static List<MegaFormInfo> GetForms(int speciesId) => AllForms.Where(f => f.SpeciesId == speciesId).ToList();

        // 이미지까지 있어서 지금 메가진화할 수 있는 폼
        public static List<MegaFormInfo> GetAvailableForms(int speciesId) => AllForms.Where(f => f.SpeciesId == speciesId && f.HasSprite).ToList();

        // 조각을 모을 수 있는 메가스톤: 스톤마다 하나씩, 그 스톤으로 메가진화하는 폼의 이미지가 있는 것만
        // (마기아나/싸리용처럼 여러 폼이 한 스톤을 쓰면 이미지가 있는 첫 폼으로 대표)
        public static List<MegaFormInfo> CollectibleStones() =>
            AllForms.Where(f => !string.IsNullOrEmpty(f.StoneId) && f.HasSprite).GroupBy(f => f.StoneId).Select(g => g.First()).ToList();

        // 스톤 ID로 대표 폼(이름 표시용) 찾기
        public static MegaFormInfo? FindStone(string stoneId) => AllForms.FirstOrDefault(f => f.StoneId == stoneId);
    }
}
