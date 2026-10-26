using System.Collections.Generic;
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

        // 능력치 변화 (예: 칼춤 = 사용자 공격 +2, 울음소리 = 상대 공격 -1, 깨물어부수기 = 20% 확률로 상대 방어 -1)
        public List<StatChange>? StatChanges { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatTarget StatTarget { get; set; } = StatTarget.Opponent;

        // 공격 기술의 능력치 변화 확률(%). 변화 기술은 항상 적용됩니다.
        public int StatChance { get; set; } = 0;

        [JsonIgnore] public bool HasStatChanges => StatChanges is { Count: > 0 };

        // 이 게임에서 효과를 낼 수 있는 기술인지: 데미지가 있거나, 지원하는 상태이상/능력치 변화가 있음
        // (일격필살, 카운터, 회복기, 고정 데미지 기술 등은 제외 → 배울 기술/상대 기술 후보에서 빠짐)
        [JsonIgnore] public bool IsSupported => Power > 0 || Ailment != SkillAilment.None || HasStatChanges;
    }

    // 배틀 중에만 유지되는 능력치 단계(-6 ~ +6)의 종류
    public enum BattleStat { Attack, Defense, SpAttack, SpDefense, Speed, Accuracy, Evasion }

    // 능력치 변화를 받는 쪽: 상대 / 기술을 쓴 포켓몬 자신
    public enum StatTarget { Opponent, User }

    public class StatChange
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BattleStat Stat { get; set; }
        public int Change { get; set; }   // +2: 크게 올라감, -1: 떨어짐
    }
}