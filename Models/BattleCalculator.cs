using System;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 배틀 수치 계산 (화면/대기 시간과 무관한 순수 계산). 내 포켓몬, 야생, 관장이 같은 공식을 씁니다.
    public static class BattleCalculator
    {
        public const double STAB_BONUS = 1.5;   // 자속 보정 (기술 타입 = 사용 포켓몬 타입)
        public const double CRIT_BONUS = 1.5;   // 급소 보정
        public const double GYM_BONUS = 1.1;    // 관장 포켓몬 보정
        public const int WILD_GENE = 15;        // 야생 포켓몬 유전자 고정값
        public const int GYM_GENE = 31;         // 관장 포켓몬 유전자 고정값

        // 능력치: ((종족값×2 + 유전자) × 레벨 / 100) + 5
        public static int Stat(int baseStat, int gene, int level) => ((baseStat * 2 + gene) * level / 100) + 5;

        // 최대 체력: ((종족값×2 + 유전자) × 레벨 / 100) + 레벨 + 10
        public static int MaxHp(int baseHp, int gene, int level) => ((baseHp * 2 + gene) * level / 100) + level + 10;

        // 공격 타입이 방어 포켓몬의 두 타입에 대해 갖는 상성 배율 (0 / 0.25 / 0.5 / 1 / 2 / 4)
        public static double TypeMultiplier(PokemonType moveType, PokemonInfo? defender)
        {
            PokemonType type1 = defender?.Type1 ?? PokemonType.Normal;
            PokemonType type2 = defender?.Type2 ?? PokemonType.None;
            return TypeMatchupHelper.GetMultiplier(moveType, type1) *
                   (type2 != PokemonType.None ? TypeMatchupHelper.GetMultiplier(moveType, type2) : 1.0);
        }

        public static double StabMultiplier(PokemonType moveType, PokemonInfo? attacker) =>
            attacker != null && (attacker.Type1 == moveType || attacker.Type2 == moveType) ? STAB_BONUS : 1.0;

        // 화상 보정: 화상 상태에서 쓰는 물리 기술의 데미지 절반 (원작 규칙)
        public static double BurnMultiplier(SkillCategory category, SkillAilment attackerAilment) =>
            category == SkillCategory.Physical && attackerAilment == SkillAilment.Burn ? 0.5 : 1.0;

        // 급소 판정: 단계 0은 1/24, 1은 1/8, 2는 1/2, 3 이상은 항상 (원작 규칙)
        public static bool RollCrit(SkillInfo skill, Random rand) => rand.Next(skill.CritRate switch { <= 0 => 24, 1 => 8, 2 => 2, _ => 1 }) == 0;

        // 연속 공격 횟수: 범위가 있으면 2회 35%, 3회 35%, 4회 15%, 5회 15% (원작 규칙)
        public static int RollHitCount(SkillInfo skill, Random rand)
        {
            if (skill.MaxHits <= 1) return 1;
            if (skill.MinHits >= skill.MaxHits) return skill.MaxHits;
            int roll = rand.Next(100);
            int extra = roll < 35 ? 0 : roll < 70 ? 1 : roll < 85 ? 2 : 3;
            return Math.Min(skill.MaxHits, skill.MinHits + extra);
        }

        // 데미지: max(2, 공격 - 방어/2) × 상성 × 자속 × 화상 × 급소 × 위력/50  (최소 1 보정은 호출하는 쪽에서)
        public static int RawDamage(int attack, int defense, double typeMultiplier, double stabMultiplier, double burnMultiplier, bool crit, int power) =>
            (int)(Math.Max(2, attack - (defense / 2)) * typeMultiplier * stabMultiplier * burnMultiplier * (crit ? CRIT_BONUS : 1.0) * (power / 50.0));

        // 급소와 상성 안내 문구 (메시지 앞에 붙임, 줄바꿈 포함)
        public static string HitText(double typeMultiplier, bool crit)
        {
            string effectiveness = EffectivenessText(typeMultiplier);
            if (!crit) return effectiveness;
            return effectiveness.Length > 0 ? $"{Loc.T("battle.crit")} {effectiveness}" : Loc.T("battle.crit") + "\n";
        }

        private static string EffectivenessText(double typeMultiplier)
        {
            if (typeMultiplier >= 2.0) return Loc.T("battle.eff.super");
            if (typeMultiplier > 0 && typeMultiplier <= 0.5) return Loc.T("battle.eff.notVery");
            return "";
        }
    }
}
