using System;
using System.Linq;
using System.Threading.Tasks;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 배틀 능력치 단계와 선공 (Battle Stat Stages & Turn Order)
        // 능력치 단계: 배틀 중에만 유지되며 -6 ~ +6 (원작 규칙). 배틀을 시작할 때마다 0으로 초기화합니다.
        private const int MAX_STAT_STAGE = 6;
        private readonly int[] _playerStages = new int[Enum.GetValues<BattleStat>().Length];
        private readonly int[] _enemyStages = new int[Enum.GetValues<BattleStat>().Length];

        // 선공: 스피드가 높은 쪽이 먼저, 같으면 50:50. 느린 쪽도 이 확률(%)로 먼저 움직입니다. 마비면 스피드 절반.
        private const int SLOW_SIDE_FIRST_CHANCE = 20;

        // 타격 연출 시간(ms)
        private const int HIT_SHAKE_MS = 600, HIT_REST_MS = 900;
        private const int MULTI_HIT_SHAKE_MS = 400, MULTI_HIT_GAP_MS = 150;

        // 풀죽음: 먼저 움직인 쪽의 기술에 맞으면 그 턴에 행동하지 못합니다.
        private bool _playerFlinched, _enemyFlinched;
        // 이번 배틀에서 기술을 쓴 적이 있는지 (속이기처럼 처음에만 성공하는 기술용)
        private bool _playerHasMoved, _enemyHasMoved;

        private void ResetStatStages()
        {
            Array.Clear(_playerStages);
            Array.Clear(_enemyStages);
            _playerFlinched = _enemyFlinched = false;
            _playerHasMoved = _enemyHasMoved = false;
            ClearTurnState();
        }

        // 이번 턴에 고른 기술 (회피/아이템 등 기술을 안 쓴 턴은 null)과 이번 턴에 맞은 기술
        private SkillInfo? _playerChoice, _enemyChoice, _playerHitBy, _enemyHitBy;

        private void ClearTurnState() => _playerChoice = _enemyChoice = _playerHitBy = _enemyHitBy = null;

        // 조건이 맞지 않아 실패하는 기술인지 (쓴 쪽, 상대의 상태로 판단)
        private bool MoveConditionMet(SkillInfo skill, bool userIsPlayer, bool wasFirstMove)
        {
            SkillAilment userAilment = userIsPlayer ? PlayerAilment : EnemyAilment;
            SkillAilment targetAilment = userIsPlayer ? EnemyAilment : PlayerAilment;
            SkillInfo? targetChoice = userIsPlayer ? _enemyChoice : _playerChoice;
            SkillInfo? hitBy = userIsPlayer ? _playerHitBy : _enemyHitBy;

            if (skill.IsFirstTurnOnly && !wasFirstMove) return false;
            if (skill.NeedsTargetAsleep && targetAilment != SkillAilment.Sleep) return false;
            if (skill.NeedsUserAsleep && userAilment != SkillAilment.Sleep) return false;
            if (skill.NeedsPhysicalHitFirst && hitBy?.Category != SkillCategory.Physical) return false;
            if (skill.NeedsTargetAttacking && (targetChoice == null || targetChoice.Power <= 0)) return false;
            return true;
        }

        // 상대 AI: 지금 쓰면 반드시 실패하는 기술은 고르지 않습니다. (상대가 뭘 고를지는 모르므로 기습/트랩셸은 후보에 둡니다)
        private bool EnemyCanUseNow(SkillInfo skill) =>
            !(skill.IsFirstTurnOnly && _enemyHasMoved) &&
            !(skill.NeedsTargetAsleep && PlayerAilment != SkillAilment.Sleep) &&
            !(skill.NeedsUserAsleep && EnemyAilment != SkillAilment.Sleep);

        // 급소는 공격 쪽의 낮아진 단계와 방어 쪽의 높아진 단계를 무시합니다. (원작 규칙)
        private static double AttackStage(int[] stages, BattleStat stat, bool crit) =>
            StageMultiplier(crit ? Math.Max(0, stages[(int)stat]) : stages[(int)stat]);

        private static double DefenseStage(int[] stages, BattleStat stat, bool crit) =>
            StageMultiplier(crit ? Math.Min(0, stages[(int)stat]) : stages[(int)stat]);

        // 타격이 끝난 뒤의 효과: 연속 공격 횟수 안내 → 흡수/반동 → 상태이상 → 능력치 변화 → 풀죽음
        // hitCount: 연속 공격이면 맞힌 횟수, 아니면 0. dealt: 실제로 깎은 체력 합계
        private async Task FinishHitsAsync(SkillInfo skill, int hitCount, int dealt, bool userIsPlayer, Random rand)
        {
            if (hitCount > 0)
            {
                BattleMessage = Loc.F("battle.multiHit", hitCount);
                await Task.Delay(1000);
            }

            await ApplyDrainAsync(skill, dealt, userIsPlayer);
            await TryInflictAilmentAsync(skill, targetIsEnemy: userIsPlayer, isStatusMove: false);
            await ApplyStatChangesAsync(skill, userIsPlayer, isStatusMove: false);

            if (rand.Next(100) < skill.FlinchChance)
            {
                if (userIsPlayer) _enemyFlinched = true; else _playerFlinched = true;
            }
        }

        // 흡수(입힌 데미지의 %만큼 회복) / 반동(입힌 데미지의 %만큼 자신도 데미지)
        private async Task ApplyDrainAsync(SkillInfo skill, int dealt, bool userIsPlayer)
        {
            if (skill.Drain == 0 || dealt <= 0) return;

            int hp = userIsPlayer ? PlayerHp : EnemyHp;
            int maxHp = userIsPlayer ? PlayerMaxHp : EnemyMaxHp;
            string who = userIsPlayer ? PlayerBattleName : Loc.F("battle.wildName", EnemyName);
            int amount = Math.Max(1, dealt * Math.Abs(skill.Drain) / 100);

            if (skill.Drain > 0)
            {
                if (hp <= 0 || hp >= maxHp) return;
                amount = Math.Min(amount, maxHp - hp);
                if (userIsPlayer) PlayerHp = hp + amount; else EnemyHp = hp + amount;
                BattleMessage = Loc.F("battle.drained", who, amount);
                await Task.Delay(1300);
                return;
            }

            if (userIsPlayer) { IsPlayerTakingDamage = true; PlayerHp = Math.Max(0, hp - amount); }
            else { IsEnemyTakingDamage = true; EnemyHp = Math.Max(0, hp - amount); }
            BattleMessage = Loc.F("battle.recoil", who, amount);
            await Task.Delay(HIT_SHAKE_MS);
            IsPlayerTakingDamage = false; IsEnemyTakingDamage = false;
            await Task.Delay(HIT_REST_MS);
        }

        // 공격/방어/특공/특방/스피드: +n이면 (2+n)/2배, -n이면 2/(2+n)배
        private static double StageMultiplier(int stage) => stage >= 0 ? (2 + stage) / 2.0 : 2.0 / (2 - stage);

        // 명중/회피: +n이면 (3+n)/3배, -n이면 3/(3+n)배
        private static double AccuracyStageMultiplier(int stage) => stage >= 0 ? (3 + stage) / 3.0 : 3.0 / (3 - stage);

        private static double Stage(int[] stages, BattleStat stat) => StageMultiplier(stages[(int)stat]);

        // 명중 판정: 기술 명중률 × (쓰는 쪽 명중 단계 - 맞는 쪽 회피 단계). 명중률이 없는 기술과 자신에게 쓰는 기술은 항상 성공
        private static bool RollHit(SkillInfo skill, int[] attackerStages, int[] defenderStages, Random rand)
        {
            if (skill.Accuracy == 0 || IsSelfTargetingStatusMove(skill)) return true;
            int stage = Math.Clamp(attackerStages[(int)BattleStat.Accuracy] - defenderStages[(int)BattleStat.Evasion], -MAX_STAT_STAGE, MAX_STAT_STAGE);
            return rand.NextDouble() * 100 < skill.Accuracy * AccuracyStageMultiplier(stage);
        }

        // 칼춤, 고속이동처럼 자기 능력치만 올리는 변화 기술 (회피로 피할 수 없고 빗나가지 않음)
        private static bool IsSelfTargetingStatusMove(SkillInfo skill) =>
            skill.Power == 0 && skill.HasStatChanges && skill.StatTarget == StatTarget.User && skill.Ailment == SkillAilment.None;

        private double PlayerBattleSpeed =>
            CombatSpeed * Stage(_playerStages, BattleStat.Speed) * (PlayerAilment == SkillAilment.Paralysis ? 0.5 : 1.0);

        private double EnemyBattleSpeed
        {
            get
            {
                double speed = WildStat(PokemonDex.Get(EnemySpeciesId)?.BaseSpeed);
                if (IsGymBattle) speed *= BattleCalculator.GYM_BONUS;
                return speed * Stage(_enemyStages, BattleStat.Speed) * (EnemyAilment == SkillAilment.Paralysis ? 0.5 : 1.0);
            }
        }

        private bool DecidePlayerFirst(SkillInfo playerSkill, SkillInfo enemySkill, Random rand)
        {
            if (playerSkill.Priority != enemySkill.Priority) return playerSkill.Priority > enemySkill.Priority; // 우선도가 높으면 무조건 먼저
            double me = PlayerBattleSpeed, foe = EnemyBattleSpeed;
            if (me == foe) return rand.Next(2) == 0;
            bool slowerGoesFirst = rand.Next(100) < SLOW_SIDE_FIRST_CHANCE;
            return (me > foe) != slowerGoesFirst;
        }

        // 기술의 능력치 변화를 적용하고 메시지를 보여줍니다.
        // userIsPlayer: 기술을 쓴 쪽이 내 포켓몬인지. isStatusMove: 변화 기술이면 항상, 공격 기술이면 StatChance 확률로 적용
        private async Task ApplyStatChangesAsync(SkillInfo skill, bool userIsPlayer, bool isStatusMove)
        {
            if (!skill.HasStatChanges) return;
            if (!isStatusMove && new Random().Next(100) >= (skill.StatChance > 0 ? skill.StatChance : 100)) return;

            bool targetIsPlayer = (skill.StatTarget == StatTarget.User) == userIsPlayer;
            if (targetIsPlayer ? PlayerHp <= 0 : EnemyHp <= 0) return; // 쓰러진 쪽은 변하지 않음

            int[] stages = targetIsPlayer ? _playerStages : _enemyStages;
            string who = targetIsPlayer ? PlayerBattleName : Loc.F("battle.wildName", EnemyName);

            // 원시의힘처럼 여러 능력치가 한꺼번에 오르면 한 문장으로 묶어서 보여줍니다.
            var results = skill.StatChanges!.Select(c =>
            {
                int before = stages[(int)c.Stat];
                stages[(int)c.Stat] = Math.Clamp(before + c.Change, -MAX_STAT_STAGE, MAX_STAT_STAGE);
                string key = stages[(int)c.Stat] == before ? (c.Change > 0 ? "battle.stat.noHigher" : "battle.stat.noLower")
                           : c.Change >= 2 ? "battle.stat.sharplyRose"
                           : c.Change > 0 ? "battle.stat.rose"
                           : c.Change <= -2 ? "battle.stat.harshlyFell"
                           : "battle.stat.fell";
                return (Key: key, Stat: c.Stat);
            }).ToList();

            foreach (var group in results.GroupBy(r => r.Key))
            {
                string stats = string.Join(", ", group.Select(r => Loc.T($"battle.stat.{r.Stat}")));
                bool topic = group.Key is "battle.stat.noHigher" or "battle.stat.noLower";
                string josa = topic ? Loc.Josa(stats, "은", "는") : Loc.Josa(stats, "이", "가"); // 한국어 문구의 {2} (영어는 사용 안 함)
                BattleMessage = Loc.F(group.Key, who, stats, josa);
                if (!topic) await PlayStatFxAsync(rose: group.Key is "battle.stat.sharplyRose" or "battle.stat.rose", targetIsEnemy: !targetIsPlayer);
                await Task.Delay(1300);
            }
        }

        // 위력 0 변화 기술: 능력치 변화 / 상태이상 (둘 다 없는 기술은 "아무 일도 일어나지 않았다")
        private async Task UseStatusMoveAsync(SkillInfo skill, bool userIsPlayer)
        {
            if (skill.HasStatChanges) await ApplyStatChangesAsync(skill, userIsPlayer, isStatusMove: true);
            if (skill.Ailment != SkillAilment.None) await PlaySkillFxAsync(skill, targetIsEnemy: userIsPlayer);
            if (skill.Ailment != SkillAilment.None || !skill.HasStatChanges)
                await TryInflictAilmentAsync(skill, targetIsEnemy: userIsPlayer, isStatusMove: true);
        }
        #endregion
    }
}
