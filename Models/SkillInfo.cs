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

        public int Priority { get; set; } = 0;     // 우선도: 높은 쪽이 스피드와 상관없이 먼저 (전광석화 +1)
        public int MinHits { get; set; } = 0;      // 연속 공격 횟수 (연속뺨치기 2~5). 0이면 한 번
        public int MaxHits { get; set; } = 0;
        public int Drain { get; set; } = 0;        // 입힌 데미지 중 %: 양수는 흡수, 음수는 반동
        public int CritRate { get; set; } = 0;     // 급소 단계 (0: 1/24, 1: 1/8, 2: 1/2, 3 이상: 항상)
        public int FlinchChance { get; set; } = 0; // 풀죽음 확률(%)

        // 조건이 맞아야 성공하는 기술 (원작 규칙)
        [JsonIgnore] public bool IsFirstTurnOnly => Id is 252 or 660;       // 속이기, 만나자마자: 처음 쓸 때만
        [JsonIgnore] public bool NeedsTargetAsleep => Id == 138;            // 꿈먹기: 상대가 잠들어 있을 때만
        [JsonIgnore] public bool NeedsUserAsleep => Id == 173;              // 코골기: 내가 잠들어 있을 때만 (잠든 채로 쓸 수 있음)
        [JsonIgnore] public bool FailsIfHitFirst => Id == 264;              // 힘껏펀치: 그 턴에 맞으면 실패
        [JsonIgnore] public bool NeedsPhysicalHitFirst => Id == 704;        // 트랩셸: 그 턴에 물리 공격을 맞아야 성공
        [JsonIgnore] public bool NeedsTargetAttacking => Id is 389 or 909;  // 기습, 질풍신뢰: 상대가 공격 기술을 고른 턴에만

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