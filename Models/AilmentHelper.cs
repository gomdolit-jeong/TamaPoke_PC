using System;

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
                        message = $"{name}은(는) 잠에서 깨어났다!";
                        return true;
                    }
                    message = $"{name}은(는) 쿨쿨 잠들어 있다...";
                    return false;

                case SkillAilment.Freeze:
                    if (rand.Next(100) < 20) // 20% 확률로 얼음 해제
                    {
                        turnsLeft = 0;
                        cured = true;
                        message = $"{name}의 얼음이 녹았다!";
                        return true;
                    }
                    message = $"{name}은(는) 얼어붙어서 움직일 수 없다!";
                    return false;

                case SkillAilment.Paralysis:
                    if (rand.Next(100) < 25) // 25% 확률로 마비 행동 불가
                    {
                        message = $"{name}은(는) 몸이 저려서 움직일 수 없다!";
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
                    message = "화상 데미지를 입었다!";
                    break;

                case SkillAilment.Poison:
                    damage = Math.Max(1, maxHp / 8);
                    message = "독 데미지를 입었다!";
                    break;

                case SkillAilment.BadPoison:
                    int multiplier = Math.Max(1, ailmentTurnCount);
                    damage = Math.Max(1, (maxHp * multiplier) / 16);
                    message = "맹독의 데미지가 점점 커진다!";
                    break;
            }

            return damage;
        }

        // 4. 상태이상 아이콘 반환
        public static string GetAilmentIcon(SkillAilment ailment)
        {
            return ailment switch
            {
                SkillAilment.Burn => "🔥 화상",
                SkillAilment.Poison => "☠️ 독",
                SkillAilment.BadPoison => "💀 맹독",
                SkillAilment.Paralysis => "⚡ 마비",
                SkillAilment.Sleep => "💤 수면",
                SkillAilment.Freeze => "❄️ 얼음",
                _ => ""
            };
        }
    }
}