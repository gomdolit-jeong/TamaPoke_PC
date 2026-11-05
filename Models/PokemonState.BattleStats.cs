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
            ClearMoveLock(userIsPlayer: true);
            ClearMoveLock(userIsPlayer: false);
            _playerUsedMoves.Clear(); _enemyUsedMoves.Clear();
            _playerLostTypes.Clear(); _enemyLostTypes.Clear();
            _playerTrapTurns = _enemyTrapTurns = 0;
            _playerTrapSkill = _enemyTrapSkill = null;
        }

        // 여러 턴에 걸친 기술의 진행 상태
        private sealed class MoveLock
        {
            public SkillInfo? Forced;  // 다음 턴에 자동으로 나가는 기술 (모으기 2턴째, 재충전, 고정 행동)
            public bool Recharge;      // 다음 턴은 쉼
            public int LockTurns;      // 고정 행동의 남은 턴
            public SkillInfo? LockSkill; // 고정 행동 중인 기술 (마지막 턴이 끝날 때까지 유지)
            public int RollCount;      // 구르기 등을 연속으로 맞힌 횟수
            public bool Hidden;        // 공중날기 등으로 숨어 있어 공격에 맞지 않음
        }

        private const int ROLLING_TURNS = 5, UPROAR_TURNS = 3;
        private bool IsUproarActive => _playerLock.LockSkill?.IsUproar == true || _enemyLock.LockSkill?.IsUproar == true;

        // 구르기, 아이스볼: 맞힐 때마다 위력 2배 (30 → 60 → 120 → 240 → 480)
        private int PowerOf(SkillInfo skill, bool userIsPlayer) =>
            skill.IsRolling ? skill.Power << Math.Min(LockOf(userIsPlayer).RollCount, ROLLING_TURNS - 1) : skill.Power;

        // 묶기 기술 상태: 남은 턴과 묶은 기술 (묶인 쪽 기준)
        private const int TRAP_DAMAGE_DIVISOR = 8;
        private int _playerTrapTurns, _enemyTrapTurns;
        private SkillInfo? _playerTrapSkill, _enemyTrapSkill;

        // 턴이 끝날 때 묶인 쪽이 데미지를 받고, 턴이 다 되면 풀려납니다.
        private async Task ProcessTrapDamageAsync(bool targetIsPlayer)
        {
            int turns = targetIsPlayer ? _playerTrapTurns : _enemyTrapTurns;
            int hp = targetIsPlayer ? PlayerHp : EnemyHp;
            if (turns <= 0 || hp <= 0) return;

            int maxHp = targetIsPlayer ? PlayerMaxHp : EnemyMaxHp;
            string who = targetIsPlayer ? PlayerBattleName : Loc.F("battle.wildName", EnemyName);
            string moveName = (targetIsPlayer ? _playerTrapSkill : _enemyTrapSkill)?.Name ?? "";
            int damage = Math.Max(1, maxHp / TRAP_DAMAGE_DIVISOR);

            if (targetIsPlayer) { PlayerHp = Math.Max(0, hp - damage); IsPlayerTakingDamage = true; }
            else { EnemyHp = Math.Max(0, hp - damage); IsEnemyTakingDamage = true; }
            BattleMessage = Loc.F("battle.trapTick", who, moveName, damage);
            await Task.Delay(HIT_SHAKE_MS);
            IsPlayerTakingDamage = false; IsEnemyTakingDamage = false;
            await Task.Delay(HIT_REST_MS);

            turns--;
            if (targetIsPlayer) _playerTrapTurns = turns; else _enemyTrapTurns = turns;
            if (turns == 0 && (targetIsPlayer ? PlayerHp : EnemyHp) > 0)
            {
                BattleMessage = Loc.F("battle.trapFree", who, moveName);
                await Task.Delay(1000);
            }
        }

        // 소란피기: 맞는 쪽이 잠들어 있으면 깨웁니다.
        private async Task WakeByUproarAsync(SkillInfo skill, bool userIsPlayer)
        {
            if (!skill.IsUproar) return;
            bool targetIsPlayer = !userIsPlayer;
            if ((targetIsPlayer ? PlayerAilment : EnemyAilment) != SkillAilment.Sleep) return;

            if (targetIsPlayer) { PlayerAilment = SkillAilment.None; _playerAilmentTurns = 0; }
            else { EnemyAilment = SkillAilment.None; _enemyAilmentTurns = 0; }
            BattleMessage = Loc.F("battle.uproarWake", targetIsPlayer ? PlayerBattleName : Loc.F("battle.wildName", EnemyName));
            await Task.Delay(1000);
        }
        private readonly MoveLock _playerLock = new(), _enemyLock = new();
        private MoveLock LockOf(bool userIsPlayer) => userIsPlayer ? _playerLock : _enemyLock;

        private void SetHidden(bool userIsPlayer, bool hidden)
        {
            LockOf(userIsPlayer).Hidden = hidden;
            if (userIsPlayer) IsPlayerVisible = !hidden; else IsEnemyVisible = !hidden;
        }

        // 잠듦, 풀죽음, 빗나감 등으로 끊기면 진행 중이던 기술도 끝납니다.
        private void ClearMoveLock(bool userIsPlayer)
        {
            var state = LockOf(userIsPlayer);
            state.Forced = null; state.Recharge = false; state.LockTurns = 0; state.LockSkill = null; state.RollCount = 0;
            SetHidden(userIsPlayer, false);
        }

        // 재충전 턴이면 쉬고 true
        private async Task<bool> RestIfRechargingAsync(bool userIsPlayer, string who)
        {
            if (!LockOf(userIsPlayer).Recharge) return false;
            ClearMoveLock(userIsPlayer);
            BattleMessage = Loc.F("battle.recharge", who);
            await Task.Delay(1500);
            return true;
        }

        private const int HIT_MOTION_TICKS = 15;
        private const int FAINT_HOLD_TICKS = 9999; // 내 포켓몬: 패배 후 배틀이 닫힐 때까지

        // 맞은 쪽 모션: 쓰러졌으면 기절 모션을 재생하고 마지막 장면에서 멈춥니다.
        private void PlayHitMotion(bool targetIsEnemy)
        {
            bool fainted = (targetIsEnemy ? EnemyHp : PlayerHp) <= 0;
            int motion = fainted ? ANIM_FAINT : ANIM_HURT;

            if (targetIsEnemy)
            {
                if (_enemyMotion == ANIM_FAINT) return;
                _enemyTempActionTimer = fainted ? 0 : HIT_MOTION_TICKS;
                UpdateEnemyAnimation(motion);
            }
            else
            {
                if (_currentActionId == ANIM_FAINT) return;
                PlayTempAction(motion, fainted ? FAINT_HOLD_TICKS : HIT_MOTION_TICKS);
                UpdateAnimation(motion);
            }
        }

        private const int CHARGE_ANIM_TICKS = 40; // 모으는 메시지가 보이는 1.3초 동안 (33ms 단위)

        // 공격 모션: 연속 공격은 연속 모션, 물리는 공격, 특수는 발사 (대부분의 스프라이트에 있는 모션만 씁니다)
        private static int AttackAnimationFor(SkillInfo skill, int hits) =>
            hits > 1 ? ANIM_DOUBLE : skill.Category == SkillCategory.Special ? ANIM_SHOOT : ANIM_ATTACK;

        // 모으기/숨기 기술의 첫 턴이면 준비만 하고 true, 둘째 턴이면 상태를 풀고 false
        private async Task<bool> StartChargeAsync(SkillInfo skill, bool userIsPlayer, string who)
        {
            if (!skill.IsCharging && !skill.IsHiding) return false;

            var state = LockOf(userIsPlayer);
            if (state.Forced?.Id == skill.Id)
            {
                ClearMoveLock(userIsPlayer);
                return false;
            }

            state.Forced = skill;
            if (skill.IsHiding) SetHidden(userIsPlayer, true);
            else if (userIsPlayer) { PlayTempAction(ANIM_CHARGE, CHARGE_ANIM_TICKS); UpdateAnimation(ANIM_CHARGE); }
            else { _enemyTempActionTimer = CHARGE_ANIM_TICKS; UpdateEnemyAnimation(ANIM_CHARGE); }
            BattleMessage = Loc.F(skill.IsHiding ? "battle.hiding" : "battle.charging", who);
            await Task.WhenAll(Task.Delay(1300), skill.IsCharging ? PlayChargeFxAsync(skill, userIsPlayer) : Task.CompletedTask);
            return true;
        }

        // 고정 행동: 처음 쓸 때 턴 수를 정하고, 턴이 남아 있으면 다음 턴에도 자동으로 나갑니다.
        private void AdvanceMoveLock(SkillInfo skill, bool userIsPlayer, Random rand)
        {
            if (!skill.IsLocking) return;
            var state = LockOf(userIsPlayer);
            if (state.LockTurns == 0)
            {
                state.LockTurns = skill.IsRolling ? ROLLING_TURNS : skill.IsUproar ? UPROAR_TURNS : rand.Next(2, 4);
                state.LockSkill = skill;
                state.RollCount = 0;
            }
            state.LockTurns--;
            state.Forced = state.LockTurns > 0 ? skill : null;
        }

        // 맞는 쪽이 숨어 있으면 빗나갑니다. (자기 능력치만 올리는 기술은 상관없음)
        private static bool MissesHiddenTarget(SkillInfo skill, MoveLock target) => target.Hidden && !IsSelfTargetingStatusMove(skill);

        // 이번 배틀에서 쓴 기술 (비장의무기)과 잃은 타입 (불사르기, 전광쌍격)
        private readonly HashSet<int> _playerUsedMoves = new(), _enemyUsedMoves = new();
        private readonly List<PokemonType> _playerLostTypes = new(), _enemyLostTypes = new();

        // 폼 타입과 잃은 타입을 반영한 배틀용 정보 (상성과 자속 계산에 쓰입니다)
        private static PokemonInfo? WithBattleTypes(PokemonInfo? info, PokemonType type1, PokemonType type2, List<PokemonType> lost)
        {
            if (info == null) return null;
            if (lost.Contains(type1)) type1 = PokemonType.None;
            if (lost.Contains(type2)) type2 = PokemonType.None;
            if (info.Type1 == type1 && info.Type2 == type2) return info;
            return new PokemonInfo
            {
                Id = info.Id, BaseHp = info.BaseHp, BaseAtk = info.BaseAtk, BaseDef = info.BaseDef, BaseSpeed = info.BaseSpeed, BaseSpA = info.BaseSpA, BaseSpD = info.BaseSpD,
                Type1 = type1,
                Type2 = type2,
            };
        }

        private PokemonInfo? EnemyBattleInfo
        {
            get
            {
                var (type1, type2) = PokemonForms.GetTypes(EnemySpeciesId, _enemySpriteFileName);
                return WithBattleTypes(PokemonDex.Get(EnemySpeciesId), type1, type2, _enemyLostTypes);
            }
        }

        private static bool HasType(PokemonInfo? info, PokemonType type) => info != null && type != PokemonType.None && (info.Type1 == type || info.Type2 == type);

        // 비장의무기: 알고 있는 다른 기술이 있고, 그 기술을 모두 써 봤을 때만
        private bool HasUsedOtherMoves(SkillInfo skill, bool userIsPlayer)
        {
            var known = (userIsPlayer ? Skills : EnemySkills).Where(id => id != 0 && id != skill.Id).ToList();
            var used = userIsPlayer ? _playerUsedMoves : _enemyUsedMoves;
            return known.Count > 0 && known.All(used.Contains);
        }

        // 이번 턴 상대의 행동과 상관없이 정해지는 조건
        private bool MoveStateAllows(SkillInfo skill, bool userIsPlayer, bool wasFirstMove)
        {
            SkillAilment userAilment = userIsPlayer ? PlayerAilment : EnemyAilment;
            SkillAilment targetAilment = userIsPlayer ? EnemyAilment : PlayerAilment;
            PokemonInfo? userInfo = userIsPlayer ? MyBattleInfo : EnemyBattleInfo;
            PokemonInfo? targetInfo = userIsPlayer ? EnemyBattleInfo : MyBattleInfo;

            if (skill.IsUnavailable) return false;
            if (skill.IsFirstTurnOnly && !wasFirstMove) return false;
            if (skill.NeedsTargetAsleep && targetAilment != SkillAilment.Sleep) return false;
            if (skill.NeedsUserAsleep && userAilment != SkillAilment.Sleep) return false;
            if (skill.NeedsSharedType && !(HasType(targetInfo, userInfo?.Type1 ?? PokemonType.None) || HasType(targetInfo, userInfo?.Type2 ?? PokemonType.None))) return false;
            if (skill.LosesOwnType && !HasType(userInfo, skill.Type)) return false;
            if (skill.IsLastResort && !HasUsedOtherMoves(skill, userIsPlayer)) return false;
            return true;
        }

        // 이번 턴에 고른 기술 (회피/아이템 등 기술을 안 쓴 턴은 null)과 이번 턴에 맞은 기술
        private SkillInfo? _playerChoice, _enemyChoice, _playerHitBy, _enemyHitBy;

        private void ClearTurnState()
        {
            _playerChoice = _enemyChoice = _playerHitBy = _enemyHitBy = null;
            if (_playerLock.LockTurns == 0) _playerLock.LockSkill = null; // 고정 행동은 마지막 턴이 끝날 때 해제
            if (_enemyLock.LockTurns == 0) _enemyLock.LockSkill = null;
        }

        // 조건이 맞지 않아 실패하는 기술인지 (쓴 쪽, 상대의 상태로 판단)
        private bool MoveConditionMet(SkillInfo skill, bool userIsPlayer, bool wasFirstMove)
        {
            SkillInfo? targetChoice = userIsPlayer ? _enemyChoice : _playerChoice;
            SkillInfo? hitBy = userIsPlayer ? _playerHitBy : _enemyHitBy;

            if (!MoveStateAllows(skill, userIsPlayer, wasFirstMove)) return false;
            if (skill.NeedsPhysicalHitFirst && hitBy?.Category != SkillCategory.Physical) return false;
            if (skill.NeedsTargetAttacking && (targetChoice == null || targetChoice.Power <= 0)) return false;
            return true;
        }

        // 상대 AI: 지금 쓰면 반드시 실패하는 기술은 고르지 않습니다. (상대가 뭘 고를지는 모르므로 기습/트랩셸은 후보에 둡니다)
        private bool EnemyCanUseNow(SkillInfo skill) => MoveStateAllows(skill, userIsPlayer: false, wasFirstMove: !_enemyHasMoved);

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

            if (skill.NeedsRecharge && dealt > 0)
            {
                var state = LockOf(userIsPlayer);
                state.Recharge = true; state.Forced = skill;
            }
            if (skill.LosesOwnType && dealt > 0)
            {
                (userIsPlayer ? _playerLostTypes : _enemyLostTypes).Add(skill.Type);
            }
            if (skill.IsRolling && dealt > 0) LockOf(userIsPlayer).RollCount++;
            if (skill.IsTrapping && dealt > 0 && (userIsPlayer ? EnemyHp : PlayerHp) > 0 && (userIsPlayer ? _enemyTrapTurns : _playerTrapTurns) == 0)
            {
                int turns = rand.Next(4, 6);
                if (userIsPlayer) { _enemyTrapTurns = turns; _enemyTrapSkill = skill; }
                else { _playerTrapTurns = turns; _playerTrapSkill = skill; }
                BattleMessage = Loc.F("battle.trapStart", userIsPlayer ? Loc.F("battle.wildName", EnemyName) : PlayerBattleName, skill.Name);
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
