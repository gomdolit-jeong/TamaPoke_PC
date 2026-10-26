using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public enum BattleAction { Dodge, Run }

    public partial class PokemonState
    {
        #region 실전 전투 능력치 계산 (Combat Stats)
        // 적(야생/관장) 포켓몬의 능력치: 유전자 15 고정
        private int WildStat(int? baseStat) => BattleCalculator.Stat(baseStat ?? 50, BattleCalculator.WILD_GENE, EnemyLevel);

        [JsonIgnore]
        public int CombatMaxHp
        {
            get
            {
                return BattleCalculator.MaxHp(PokemonDex.Get(SpeciesId)?.BaseHp ?? 50, Genes.HpGene + (TrDef / 4), Level);
            }
        }

        [JsonIgnore]
        public int CombatAtk
        {
            get
            {
                return BattleCalculator.Stat(PokemonDex.Get(SpeciesId)?.BaseAtk ?? 50, Genes.AtkGene + (TrAtk / 4), Level);
            }
        }

        [JsonIgnore]
        public int CombatDef
        {
            get
            {
                return BattleCalculator.Stat(PokemonDex.Get(SpeciesId)?.BaseDef ?? 50, Genes.DefGene + (TrDef / 4), Level);
            }
        }

        [JsonIgnore]
        public int CombatSpeed
        {
            get
            {
                return BattleCalculator.Stat(PokemonDex.Get(SpeciesId)?.BaseSpeed ?? 50, Genes.SpeGene + (TrSpeed / 4), Level);
            }
        }

        // 특수 기술용 능력치 (특수공격은 노력치 그대로, 특수방어는 노력치 절반을 더함)
        [JsonIgnore] public int CombatSpAtk => BattleCalculator.Stat(PokemonDex.Get(SpeciesId)?.BaseSpA ?? 50, Genes.AtkGene - 80, Level) + TrAtk;
        [JsonIgnore] public int CombatSpDef => BattleCalculator.Stat(PokemonDex.Get(SpeciesId)?.BaseSpD ?? 50, Genes.DefGene - 80, Level) + (TrDef / 2);
        #endregion

        #region 배틀 시스템 UI 상태 (Battle System UI)
        private bool _isBattleOpen = false;
        [JsonIgnore] public bool IsBattleOpen { get => _isBattleOpen; set { if (SetProperty(ref _isBattleOpen, value)) { OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

        private bool _isDefeatedFadeOut = false;
        [JsonIgnore] public bool IsDefeatedFadeOut { get => _isDefeatedFadeOut; set => SetProperty(ref _isDefeatedFadeOut, value); }

        private bool _isPlayerTurn = true;
        [JsonIgnore] public bool IsPlayerTurn
        {
            get => _isPlayerTurn;
            set
            {
                if (SetProperty(ref _isPlayerTurn, value))
                {
                    OnPropertyChanged(nameof(IsMainMenuVisible));
                    OnPropertyChanged(nameof(IsAttackMenuVisible));
                }
            }
        }

        [JsonIgnore] public bool IsMainMenuVisible => !IsCatchOffered && !IsBattleResolved && !IsAttackMenuOpen && !IsInventoryOpen && IsPlayerTurn;
        [JsonIgnore] public bool IsAttackMenuVisible => !IsCatchOffered && !IsBattleResolved && IsAttackMenuOpen && !IsInventoryOpen && IsPlayerTurn;

        private bool _isBattleResolved = false;
        [JsonIgnore] public bool IsBattleResolved { get => _isBattleResolved; set { if (SetProperty(ref _isBattleResolved, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isCatchOffered = false;
        [JsonIgnore] public bool IsCatchOffered { get => _isCatchOffered; set { if (SetProperty(ref _isCatchOffered, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isAttackMenuOpen = false;
        [JsonIgnore] public bool IsAttackMenuOpen { get => _isAttackMenuOpen; set { if (SetProperty(ref _isAttackMenuOpen, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isBattleInventoryOpen = false;
        [JsonIgnore] public bool IsBattleInventoryOpen
        {
            get => _isBattleInventoryOpen;
            set
            {
                if (SetProperty(ref _isBattleInventoryOpen, value))
                {
                    OnPropertyChanged(nameof(IsMainMenuVisible));
                    OnPropertyChanged(nameof(IsAttackMenuVisible));
                }
            }
        }

        private bool _isEnemyVisible = true;
        [JsonIgnore] public bool IsEnemyVisible { get => _isEnemyVisible; set => SetProperty(ref _isEnemyVisible, value); }

        private int _enemySpeciesId = 1;
        [JsonIgnore] public int EnemySpeciesId { get => _enemySpeciesId; set { if (SetProperty(ref _enemySpeciesId, value)) OnPropertyChanged(nameof(EnemyName)); } }

        [JsonIgnore] public string EnemyName => PokemonDex.GetName(EnemySpeciesId);

        private int _enemyLevel = 1;
        [JsonIgnore] public int EnemyLevel { get => _enemyLevel; set => SetProperty(ref _enemyLevel, value); }

        private int _playerHp = 100;
        [JsonIgnore] public int PlayerHp { get => _playerHp; set { if (SetProperty(ref _playerHp, value)) OnPropertyChanged(nameof(PlayerHpColor)); } }
        private int _playerMaxHp = 100;
        [JsonIgnore] public int PlayerMaxHp { get => _playerMaxHp; set { if (SetProperty(ref _playerMaxHp, value)) OnPropertyChanged(nameof(PlayerHpColor)); } }

        private int _enemyHp = 100;
        [JsonIgnore] public int EnemyHp { get => _enemyHp; set { if (SetProperty(ref _enemyHp, value)) OnPropertyChanged(nameof(EnemyHpColor)); } }
        private int _enemyMaxHp = 100;
        [JsonIgnore] public int EnemyMaxHp { get => _enemyMaxHp; set { if (SetProperty(ref _enemyMaxHp, value)) OnPropertyChanged(nameof(EnemyHpColor)); } }

        [JsonIgnore] public System.Windows.Media.Brush PlayerHpColor => GetHpColor(PlayerHp, PlayerMaxHp);
        [JsonIgnore] public System.Windows.Media.Brush EnemyHpColor => GetHpColor(EnemyHp, EnemyMaxHp);

        private System.Windows.Media.Brush GetHpColor(int hp, int maxHp)
        {
            if (maxHp <= 0) return System.Windows.Media.Brushes.Green;
            double ratio = (double)hp / maxHp;
            if (ratio > 0.5) return BrushCache.Get("#4CAF50");
            if (ratio > 0.2) return BrushCache.Get("#FBC02D");
            return BrushCache.Get("#E53935");
        }

        private bool _isPlayerTakingDamage = false;
        [JsonIgnore] public bool IsPlayerTakingDamage { get => _isPlayerTakingDamage; set => SetProperty(ref _isPlayerTakingDamage, value); }
        private bool _isEnemyTakingDamage = false;
        [JsonIgnore] public bool IsEnemyTakingDamage { get => _isEnemyTakingDamage; set => SetProperty(ref _isEnemyTakingDamage, value); }

        private string _battleMessage = "";
        [JsonIgnore] public string BattleMessage { get => _battleMessage; set => SetProperty(ref _battleMessage, value); }

        private bool _isCounterReady = false;
        [JsonIgnore] public int[] EnemySkills { get; set; } = new int[4] { 0, 0, 0, 0 };

        private void GenerateEnemySkills()
        {
            for (int i = 0; i < 4; i++) EnemySkills[i] = 0;
            var d = PokemonDex.Get(EnemySpeciesId);
            if (d == null) return;

            int n = SkillDex.GetLearnCount(EnemySpeciesId);
            if (n == 0)
            {
                EnemySkills[0] = 1;
                int fallback = SkillDex.GetFallbackMove(d.Type1);
                if (fallback != 1) EnemySkills[1] = fallback;
                return;
            }

            int[] score = new int[4];
            for (int pass = 0; pass < 2; pass++)
            {
                bool tmPass = (pass == 1);
                for (int i = 0; i < n; i++)
                {
                    int at = SkillDex.GetLearnLevel(EnemySpeciesId, i);
                    if (tmPass != (at == 0)) continue;
                    if (!tmPass && at > EnemyLevel) continue;
                    if (tmPass && EnemyLevel < 30) continue;

                    int mv = SkillDex.GetLearnMove(EnemySpeciesId, i);
                    if (mv == 0 || EnemySkills.Contains(mv)) continue;

                    var m = SkillDex.GetSkill(mv);
                    if (m == null) continue;

                    int sc = (m.Category == SkillCategory.Status) ? 10 : m.Power + 20;
                    if (m.Category != SkillCategory.Status && (m.Type == d.Type1 || m.Type == d.Type2)) sc += 40;

                    sc += (at == 0 ? 50 : at);

                    int slot = -1;
                    for (int s = 0; s < 4; s++) { if (sc > score[s]) { slot = s; break; } }
                    if (slot < 0) continue;
                    for (int s = 3; s > slot; s--) { score[s] = score[s - 1]; EnemySkills[s] = EnemySkills[s - 1]; }
                    score[slot] = sc; EnemySkills[slot] = mv;
                }
            }

            bool hasAttack = false;
            for (int i = 0; i < 4; i++)
            {
                if (EnemySkills[i] != 0 && SkillDex.GetSkill(EnemySkills[i])?.Category != SkillCategory.Status)
                {
                    hasAttack = true; break;
                }
            }

            if (!hasAttack)
            {
                int slotToFill = 0;
                for (int i = 0; i < 4; i++) { if (EnemySkills[i] == 0) { slotToFill = i; break; } }
                int fallback = SkillDex.GetFallbackMove(d.Type1);
                EnemySkills[slotToFill] = fallback > 0 ? fallback : 1;
            }
        }

        private SkillInfo ChooseEnemySkill()
        {
            var enemyInfo = PokemonDex.Get(EnemySpeciesId);
            var myInfo = PokemonDex.Get(SpeciesId);
            Random rand = new Random();

            var availableSkills = EnemySkills.Where(id => id != 0).Select(id => SkillDex.GetSkill(id)).Where(s => s != null).ToList();
            if (availableSkills.Count == 0) return SkillDex.GetSkill(1)!;

            SkillInfo bestSkill = availableSkills[0]!;
            int bestScore = -9999;

            foreach (var m in availableSkills)
            {
                int score = 0;
                if (m!.Category == SkillCategory.Status)
                {
                    // 상태이상을 걸 수 있는 상황이면 40% 확률로 우선 사용, 걸 수 없으면 사용하지 않음
                    bool canInflict = m.Ailment != SkillAilment.None && PlayerAilment == SkillAilment.None &&
                                      !AilmentHelper.IsImmune(m.Ailment, myInfo?.Type1 ?? PokemonType.Normal, myInfo?.Type2 ?? PokemonType.None);
                    if (m.Ailment != SkillAilment.None) score = canInflict && rand.Next(100) < 40 ? 500 : -100;
                    else score = 5;
                }
                else
                {
                    double typeMult = BattleCalculator.TypeMultiplier(m.Type, myInfo);
                    double stabMult = BattleCalculator.StabMultiplier(m.Type, enemyInfo);
                    int enemyAtk = WildStat(m.Category == SkillCategory.Special ? enemyInfo?.BaseSpA : enemyInfo?.BaseAtk);
                    int expectedDamage = BattleCalculator.RawDamage(enemyAtk, CombatDef, typeMult, stabMult, m.Power);

                    score = expectedDamage;
                    if (expectedDamage >= PlayerHp) score += 1000;

                    int acc = m.Accuracy == 0 ? 100 : m.Accuracy;
                    score = score * acc / 100;

                    if (IsGymBattle)
                    {
                        if (typeMult == 0) score = -9999;
                        else if (typeMult >= 2.0) score += 200;
                        else if (typeMult <= 0.5) score -= 50;
                    }
                }

                if (!IsGymBattle) score += rand.Next(0, 10);
                if (score > bestScore) { bestScore = score; bestSkill = m; }
            }
            return bestSkill;
        }
        #endregion

        #region 상태이상 관리 (Ailments & Battle Logic)
        private SkillAilment _playerAilment = SkillAilment.None;
        // 배지/효과 표시는 BattleView에서 AilmentStyleConverter로 변환합니다.
        [JsonIgnore] public SkillAilment PlayerAilment { get => _playerAilment; set => SetProperty(ref _playerAilment, value); }
        private int _playerAilmentTurns = 0;
        private int _playerBadPoisonTurnCount = 0;

        private SkillAilment _enemyAilment = SkillAilment.None;
        [JsonIgnore] public SkillAilment EnemyAilment { get => _enemyAilment; set => SetProperty(ref _enemyAilment, value); }

        // 상태이상이 막 걸린 순간 배지를 깜빡이게 하는 신호
        private bool _isPlayerAilmentFlash;
        [JsonIgnore] public bool IsPlayerAilmentFlash { get => _isPlayerAilmentFlash; set => SetProperty(ref _isPlayerAilmentFlash, value); }
        private bool _isEnemyAilmentFlash;
        [JsonIgnore] public bool IsEnemyAilmentFlash { get => _isEnemyAilmentFlash; set => SetProperty(ref _isEnemyAilmentFlash, value); }
        private int _enemyAilmentTurns = 0;
        private int _enemyBadPoisonTurnCount = 0;

        private bool _isEnemyShiny = false;
        [JsonIgnore] public bool IsEnemyShiny { get => _isEnemyShiny; set => SetProperty(ref _isEnemyShiny, value); }

        private bool _isEnemyFemale = false;
        private bool _isEnemyGenderless = false;
        private string _enemySpriteFileName = "";

        public async void StartWildBattle()
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;

            IsProfileOpen = false; _isCounterReady = false;
            IsBattleResolved = false; IsCatchOffered = false; IsAttackMenuOpen = false; IsEnemyVisible = true;
            IsSkillLearnMenuOpen = false; IsSkillReplaceMenuOpen = false;
            IsGymBattle = false;

            PlayerAilment = SkillAilment.None; _playerAilmentTurns = 0; _playerBadPoisonTurnCount = 0;
            EnemyAilment = SkillAilment.None; _enemyAilmentTurns = 0; _enemyBadPoisonTurnCount = 0;

            Random rand = new Random();
            // 야생 상대도 기본 이미지가 있는 포켓몬만 나옵니다.
            var legendaries = LegendaryIds.Where(SpriteForms.IsUsable).ToArray();
            if (rand.Next(100) < 1 && legendaries.Length > 0) { EnemySpeciesId = legendaries[rand.Next(legendaries.Length)]; }
            else { var ids = PokemonDex.AllPokemons.Where(p => p.Id <= 493 && !LegendaryIds.Contains(p.Id) && SpriteForms.IsUsable(p.Id)).Select(p => p.Id).ToList(); EnemySpeciesId = ids.Count > 0 ? ids[rand.Next(ids.Count)] : 1; }

            EnemyLevel = Math.Max(1, Level + rand.Next(-2, 3));
            PlayerMaxHp = CombatMaxHp; PlayerHp = PlayerMaxHp;

            IsEnemyShiny = rand.Next(100) < 1;
            (_isEnemyGenderless, _isEnemyFemale) = PokemonDex.RollGender(EnemySpeciesId, rand);

            string[] allForms = AppPaths.GetSpriteForms(EnemySpeciesId);
            _enemySpriteFileName = SpriteForms.ApplyVariants(allForms, SpriteForms.PickBaseForm(EnemySpeciesId, allForms, rand), IsEnemyShiny, _isEnemyFemale);

            var enemyInfo = PokemonDex.Get(EnemySpeciesId);
            int enemyBaseHp = enemyInfo != null ? enemyInfo.BaseHp : 50;
            EnemyMaxHp = BattleCalculator.MaxHp(enemyBaseHp, BattleCalculator.WILD_GENE, EnemyLevel);
            EnemyHp = EnemyMaxHp;

            GenerateEnemySkills();
            UpdateEnemyAnimation(0);

            IsBattleOpen = true; IsPlayerTurn = false;
            BattleMessage = Loc.F(IsEnemyShiny ? "battle.appear.shiny" : "battle.appear", EnemyName);

            if (Settings.UseTrayNotifications) TrayNotificationRequested?.Invoke(Loc.T("notify.wild.title"), "⚔️");

            await Task.Delay(1500);
            BattleMessage = Loc.T("battle.choose");
            IsPlayerTurn = true;
        }

        public void CloseBattle()
        {
            IsBattleOpen = false;
            IsGymBattle = false;
            BattleMessage = "";
        }

        private async Task<bool> PlayerCanActThisTurnAsync()
        {
            if (PlayerAilment != SkillAilment.None)
            {
                if (_playerAilmentTurns > 0) _playerAilmentTurns--;
                bool canAct = AilmentHelper.CanActThisTurn(PlayerAilment, ref _playerAilmentTurns, Name, out string statusMsg, out bool cured);
                if (cured) PlayerAilment = SkillAilment.None;

                if (!string.IsNullOrEmpty(statusMsg))
                {
                    BattleMessage = statusMsg;
                    await Task.Delay(1500);
                }
                return canAct;
            }
            return true;
        }

        public async Task ExecuteTurnAsync(BattleAction playerAction)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || !IsPlayerTurn) return;
            IsPlayerTurn = false; Random rand = new Random();

            if (playerAction == BattleAction.Run)
            {
                BattleMessage = Loc.T("battle.ran");
                await Task.Delay(1000);
                CloseBattle();
                IsPlayerTurn = true;
                return;
            }

            bool playerDodged = false;
            if (playerAction == BattleAction.Dodge)
            {
                if (!await PlayerCanActThisTurnAsync())
                {
                    await ProcessPlayerTurnEndAilment();
                    if (PlayerHp <= 0) { await CheckBattleEndAsync(); return; }
                    await EnemyTurnAction(false);
                    return;
                }

                if (rand.Next(100) < 70)
                {
                    playerDodged = true; _isCounterReady = true;
                    BattleMessage = Loc.T("battle.dodge.ready");
                }
                else BattleMessage = Loc.T("battle.dodge.fail");

                await Task.Delay(1500);
            }

            if (EnemyHp <= 0) { await CheckBattleEndAsync(); return; }
            await EnemyTurnAction(playerDodged);
        }

        public async Task ExecuteSkillTurnAsync(SkillInfo playerSkill)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || !IsPlayerTurn) return;
            IsPlayerTurn = false; IsAttackMenuOpen = false;
            Random rand = new Random();

            if (!await PlayerCanActThisTurnAsync())
            {
                await ProcessPlayerTurnEndAilment();
                if (PlayerHp <= 0) { await CheckBattleEndAsync(); return; }
                await EnemyTurnAction(false);
                return;
            }

            var myInfo = PokemonDex.Get(SpeciesId);
            var enemyInfo = PokemonDex.Get(EnemySpeciesId);

            BattleMessage = Loc.F("battle.used", Name, playerSkill.Name);
            await Task.Delay(1000);

            bool isHit = playerSkill.Accuracy == 0 || rand.Next(100) < playerSkill.Accuracy;

            if (isHit && playerSkill.Power > 0)
            {
                bool special = playerSkill.Category == SkillCategory.Special;
                double typeMultiplier = BattleCalculator.TypeMultiplier(playerSkill.Type, enemyInfo);
                double stabMultiplier = BattleCalculator.StabMultiplier(playerSkill.Type, myInfo);

                int myCombatAtk = special ? CombatSpAtk : CombatAtk;
                int enemyCombatDef = WildStat(special ? enemyInfo?.BaseSpD : enemyInfo?.BaseDef);
                if (IsGymBattle) enemyCombatDef = (int)(enemyCombatDef * BattleCalculator.GYM_BONUS);

                int damage = BattleCalculator.RawDamage(myCombatAtk, enemyCombatDef, typeMultiplier, stabMultiplier, playerSkill.Power);

                if (_isCounterReady) { damage *= 2; _isCounterReady = false; }
                damage = Math.Max(1, damage);

                IsEnemyTakingDamage = true;
                EnemyHp = Math.Max(0, EnemyHp - damage);

                BattleMessage = Loc.F("battle.damage.dealt", BattleCalculator.EffectivenessText(typeMultiplier), damage);

                await TryInflictAilmentAsync(playerSkill, targetIsEnemy: true, isStatusMove: false);

                int attackAnim = playerSkill.Category == SkillCategory.Physical ? ANIM_ATTACK
                               : playerSkill.Category == SkillCategory.Special ? ANIM_DEEPBREATH
                               : ANIM_POSE;
                PlayTempAction(attackAnim, 15); UpdateAnimation(attackAnim);
                _enemyTempActionTimer = 15; UpdateEnemyAnimation(6);

                await Task.Delay(600); IsEnemyTakingDamage = false; await Task.Delay(900);
            }
            else if (isHit)
            {
                // 위력 0인 변화 기술 (전기자석파, 수면가루 등)
                PlayTempAction(ANIM_POSE, 30); UpdateAnimation(ANIM_POSE);
                await TryInflictAilmentAsync(playerSkill, targetIsEnemy: true, isStatusMove: true);
            }
            else
            {
                _isCounterReady = false;
                BattleMessage = Loc.F("battle.missed", Name);
                await Task.Delay(1500);
            }

            if (EnemyHp <= 0) { await CheckBattleEndAsync(); return; }

            await EnemyTurnAction(false);
        }

        public async Task ExecuteItemTurnAsync(ItemInfo selectedItem)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || !IsPlayerTurn) return;
            IsPlayerTurn = false;

            if (selectedItem.Type == ItemType.Potion)
            {
                int healAmount = (int)(PlayerMaxHp * (selectedItem.EffectValue / 100.0));
                if (healAmount < 1) healAmount = 1;
                PlayerHp = Math.Min(PlayerMaxHp, PlayerHp + healAmount);

                BattleMessage = Loc.F("battle.item.used", Name, selectedItem.Name, healAmount);

                PlayTempAction(10, 30); CheckStateAndAnimate();
                await Task.Delay(1500);

                await EnemyTurnAction(false);
            }
        }

        // 기술의 상태이상을 대상에게 걸어 봅니다. (확률, 이미 걸린 상태, 타입 면역 반영)
        // isStatusMove: 위력 0인 변화 기술이면 실패/무효 메시지도 보여줍니다.
        private async Task<bool> TryInflictAilmentAsync(SkillInfo skill, bool targetIsEnemy, bool isStatusMove)
        {
            string targetName = targetIsEnemy ? Loc.F("battle.wildName", EnemyName) : Name;

            if (skill.Ailment == SkillAilment.None)
            {
                if (isStatusMove) { BattleMessage = Loc.T("battle.nothing"); await Task.Delay(1500); }
                return false;
            }

            var targetInfo = PokemonDex.Get((targetIsEnemy ? EnemySpeciesId : SpeciesId));
            bool alreadyAiled = (targetIsEnemy ? EnemyAilment : PlayerAilment) != SkillAilment.None;
            bool immune = AilmentHelper.IsImmune(skill.Ailment, targetInfo?.Type1 ?? PokemonType.Normal, targetInfo?.Type2 ?? PokemonType.None);

            if (alreadyAiled || immune)
            {
                if (isStatusMove)
                {
                    BattleMessage = immune ? Loc.F("battle.noEffectOn", targetName) : Loc.T("battle.failed");
                    await Task.Delay(1500);
                }
                return false;
            }

            if (new Random().Next(100) >= skill.AilmentChance)
            {
                if (isStatusMove) { BattleMessage = Loc.T("battle.failed"); await Task.Delay(1500); }
                return false;
            }

            if (targetIsEnemy)
            {
                EnemyAilment = skill.Ailment;
                _enemyAilmentTurns = AilmentHelper.GetInitialTurns(EnemyAilment);
                _enemyBadPoisonTurnCount = 0;
            }
            else
            {
                PlayerAilment = skill.Ailment;
                _playerAilmentTurns = AilmentHelper.GetInitialTurns(PlayerAilment);
                _playerBadPoisonTurnCount = 0;
            }

            if (!isStatusMove) await Task.Delay(1000);
            BattleMessage = Loc.F("battle.ailment.inflicted", targetName, AilmentHelper.GetAilmentIcon(skill.Ailment));
            if (targetIsEnemy) IsEnemyAilmentFlash = true; else IsPlayerAilmentFlash = true;
            await Task.Delay(1500);
            IsEnemyAilmentFlash = false; IsPlayerAilmentFlash = false;
            return true;
        }

        private async Task ProcessPlayerTurnEndAilment()
        {
            if (PlayerAilment != SkillAilment.None && PlayerHp > 0)
            {
                if (PlayerAilment == SkillAilment.BadPoison) _playerBadPoisonTurnCount++;
                int tickDamage = AilmentHelper.GetTurnEndDamage(PlayerAilment, PlayerMaxHp, _playerBadPoisonTurnCount, out string ailmentMsg);
                if (tickDamage > 0)
                {
                    PlayerHp = Math.Max(0, PlayerHp - tickDamage);
                    BattleMessage = Loc.F("battle.ailment.tick", Name, ailmentMsg, tickDamage);
                    await Task.Delay(1500);
                }
            }
        }

        private async Task EnemyTurnAction(bool playerDodged)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || IsBattleResolved) return;

            var myInfo = PokemonDex.Get(SpeciesId);
            var enemyInfo = PokemonDex.Get(EnemySpeciesId);
            Random rand = new Random();

            if (EnemyAilment != SkillAilment.None)
            {
                if (_enemyAilmentTurns > 0) _enemyAilmentTurns--;

                bool canAct = AilmentHelper.CanActThisTurn(EnemyAilment, ref _enemyAilmentTurns, Loc.F("battle.wildName", EnemyName), out string statusMsg, out bool cured);
                if (cured) EnemyAilment = SkillAilment.None;
                if (!string.IsNullOrEmpty(statusMsg))
                {
                    BattleMessage = statusMsg;
                    await Task.Delay(1500);
                }

                if (!canAct)
                {
                    await ProcessEnemyTurnEndAilment();
                    await ProcessPlayerTurnEndAilment();

                    if (PlayerHp <= 0 || EnemyHp <= 0) { await CheckBattleEndAsync(); return; }

                    BattleMessage = Loc.T("battle.choose");
                    IsPlayerTurn = true;
                    return;
                }
            }

            SkillInfo enemySkill = ChooseEnemySkill();
            BattleMessage = Loc.F("battle.used", Loc.F("battle.wildName", EnemyName), enemySkill.Name);
            await Task.Delay(1000);

            bool enemyHits = enemySkill.Accuracy == 0 || rand.Next(100) < enemySkill.Accuracy;

            if (!playerDodged && enemyHits && enemySkill.Power > 0)
            {
                bool special = enemySkill.Category == SkillCategory.Special;
                double typeMultiplier = BattleCalculator.TypeMultiplier(enemySkill.Type, myInfo);
                double stabMultiplier = BattleCalculator.StabMultiplier(enemySkill.Type, enemyInfo);

                int enemyCombatAtk = WildStat(special ? enemyInfo?.BaseSpA : enemyInfo?.BaseAtk);
                if (IsGymBattle) enemyCombatAtk = (int)(enemyCombatAtk * BattleCalculator.GYM_BONUS);
                int myCombatDef = special ? CombatSpDef : CombatDef;

                int enemyDamage = Math.Max(1, BattleCalculator.RawDamage(enemyCombatAtk, myCombatDef, typeMultiplier, stabMultiplier, enemySkill.Power));

                IsPlayerTakingDamage = true;
                PlayerHp = Math.Max(0, PlayerHp - enemyDamage);

                BattleMessage = Loc.F("battle.damage.taken", BattleCalculator.EffectivenessText(typeMultiplier), Name, enemyDamage);

                await TryInflictAilmentAsync(enemySkill, targetIsEnemy: false, isStatusMove: false);

                _enemyTempActionId = 1; _enemyTempActionTimer = 15; UpdateEnemyAnimation(1);
                PlayTempAction(6, 15); UpdateAnimation(6);

                await Task.Delay(600); IsPlayerTakingDamage = false; await Task.Delay(900);
            }
            else if (!playerDodged && enemyHits)
            {
                // 위력 0인 변화 기술
                _enemyTempActionId = ANIM_POSE; _enemyTempActionTimer = 30; UpdateEnemyAnimation(ANIM_POSE);
                await TryInflictAilmentAsync(enemySkill, targetIsEnemy: false, isStatusMove: true);
            }
            else if (playerDodged) { BattleMessage = Loc.F("battle.dodged", Name); await Task.Delay(1500); }
            else if (!enemyHits) { BattleMessage = Loc.F("battle.missed", Loc.F("battle.wildName", EnemyName)); await Task.Delay(1500); }

            await ProcessEnemyTurnEndAilment();
            await ProcessPlayerTurnEndAilment();

            if (PlayerHp <= 0 || EnemyHp <= 0) { await CheckBattleEndAsync(); return; }

            BattleMessage = Loc.T("battle.choose");
            IsPlayerTurn = true;
        }

        private async Task ProcessEnemyTurnEndAilment()
        {
            if (EnemyAilment != SkillAilment.None && EnemyHp > 0)
            {
                if (EnemyAilment == SkillAilment.BadPoison) _enemyBadPoisonTurnCount++;
                int tickDamage = AilmentHelper.GetTurnEndDamage(EnemyAilment, EnemyMaxHp, _enemyBadPoisonTurnCount, out string ailmentMsg);
                if (tickDamage > 0)
                {
                    EnemyHp = Math.Max(0, EnemyHp - tickDamage);
                    BattleMessage = Loc.F("battle.ailment.tick", Loc.F("battle.wildName", EnemyName), ailmentMsg, tickDamage);
                    await Task.Delay(1500);
                }
            }
        }
        #endregion

        #region 승리 및 보상 분기 시퀀스 (Victory & Sequence)

        // [시퀀스 1] 전투 종료 판정
        private async Task CheckBattleEndAsync()
        {
            IsPlayerTurn = false; // 턴 잠금

            if (PlayerHp <= 0)
            {
                IsBattleResolved = true;
                BattleMessage = Loc.T(IsGymBattle ? "battle.lost.gym" : "battle.lost.wild");
                Joy = Math.Max(0, Joy - 10);
                Energy = Math.Max(0, Energy - 20);

                IsDefeatedFadeOut = true;
                await Task.Delay(3000);

                IsDefeatedFadeOut = false;
                IsGymBattle = false;
                CloseBattle();
            }
            else if (EnemyHp <= 0)
            {
                IsBattleResolved = true;
                TrAtk = Math.Min(100, TrAtk + 5);
                Bond = Math.Min(100, Bond + 5);

                if (IsGymBattle)
                {
                    // 도전한 체육관 기준: 다음 차례 체육관이면 배지, 이미 깬 체육관이면 재대결 승리
                    var leader = GymManager.GetLeader(_selectedGymIndex);
                    if (leader != null && !_isGymRematch)
                    {
                        GymBadges++; Save();
                        BattleMessage = Loc.F("battle.gym.won", leader.LeaderName, leader.BadgeName);
                        await Task.Delay(3000);

                        if (TryGrantMegaRing())
                        {
                            BattleMessage = Loc.T("mega.ring.got");
                            await Task.Delay(3000);
                        }
                    }
                    else if (leader != null)
                    {
                        BattleMessage = Loc.F("battle.gym.rematchWon", leader.LeaderName);
                        await Task.Delay(3000);
                    }
                }
                else
                {
                    BattleMessage = Loc.T("battle.won");
                    await Task.Delay(2000);
                }

                // [시퀀스 2] 스킬 학습 단계로 이동
                CheckAndLearnSkillAfterVictory();
            }
        }

        // [시퀀스 2] 스킬 학습 처리
        public void CheckAndLearnSkillAfterVictory()
        {
            var activeMoves = SkillDex.GetActiveLearnset(SpeciesId);

            if (activeMoves.Count == 0)
            {
                ProceedToCatchOrEnd();
                return;
            }

            var availableSkills = activeMoves
                .Where(m => m.Level <= Level && !KnowsSkill(m.MoveId))
                .Select(m => new { m.Level, Skill = SkillDex.GetSkill(m.MoveId) })
                .Where(x => x.Skill != null)
                .ToList();

            if (availableSkills.Count > 0)
            {
                Random rand = new Random();
                var picked = availableSkills.OrderByDescending(x => x.Level).ThenBy(x => rand.Next()).Take(2).Select(x => x.Skill!).ToList();

                RecommendedSkill1 = picked.Count > 0 ? picked[0] : null;
                RecommendedSkill2 = picked.Count > 1 ? picked[1] : null;

                IsSkillLearnMenuOpen = true;
                BattleMessage = Loc.T("battle.newSkill");
            }
            else
            {
                ProceedToCatchOrEnd();
            }
        }

        // [시퀀스 3] 최종 분기점 (체육관 보상 vs 야생 포획)
        public async void ProceedToCatchOrEnd()
        {
            if (IsGymBattle)
            {
                // 체육관은 포획 없이 바로 보상 시퀀스 진입!
                await ProcessGymBattleRewardsAsync();
            }
            else
            {
                // 야생은 포획 선택 창 오픈!
                BattleMessage = Loc.T("battle.askCatch");
                IsCatchOffered = true;
            }
        }

        // [시퀀스 4-A] 체육관 전용 보상
        private async Task ProcessGymBattleRewardsAsync()
        {
            BattleMessage = Loc.T("battle.gym.prize");
            await Task.Delay(2000);

            AddItem(ItemDex.HyperPotion, 2);
            AddItem(ItemDex.MonsterBall, 3);

            BattleMessage = Loc.F("battle.gym.prizeItems", ItemDex.Get(ItemDex.HyperPotion).Name, ItemDex.Get(ItemDex.MonsterBall).Name);
            await Task.Delay(2500);

            string? shardMessage = GiveMegaShards(_isGymRematch ? GYM_REMATCH_SHARDS : GYM_SHARDS, new Random());
            if (shardMessage != null)
            {
                BattleMessage = shardMessage;
                await Task.Delay(2500);
            }

            Bond = Math.Min(100, Bond + 10);

            BattleMessage = Loc.F("battle.bondUp", Name);
            await Task.Delay(2500);

            IsGymBattle = false;
            CloseBattle();
        }

        // [시퀀스 4-B] 야생 포획 연출
        // [시퀀스 4-B] 야생 포획 연출 및 확률 계산
        public async Task ExecuteCatchResultAsync()
        {
            double hpPercent = (double)EnemyHp / EnemyMaxHp;
            int catchRate = 10;
            if (hpPercent <= 0.2) catchRate = 70;
            else if (hpPercent <= 0.5) catchRate = 35;

            Random rand = new Random();
            bool isCaught = rand.Next(100) < catchRate;

            if (isCaught)
            {
                UnlockPokemonInPokedex(EnemySpeciesId);

                var newMember = new PartyMember
                {
                    SpeciesId = EnemySpeciesId,
                    Name = EnemyName,
                    Level = EnemyLevel,
                    AgeMinutes = (EnemyLevel - 1) * MINUTES_PER_LEVEL,
                    IsShiny = IsEnemyShiny,
                    IsFemale = _isEnemyFemale,
                    IsGenderless = _isEnemyGenderless,
                    TrAtk = 0,
                    TrDef = 0,
                    TrSpeed = 0,
                    Weight = new Random().Next(10, 31), // 야생에서 막 온 포켓몬: 바로 퍼펙트 바디를 받지 않도록
                    Skills = (int[])EnemySkills.Clone(),
                    Genes = new PokemonGene(),
                    SpriteFileName = _enemySpriteFileName
                };

                if (Party != null && Party.Count >= 6)
                {
                    _pendingRetiree = newMember;
                    IsSwapMode = true;
                    SyncMainToLeader();
                    UpdatePartyFirstFlags();
                    IsPartyOpen = true;

                    BattleMessage = Loc.F("battle.caught.full", EnemyName);
                    await Task.Delay(2500);
                }
                else if (Party != null)
                {
                    Party.Add(newMember);
                    BattleMessage = Loc.F("battle.caught", EnemyName);
                    await Task.Delay(2000);
                }

                // 포획 성공 시 보상 획득 후 종료
                await ProcessWildBattleRewardsAsync();
            }
            else
            {
                IsEnemyVisible = true;
                BattleMessage = Loc.F("battle.brokeFree", EnemyName);
                await Task.Delay(2000);

                // 🌟 핵심 분기: 적의 체력에 따라 전투 중인지, 승리 후인지 판단합니다.
                if (EnemyHp <= 0)
                {
                    // [승리 후 포획 실패] 다시 포획/그냥가기 선택 창을 띄웁니다.
                    BattleMessage = Loc.T("battle.throwAgain");
                    IsCatchOffered = true;
                }
                else
                {
                    // [전투 중 포획 실패] 배틀이 안 끝났으므로 팝업 없이 적의 턴으로 넘깁니다!
                    await EnemyTurnAction(false);
                }
            }
        }

        // [시퀀스 4-C] 야생에서 도망가기
        public async void LeaveWildBattle()
        {
            IsCatchOffered = false;
            BattleMessage = Loc.F("battle.leave", EnemyName);
            await Task.Delay(1500);

            // 도망 후 야생 보상으로 이동
            await ProcessWildBattleRewardsAsync();
        }

        // [시퀀스 5] 야생 전용 보상 및 종료
        private async Task ProcessWildBattleRewardsAsync()
        {
            Random rand = new Random();
            string dropMessage = "";

            if (rand.Next(100) < 50)
            {
                int itemRoll = rand.Next(100);
                if (itemRoll < 50) { AddItem(ItemDex.MonsterBall, 1); dropMessage = Loc.F("battle.drop", ItemDex.Get(ItemDex.MonsterBall).Name); }
                else
                {
                    int potionRoll = rand.Next(100);
                    if (potionRoll < 60) { AddItem(ItemDex.Potion, 1); dropMessage = Loc.F("battle.drop", ItemDex.Get(ItemDex.Potion).Name); }
                    else if (potionRoll < 90) { AddItem(ItemDex.SuperPotion, 1); dropMessage = Loc.F("battle.drop", ItemDex.Get(ItemDex.SuperPotion).Name); }
                    else { AddItem(ItemDex.HyperPotion, 1); dropMessage = Loc.F("battle.drop.rare", ItemDex.Get(ItemDex.HyperPotion).Name); }
                }
            }

            if (!string.IsNullOrEmpty(dropMessage))
            {
                BattleMessage = dropMessage;
                await Task.Delay(2000);
            }

            if (rand.Next(100) < WILD_SHARD_CHANCE)
            {
                string? shardMessage = GiveMegaShards(1, rand);
                if (shardMessage != null)
                {
                    BattleMessage = shardMessage;
                    await Task.Delay(2000);
                }
            }

            CloseBattle();
        }

        #endregion

        #region 체육관 시스템 (Gym System)
        private int _gymBadges = 0;
        public int GymBadges { get => _gymBadges; set => SetProperty(ref _gymBadges, value); }

        private bool _isGymBattle = false;
        [JsonIgnore] public bool IsGymBattle { get => _isGymBattle; set => SetProperty(ref _isGymBattle, value); }

        private bool _isGymConfirmOpen;
        [JsonIgnore] public bool IsGymConfirmOpen { get => _isGymConfirmOpen; set => SetProperty(ref _isGymConfirmOpen, value); }

        private GymLeaderInfo? _selectedGymLeader;
        [JsonIgnore] public GymLeaderInfo? SelectedGymLeader { get => _selectedGymLeader; set { if (SetProperty(ref _selectedGymLeader, value)) OnPropertyChanged(nameof(GymChallengePrompt)); } }
        [JsonIgnore] public string GymChallengePrompt => Loc.F("gym.challenge.prompt", SelectedGymLeader?.GymName);

        private int _selectedGymIndex;
        private bool _isGymRematch; // 이미 배지를 받은 체육관에 다시 도전 중
        public void PromptGymChallenge(int gymIndex)
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;

            var leader = GymManager.GetLeader(gymIndex);
            if (leader == null || gymIndex > GymBadges) return;

            _selectedGymIndex = gymIndex;
            SelectedGymLeader = leader;
            IsGymConfirmOpen = true;
        }

        public void ConfirmGymChallenge() { IsGymConfirmOpen = false; StartSpecificGymBattle(_selectedGymIndex); }
        public void CancelGymChallenge() { IsGymConfirmOpen = false; }

        public async void StartSpecificGymBattle(int gymIndex)
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;

            var leader = GymManager.GetLeader(gymIndex);
            if (leader == null || gymIndex > GymBadges) return;

            _selectedGymIndex = gymIndex;
            _isGymRematch = gymIndex < GymBadges;

            IsProfileOpen = false; _isCounterReady = false;
            IsBattleResolved = false; IsCatchOffered = false; IsAttackMenuOpen = false; IsEnemyVisible = true;
            IsSkillLearnMenuOpen = false; IsSkillReplaceMenuOpen = false;
            IsGymBattle = true;

            PlayerAilment = SkillAilment.None;
            _playerAilmentTurns = 0;
            _playerBadPoisonTurnCount = 0;

            EnemyAilment = SkillAilment.None;
            _enemyAilmentTurns = 0;
            _enemyBadPoisonTurnCount = 0;

            EnemySpeciesId = leader.PokemonSpeciesId;
            EnemyLevel = leader.Level;
            PlayerMaxHp = CombatMaxHp; PlayerHp = PlayerMaxHp;

            var enemyInfo = PokemonDex.Get(EnemySpeciesId);
            int enemyBaseHp = enemyInfo != null ? enemyInfo.BaseHp : 50;

            EnemyMaxHp = (int)(BattleCalculator.MaxHp(enemyBaseHp, BattleCalculator.GYM_GENE, EnemyLevel) * BattleCalculator.GYM_BONUS);
            EnemyHp = EnemyMaxHp;

            EnemySkills = (int[])(leader.SpecificSkills?.Clone() ?? new int[4]);
            UpdateEnemyAnimation(0);

            IsBattleOpen = true; IsPlayerTurn = false;
            BattleMessage = Loc.F("battle.gym.intro", leader.LeaderName);

            await Task.Delay(2000);
            BattleMessage = Loc.T("battle.choose");
            IsPlayerTurn = true;
        }
        #endregion
    }
}