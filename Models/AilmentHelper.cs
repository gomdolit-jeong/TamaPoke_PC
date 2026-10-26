using System;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public static class AilmentHelper
    {
        private static readonly Random rand = new Random();

        // 1. 상태이상 초기 지속 턴 수 결정
        public static int GetInitialTurns(SkillAilment ailment)
        {
            return ailment switch
            {
                SkillAilment.Sleep => rand.Next(2, 5), // 수면: 1~3턴 행동 불가 (턴 시작 시 1 감소 후 0이면 깨어남)
                _ => 999 // 화상, 마비, 독, 얼음은 영구 유지
            };
        }

        // 타입에 따른 상태이상 면역 (불꽃-화상, 얼음-얼음, 전기-마비, 독/강철-독)
        public static bool IsImmune(SkillAilment ailment, PokemonType type1, PokemonType type2)
        {
            bool Has(PokemonType t) => type1 == t || type2 == t;
            return ailment switch
            {
                SkillAilment.Burn => Has(PokemonType.Fire),
                SkillAilment.Freeze => Has(PokemonType.Ice),
                SkillAilment.Paralysis => Has(PokemonType.Electric),
                SkillAilment.Poison or SkillAilment.BadPoison => Has(PokemonType.Poison) || Has(PokemonType.Steel),
                _ => false
            };
        }

        // 2. 턴 시작 시 행동 가능 여부 검사
        // cured가 true면 상태이상이 풀린 것이므로 호출한 쪽에서 상태이상을 None으로 바꿔야 합니다.
        public static bool CanActThisTurn(SkillAilment ailment, ref int turnsLeft, string name, out string message, out bool cured)
        {
            message = "";
            cured = false;

            switch (ailment)
            {
                case SkillAilment.Sleep:
                    if (turnsLeft <= 0)
                    {
                        cured = true;
                        message = Loc.F("ailment.wake", name);
                        return true;
                    }
                    message = Loc.F("ailment.asleep", name);
                    return false;

                case SkillAilment.Freeze:
                    if (rand.Next(100) < 20) // 20% 확률로 얼음 해제
                    {
                        turnsLeft = 0;
                        cured = true;
                        message = Loc.F("ailment.thaw", name);
                        return true;
                    }
                    message = Loc.F("ailment.frozen", name);
                    return false;

                case SkillAilment.Paralysis:
                    if (rand.Next(100) < 25) // 25% 확률로 마비 행동 불가
                    {
                        message = Loc.F("ailment.paralyzed", name);
                        return false;
                    }
                    return true;

                default:
                    return true;
            }
        }

        // 3. 턴 종료 시 지속 데미지(틱뎀) 계산
        public static int GetTurnEndDamage(SkillAilment ailment, int maxHp, int ailmentTurnCount, out string message)
        {
            message = "";
            int damage = 0;

            switch (ailment)
            {
                case SkillAilment.Burn:
                    damage = Math.Max(1, maxHp / 16);
                    message = Loc.T("ailment.tick.burn");
                    break;

                case SkillAilment.Poison:
                    damage = Math.Max(1, maxHp / 8);
                    message = Loc.T("ailment.tick.poison");
                    break;

                case SkillAilment.BadPoison:
                    int multiplier = Math.Max(1, ailmentTurnCount);
                    damage = Math.Max(1, (maxHp * multiplier) / 16);
                    message = Loc.T("ailment.tick.badPoison");
                    break;
            }

            return damage;
        }

        // 4. 상태이상 아이콘 반환
        public static string GetAilmentIcon(SkillAilment ailment)
        {
            return ailment switch
            {
                SkillAilment.Burn => Loc.T("ailment.name.burn"),
                SkillAilment.Poison => Loc.T("ailment.name.poison"),
                SkillAilment.BadPoison => Loc.T("ailment.name.badPoison"),
                SkillAilment.Paralysis => Loc.T("ailment.name.paralysis"),
                SkillAilment.Sleep => Loc.T("ailment.name.sleep"),
                SkillAilment.Freeze => Loc.T("ailment.name.freeze"),
                _ => ""
            };
        }
    }
}