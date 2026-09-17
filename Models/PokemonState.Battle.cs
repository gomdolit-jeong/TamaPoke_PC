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
        [JsonIgnore]
        public int CombatMaxHp
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                int baseHp = p?.BaseHp ?? 50;
                return (((baseHp * 2 + Genes.HpGene + (TrDef / 4)) * Level) / 100) + Level + 10;
            }
        }

        [JsonIgnore]
        public int CombatAtk
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                return CalculateStat(p?.BaseAtk ?? 50, Genes.AtkGene, TrAtk, Level);
            }
        }

        [JsonIgnore]
        public int CombatDef
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                return CalculateStat(p?.BaseDef ?? 50, Genes.DefGene, TrDef, Level);
            }
        }

        [JsonIgnore]
        public int CombatSpeed
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                int baseSpe = p?.BaseSpeed ?? 50;
                return (((baseSpe * 2 + Genes.SpeGene + (TrSpeed / 4)) * Level) / 100) + 5;
            }
        }

        private int CalculateStat(int baseStat, int gene, int effortValue, int currentLevel)
        {
            return (((baseStat * 2 + gene + (effortValue / 4)) * currentLevel) / 100) + 5;
        }
        #endregion

        #region 배틀 시스템 UI 상태 (Battle System UI)
        private bool _isBattleOpen = false;
        public bool IsBattleOpen { get => _isBattleOpen; set { if (SetProperty(ref _isBattleOpen, value)) { OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

        private bool _isDefeatedFadeOut = false;
        public bool IsDefeatedFadeOut { get => _isDefeatedFadeOut; set => SetProperty(ref _isDefeatedFadeOut, value); }

        private bool _isPlayerTurn = true;
        public bool IsPlayerTurn
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
        public bool IsBattleResolved { get => _isBattleResolved; set { if (SetProperty(ref _isBattleResolved, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isCatchOffered = false;
        public bool IsCatchOffered { get => _isCatchOffered; set { if (SetProperty(ref _isCatchOffered, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isAttackMenuOpen = false;
        public bool IsAttackMenuOpen { get => _isAttackMenuOpen; set { if (SetProperty(ref _isAttackMenuOpen, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isBattleInventoryOpen = false;
        public bool IsBattleInventoryOpen
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
        public bool IsEnemyVisible { get => _isEnemyVisible; set => SetProperty(ref _isEnemyVisible, value); }

        private int _enemySpeciesId = 1;
        public int EnemySpeciesId { get => _enemySpeciesId; set { if (SetProperty(ref _enemySpeciesId, value)) OnPropertyChanged(nameof(EnemyName)); } }

        [JsonIgnore] public string EnemyName => PokemonDex.GetName(EnemySpeciesId);

        private int _enemyLevel = 1;
        public int EnemyLevel { get => _enemyLevel; set => SetProperty(ref _enemyLevel, value); }

        private int _playerHp = 100;
        public int PlayerHp { get => _playerHp; set { if (SetProperty(ref _playerHp, value)) OnPropertyChanged(nameof(PlayerHpColor)); } }
        private int _playerMaxHp = 100;
        public int PlayerMaxHp { get => _playerMaxHp; set { if (SetProperty(ref _playerMaxHp, value)) OnPropertyChanged(nameof(PlayerHpColor)); } }

        private int _enemyHp = 100;
        public int EnemyHp { get => _enemyHp; set { if (SetProperty(ref _enemyHp, value)) OnPropertyChanged(nameof(EnemyHpColor)); } }
        private int _enemyMaxHp = 100;
        public int EnemyMaxHp { get => _enemyMaxHp; set { if (SetProperty(ref _enemyMaxHp, value)) OnPropertyChanged(nameof(EnemyHpColor)); } }

        [JsonIgnore] public System.Windows.Media.Brush PlayerHpColor => GetHpColor(PlayerHp, PlayerMaxHp);
        [JsonIgnore] public System.Windows.Media.Brush EnemyHpColor => GetHpColor(EnemyHp, EnemyMaxHp);

        private System.Windows.Media.Brush GetHpColor(int hp, int maxHp)
        {
            if (maxHp <= 0) return System.Windows.Media.Brushes.Green;
            double ratio = (double)hp / maxHp;
            if (ratio > 0.5) return (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#4CAF50")!;
            if (ratio > 0.2) return (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#FBC02D")!;
            return (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E53935")!;
        }

        private bool _isPlayerTakingDamage = false;
        public bool IsPlayerTakingDamage { get => _isPlayerTakingDamage; set => SetProperty(ref _isPlayerTakingDamage, value); }
        private bool _isEnemyTakingDamage = false;
        public bool IsEnemyTakingDamage { get => _isEnemyTakingDamage; set => SetProperty(ref _isEnemyTakingDamage, value); }

        private string _battleMessage = "";
        public string BattleMessage { get => _battleMessage; set => SetProperty(ref _battleMessage, value); }

        private bool _isCounterReady = false;
        public int[] EnemySkills { get; set; } = new int[4] { 0, 0, 0, 0 };

        private void GenerateEnemySkills()
        {
            for (int i = 0; i < 4; i++) EnemySkills[i] = 0;
            var d = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
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
            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            var myInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
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
                    score = 5;
                }
                else
                {
                    PokemonType myType1 = myInfo?.Type1 ?? PokemonType.Normal;
                    PokemonType myType2 = myInfo?.Type2 ?? PokemonType.None;

                    double typeMult = TypeMatchupHelper.GetMultiplier(m.Type, myType1) * (myType2 != PokemonType.None ? TypeMatchupHelper.GetMultiplier(m.Type, myType2) : 1.0);
                    double stabMult = (enemyInfo != null && (enemyInfo.Type1 == m.Type || enemyInfo.Type2 == m.Type)) ? 1.5 : 1.0;

                    int enemyAtk = (((enemyInfo?.BaseAtk ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                    if (m.Category == SkillCategory.Special) enemyAtk = (((enemyInfo?.BaseSpA ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;

                    int expectedDamage = (int)(Math.Max(2, enemyAtk - (CombatDef / 2)) * typeMult * stabMult * (m.Power / 50.0));

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
        public SkillAilment PlayerAilment
        {
            get => _playerAilment;
            set
            {
                if (SetProperty(ref _playerAilment, value))
                    OnPropertyChanged(nameof(PlayerAilmentUI));
            }
        }
        [JsonIgnore] public string PlayerAilmentUI => PlayerAilment != SkillAilment.None ? AilmentHelper.GetAilmentIcon(PlayerAilment) : "";
        private int _playerAilmentTurns = 0;
        private int _playerBadPoisonTurnCount = 0;

        private SkillAilment _enemyAilment = SkillAilment.None;
        public SkillAilment EnemyAilment
        {
            get => _enemyAilment;
            set
            {
                if (SetProperty(ref _enemyAilment, value))
                    OnPropertyChanged(nameof(EnemyAilmentUI));
            }
        }
        [JsonIgnore] public string EnemyAilmentUI => EnemyAilment != SkillAilment.None ? AilmentHelper.GetAilmentIcon(EnemyAilment) : "";
        private int _enemyAilmentTurns = 0;
        private int _enemyBadPoisonTurnCount = 0;

        private bool _isEnemyShiny = false;
        public bool IsEnemyShiny { get => _isEnemyShiny; set => SetProperty(ref _isEnemyShiny, value); }

        private bool _isEnemyFemale = false;
        private string _enemySpriteFileName = "";

        private void DetermineEnemyGender(int speciesId, Random rand)
        {
            var pInfo = PokemonDex.AllPokemons.FirstOrDefault(p => p.Id == speciesId);
            int rate = pInfo != null ? pInfo.GenderRate : -1;

            if (rate == -1 || rate == 0) _isEnemyFemale = false;
            else if (rate == 8) _isEnemyFemale = true;
            else _isEnemyFemale = rand.Next(8) < rate;
        }

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
            if (rand.Next(100) < 1) { EnemySpeciesId = LegendaryIds[rand.Next(LegendaryIds.Length)]; }
            else { var ids = PokemonDex.AllPokemons.Where(p => p.Id <= 493 && !LegendaryIds.Contains(p.Id)).Select(p => p.Id).ToList(); EnemySpeciesId = ids.Count > 0 ? ids[rand.Next(ids.Count)] : 1; }

            EnemyLevel = Math.Max(1, Level + rand.Next(-2, 3));
            PlayerMaxHp = CombatMaxHp; PlayerHp = PlayerMaxHp;

            IsEnemyShiny = rand.Next(100) < 1;
            DetermineEnemyGender(EnemySpeciesId, rand);

            string assetFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites");
            string searchPattern = $"p{EnemySpeciesId:D4}*.bin";
            string[] allForms = System.IO.Directory.Exists(assetFolderPath) ? System.IO.Directory.GetFiles(assetFolderPath, searchPattern) : Array.Empty<string>();

            string baseFileName = $"p{EnemySpeciesId:D4}.bin";
            _enemySpriteFileName = baseFileName;

            if (allForms.Length > 1 && rand.Next(100) < 10)
            {
                var specialForms = allForms.Where(f =>
                    System.IO.Path.GetFileName(f) != baseFileName &&
                    !System.IO.Path.GetFileName(f).ToLower().Contains("shiny") &&
                    !System.IO.Path.GetFileName(f).ToLower().Contains("female")).ToArray();

                if (specialForms.Length > 0) _enemySpriteFileName = System.IO.Path.GetFileName(specialForms[rand.Next(specialForms.Length)]);
            }

            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(_enemySpriteFileName);
            bool tryShiny = IsEnemyShiny && !fileNameWithoutExt.ToLower().Contains("shiny");
            bool tryFemale = _isEnemyFemale && !fileNameWithoutExt.ToLower().Contains("female");
            string? finalMatch = null;

            if (tryShiny && tryFemale)
            {
                finalMatch = allForms.FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny_Female", StringComparison.OrdinalIgnoreCase) || System.IO.Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female_Shiny", StringComparison.OrdinalIgnoreCase))
                             ?? allForms.FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
            }
            else if (tryFemale) finalMatch = allForms.FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female", StringComparison.OrdinalIgnoreCase));
            else if (tryShiny) finalMatch = allForms.FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));

            if (finalMatch != null) _enemySpriteFileName = System.IO.Path.GetFileName(finalMatch);

            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            int enemyBaseHp = enemyInfo != null ? enemyInfo.BaseHp : 50;
            EnemyMaxHp = ((enemyBaseHp * 2 + 15) * EnemyLevel / 100) + EnemyLevel + 10;
            EnemyHp = EnemyMaxHp;

            GenerateEnemySkills();
            UpdateEnemyAnimation(0);

            IsBattleOpen = true; IsPlayerTurn = false;
            BattleMessage = IsEnemyShiny ? $"앗! 색이 다른 야생 {EnemyName}이(가) 나타났다!" : $"앗! 야생 {EnemyName}이(가) 나타났다!";

            if (Settings.UseTrayNotifications) TrayNotificationRequested?.Invoke("야생 포켓몬 출현!", "⚔️");

            await Task.Delay(1500);
            BattleMessage = "행동을 선택하세요.";
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
                bool canAct = AilmentHelper.CanActThisTurn(PlayerAilment, ref _playerAilmentTurns, Name, out string statusMsg);

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
                BattleMessage = "무사히 도망쳤다!";
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
                    BattleMessage = "적의 공격을 피할 준비를 했다!\n(카운터 대기)";
                }
                else BattleMessage = "회피 준비에 실패했다...";

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

            var myInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);

            BattleMessage = $"{Name}의 {playerSkill.Name}!";
            await Task.Delay(1000);

            bool isHit = playerSkill.Accuracy == 0 || rand.Next(100) < playerSkill.Accuracy;

            if (isHit && playerSkill.Power > 0)
            {
                PokemonType enemyType1 = enemyInfo?.Type1 ?? PokemonType.Normal;
                PokemonType enemyType2 = enemyInfo?.Type2 ?? PokemonType.None;

                double typeMultiplier = TypeMatchupHelper.GetMultiplier(playerSkill.Type, enemyType1) * (enemyType2 != PokemonType.None ? TypeMatchupHelper.GetMultiplier(playerSkill.Type, enemyType2) : 1.0);
                double stabMultiplier = (myInfo != null && (myInfo.Type1 == playerSkill.Type || myInfo.Type2 == playerSkill.Type)) ? 1.5 : 1.0;

                int enemyCombatDef = (((enemyInfo?.BaseDef ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                int myCombatAtk = CombatAtk;

                if (playerSkill.Category == SkillCategory.Special)
                {
                    enemyCombatDef = (((enemyInfo?.BaseSpD ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                    myCombatAtk = (((myInfo?.BaseSpA ?? 50) * 2 + (Genes.AtkGene - 80)) * Level / 100) + 5 + TrAtk;
                }

                if (IsGymBattle) enemyCombatDef = (int)(enemyCombatDef * 1.1);

                int baseDamage = Math.Max(2, myCombatAtk - (enemyCombatDef / 2));
                int damage = (int)(baseDamage * typeMultiplier * stabMultiplier * (playerSkill.Power / 50.0));

                if (_isCounterReady) { damage *= 2; _isCounterReady = false; }
                damage = Math.Max(1, damage);

                IsEnemyTakingDamage = true;
                EnemyHp = Math.Max(0, EnemyHp - damage);

                string extraMsg = typeMultiplier >= 2.0 ? "효과가 굉장했다!\n" : (typeMultiplier > 0 && typeMultiplier <= 0.5 ? "효과가 별로인 듯하다...\n" : (typeMultiplier == 0 ? "효과가 없는 것 같다...\n" : ""));
                BattleMessage = $"{extraMsg}적에게 {damage} 데미지를 입혔다!";

                if (playerSkill.Ailment != SkillAilment.None && EnemyAilment == SkillAilment.None)
                {
                    if (rand.Next(100) < playerSkill.AilmentChance)
                    {
                        EnemyAilment = playerSkill.Ailment;
                        _enemyAilmentTurns = AilmentHelper.GetInitialTurns(EnemyAilment);
                        _enemyBadPoisonTurnCount = 1;

                        await Task.Delay(1000);
                        string ailmentIcon = AilmentHelper.GetAilmentIcon(EnemyAilment);
                        BattleMessage = $"야생 {EnemyName}은(는) {ailmentIcon} 상태가 되었다!";
                        await Task.Delay(1500);
                    }
                }

                if (playerSkill.Category == SkillCategory.Physical) _tempActionId = 1;
                else if (playerSkill.Category == SkillCategory.Special) _tempActionId = 21;
                else _tempActionId = 17;

                _tempActionTimer = 15; UpdateAnimation(_tempActionId);
                _enemyTempActionTimer = 15; UpdateEnemyAnimation(6);

                await Task.Delay(600); IsEnemyTakingDamage = false; await Task.Delay(900);
            }
            else if (!isHit)
            {
                _isCounterReady = false;
                BattleMessage = $"{Name}의 공격은 빗나갔다!";
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

                BattleMessage = $"{Name}에게 {selectedItem.Name}을(를) 사용했다!\n체력이 {healAmount} 회복되었다!";

                _tempActionId = 10; _tempActionTimer = 30; CheckStateAndAnimate();
                await Task.Delay(1500);

                await EnemyTurnAction(false);
            }
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
                    BattleMessage = $"{Name}(은)는 {ailmentMsg} ({tickDamage} 데미지)";
                    await Task.Delay(1500);
                }
            }
        }

        private async Task EnemyTurnAction(bool playerDodged)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || IsBattleResolved) return;

            var myInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            Random rand = new Random();

            if (EnemyAilment != SkillAilment.None)
            {
                if (_enemyAilmentTurns > 0) _enemyAilmentTurns--;

                bool canAct = AilmentHelper.CanActThisTurn(EnemyAilment, ref _enemyAilmentTurns, $"야생 {EnemyName}", out string statusMsg);
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

                    BattleMessage = "행동을 선택하세요.";
                    IsPlayerTurn = true;
                    return;
                }
            }

            SkillInfo enemySkill = ChooseEnemySkill();
            BattleMessage = $"야생 {EnemyName}의 {enemySkill.Name}!";
            await Task.Delay(1000);

            bool enemyHits = enemySkill.Accuracy == 0 || rand.Next(100) < enemySkill.Accuracy;

            if (!playerDodged && enemyHits && enemySkill.Power > 0)
            {
                PokemonType myType1 = myInfo?.Type1 ?? PokemonType.Normal;
                PokemonType myType2 = myInfo?.Type2 ?? PokemonType.None;

                double typeMultiplier = TypeMatchupHelper.GetMultiplier(enemySkill.Type, myType1) * (myType2 != PokemonType.None ? TypeMatchupHelper.GetMultiplier(enemySkill.Type, myType2) : 1.0);
                double stabMultiplier = (enemyInfo != null && (enemyInfo.Type1 == enemySkill.Type || enemyInfo.Type2 == enemySkill.Type)) ? 1.5 : 1.0;

                int enemyCombatAtk = (((enemyInfo?.BaseAtk ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                int myCombatDef = CombatDef;

                if (enemySkill.Category == SkillCategory.Special)
                {
                    enemyCombatAtk = (((enemyInfo?.BaseSpA ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                    myCombatDef = (((myInfo?.BaseSpD ?? 50) * 2 + (Genes.DefGene - 80)) * Level / 100) + 5 + (TrDef / 2);
                }

                if (IsGymBattle) enemyCombatAtk = (int)(enemyCombatAtk * 1.1);

                int baseEnemyDamage = Math.Max(2, enemyCombatAtk - (myCombatDef / 2));
                int enemyDamage = (int)(baseEnemyDamage * typeMultiplier * stabMultiplier * (enemySkill.Power / 50.0));
                enemyDamage = Math.Max(1, enemyDamage);

                IsPlayerTakingDamage = true;
                PlayerHp = Math.Max(0, PlayerHp - enemyDamage);

                string extraMsg = typeMultiplier >= 2.0 ? "효과가 굉장했다!\n" : (typeMultiplier > 0 && typeMultiplier <= 0.5 ? "효과가 별로인 듯하다...\n" : (typeMultiplier == 0 ? "효과가 없는 것 같다...\n" : ""));
                BattleMessage = $"{extraMsg}{Name}(은)는 {enemyDamage} 데미지를 입었다!";

                if (enemySkill.Ailment != SkillAilment.None && PlayerAilment == SkillAilment.None)
                {
                    if (rand.Next(100) < enemySkill.AilmentChance)
                    {
                        PlayerAilment = enemySkill.Ailment;
                        _playerAilmentTurns = AilmentHelper.GetInitialTurns(PlayerAilment);
                        _playerBadPoisonTurnCount = 1;

                        await Task.Delay(1000);
                        string ailmentIcon = AilmentHelper.GetAilmentIcon(PlayerAilment);
                        BattleMessage = $"{Name}(은)는 {ailmentIcon} 상태가 되었다!";
                        await Task.Delay(1500);
                    }
                }

                _enemyTempActionId = 1; _enemyTempActionTimer = 15; UpdateEnemyAnimation(1);
                _tempActionId = 6; _tempActionTimer = 15; UpdateAnimation(6);

                await Task.Delay(600); IsPlayerTakingDamage = false; await Task.Delay(900);
            }
            else if (playerDodged) { BattleMessage = $"{Name}(은)는 공격을 멋지게 피했다!"; await Task.Delay(1500); }
            else if (!enemyHits) { BattleMessage = $"야생 {EnemyName}의 공격은 빗나갔다!"; await Task.Delay(1500); }

            await ProcessEnemyTurnEndAilment();
            await ProcessPlayerTurnEndAilment();

            if (PlayerHp <= 0 || EnemyHp <= 0) { await CheckBattleEndAsync(); return; }

            BattleMessage = "행동을 선택하세요.";
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
                    BattleMessage = $"야생 {EnemyName}은(는) {ailmentMsg} ({tickDamage} 데미지)";
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
                BattleMessage = IsGymBattle ? "관장에게 패배했습니다...\n수행이 더 필요합니다." : "눈앞이 깜깜해졌다...\n배틀에서 패배했습니다.";
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
                    var leader = GymManager.GetLeader(GymBadges);
                    if (leader != null)
                    {
                        GymBadges++; Save();
                        BattleMessage = $"대단한 승부였다!\n{leader.LeaderName}에게서\n[{leader.BadgeName}]을(를) 얻었다!";
                        await Task.Delay(3000);
                    }
                }
                else
                {
                    BattleMessage = "배틀에서 승리했다!";
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
                BattleMessage = "실전 경험을 통해 새로운 스킬을 떠올렸다!\n어떤 스킬을 배울까?";
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
                BattleMessage = "야생 포켓몬을 포획하시겠습니까?";
                IsCatchOffered = true;
            }
        }

        // [시퀀스 4-A] 체육관 전용 보상
        private async Task ProcessGymBattleRewardsAsync()
        {
            BattleMessage = "관장에게 승리하여\n상품을 획득했습니다!";
            await Task.Delay(2000);

            AddItemToInventory("고급상처약", "체력의 50% 회복", ItemType.Potion, 50, 2);
            AddItemToInventory("몬스터볼", "야생 포켓몬 포획", ItemType.monsterball, 0, 3);

            BattleMessage = "고급상처약 2개와\n몬스터볼 3개를 얻었다!";
            await Task.Delay(2500);

            Bond = Math.Min(100, Bond + 10);

            BattleMessage = $"{Name}와(과)의 유대감이 깊어졌다!";
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
                    TrAtk = 0,
                    TrDef = 0,
                    TrSpeed = 0,
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

                    BattleMessage = $"{EnemyName}을(를) 잡았지만 파티가 꽉 찼다!\n바꿀 포켓몬을 선택해 주세요.";
                    await Task.Delay(2500);
                }
                else if (Party != null)
                {
                    Party.Add(newMember);
                    BattleMessage = $"신난다! {EnemyName}을(를) 잡았다!\n파티에 합류했습니다.";
                    await Task.Delay(2000);
                }

                // 포획 성공 시 보상 획득 후 종료
                await ProcessWildBattleRewardsAsync();
            }
            else
            {
                IsEnemyVisible = true;
                BattleMessage = $"아아! {EnemyName}이(가) 볼에서 빠져나왔다!";
                await Task.Delay(2000);

                // 🌟 핵심 분기: 적의 체력에 따라 전투 중인지, 승리 후인지 판단합니다.
                if (EnemyHp <= 0)
                {
                    // [승리 후 포획 실패] 다시 포획/그냥가기 선택 창을 띄웁니다.
                    BattleMessage = "포켓볼을 더 던지겠습니까?";
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
            BattleMessage = $"{EnemyName}을(를) 뒤로하고 길을 떠납니다...";
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
                if (itemRoll < 50) { AddItemToInventory("몬스터볼", "야생 포켓몬을 잡을 때 쓴다.", ItemType.monsterball, 1, 1); dropMessage = "몬스터볼 1개를 얻었다!"; }
                else
                {
                    int potionRoll = rand.Next(100);
                    if (potionRoll < 60) { AddItemToInventory("상처약", "포켓몬의 체력을 15% 회복한다.", ItemType.Potion, 15, 1); dropMessage = "상처약 1개를 얻었다!"; }
                    else if (potionRoll < 90) { AddItemToInventory("좋은상처약", "포켓몬의 체력을 30% 회복한다.", ItemType.Potion, 30, 1); dropMessage = "좋은상처약 1개를 얻었다!"; }
                    else { AddItemToInventory("고급상처약", "포켓몬의 체력을 50% 회복한다.", ItemType.Potion, 50, 1); dropMessage = "앗! 고급상처약 1개를 얻었다!"; }
                }
            }

            if (!string.IsNullOrEmpty(dropMessage))
            {
                BattleMessage = dropMessage;
                await Task.Delay(2000);
            }

            CloseBattle();
        }

        #endregion

        #region 체육관 시스템 (Gym System)
        private int _gymBadges = 0;
        public int GymBadges { get => _gymBadges; set => SetProperty(ref _gymBadges, value); }

        private bool _isGymBattle = false;
        public bool IsGymBattle { get => _isGymBattle; set => SetProperty(ref _isGymBattle, value); }

        private bool _isGymConfirmOpen;
        public bool IsGymConfirmOpen { get => _isGymConfirmOpen; set => SetProperty(ref _isGymConfirmOpen, value); }

        private GymLeaderInfo? _selectedGymLeader;
        public GymLeaderInfo? SelectedGymLeader { get => _selectedGymLeader; set => SetProperty(ref _selectedGymLeader, value); }

        private int _selectedGymIndex;

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
            if (leader == null) return;

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

            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            int enemyBaseHp = enemyInfo != null ? enemyInfo.BaseHp : 50;

            int baseGymHp = (((enemyBaseHp * 2 + 31) * EnemyLevel) / 100) + EnemyLevel + 10;
            EnemyMaxHp = (int)(baseGymHp * 1.1);
            EnemyHp = EnemyMaxHp;

            EnemySkills = (int[])(leader.SpecificSkills?.Clone() ?? new int[4]);
            UpdateEnemyAnimation(0);

            IsBattleOpen = true; IsPlayerTurn = false;
            BattleMessage = $"체육관 관장 {leader.LeaderName}이(가)\n승부를 걸어왔다!";

            await Task.Delay(2000);
            BattleMessage = "행동을 선택하세요.";
            IsPlayerTurn = true;
        }
        #endregion
    }
}