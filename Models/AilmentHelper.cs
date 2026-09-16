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
                SkillAilment.Sleep => rand.Next(1, 4), // 수면: 1~3턴 지속
                _ => 999 // 화상, 마비, 독, 얼음은 영구 유지
            };
        }

        // 2. 턴 시작 시 행동 가능 여부 검사
        public static bool CanActThisTurn(SkillAilment ailment, ref int turnsLeft, string name, out string message)
        {
            message = "";

            switch (ailment)
            {
                case SkillAilment.Sleep:
                    if (turnsLeft <= 0)
                    {
                        message = $"{name}은(는) 잠에서 깨어났다!";
                        return true;
                    }
                    message = $"{name}은(는) 쿨쿨 잠들어 있다...";
                    return false;

                case SkillAilment.Freeze:
                    if (rand.Next(100) < 20) // 20% 확률로 얼음 해제
                    {
                        turnsLeft = 0;
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