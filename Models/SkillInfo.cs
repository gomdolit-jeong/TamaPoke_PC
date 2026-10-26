using System.Text.Json.Serialization;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public enum SkillCategory { Physical, Special, Status }

    // 🌟 API에서 가져올 모든 상태이상 종류를 한곳에 모아둡니다.
    public enum SkillAilment
    {
        None,
        Paralysis,  // 마비
        Sleep,      // 수면
        Freeze,     // 얼음
        Burn,       // 화상
        Poison,     // 일반 독
        BadPoison,  // 맹독
    }

    public class SkillInfo
    {
        public int Id { get; set; }
        // 이름: 한국어(기존 JSON의 "Name" 필드)와 영어. 화면에는 현재 언어 이름을 씁니다.
        [JsonPropertyName("Name")] public string NameKo { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        [JsonIgnore] public string Name => Loc.Instance.IsEnglish && !string.IsNullOrEmpty(NameEn) ? NameEn : NameKo;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PokemonType Type { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SkillCategory Category { get; set; }

        public int Power { get; set; }
        public int Accuracy { get; set; }

        // 🌟 스킬이 가진 상태이상 정보 저장 공간
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SkillAilment Ailment { get; set; } = SkillAilment.None;

        // 🌟 상태이상이 터질 확률 (예: 10, 30, 100)
        public int AilmentChance { get; set; } = 0;
    }
}