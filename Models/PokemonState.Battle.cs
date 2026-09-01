using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 실전 전투 능력치 계산 (Combat Stats)
        [JsonIgnore]
        public int CombatMaxHp
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                return (((p?.BaseHp ?? 50) * 2 + (Genes.HpGene - 80)) * Level / 100) + Level + 10 + (TrDef / 2);
            }
        }

        [JsonIgnore]
        public int CombatAtk
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                return (((p?.BaseAtk ?? 50) * 2 + (Genes.AtkGene - 80)) * Level / 100) + 5 + TrAtk;
            }
        }

        [JsonIgnore]
        public int CombatDef
        {
            get
            {
                var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
                return (((p?.BaseDef ?? 50) * 2 + (Genes.DefGene - 80)) * Level / 100) + 5 + TrDef;
            }
        }
        #endregion

        #region 배틀 시스템 UI 상태 (Battle System UI)
        private bool _isBattleOpen = false;
        public bool IsBattleOpen { get => _isBattleOpen; set { if (SetProperty(ref _isBattleOpen, value)) { OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

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

        // 🌟 개발자님의 자동화 시스템!
        [JsonIgnore] public bool IsMainMenuVisible => !IsCatchOffered && !IsBattleResolved && !IsAttackMenuOpen && !IsInventoryOpen && IsPlayerTurn;
        [JsonIgnore] public bool IsAttackMenuVisible => !IsCatchOffered && !IsBattleResolved && IsAttackMenuOpen && !IsInventoryOpen && IsPlayerTurn;

        private bool _isBattleResolved = false;
        public bool IsBattleResolved { get => _isBattleResolved; set { if (SetProperty(ref _isBattleResolved, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isCatchOffered = false;
        public bool IsCatchOffered { get => _isCatchOffered; set { if (SetProperty(ref _isCatchOffered, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        private bool _isAttackMenuOpen = false;
        public bool IsAttackMenuOpen { get => _isAttackMenuOpen; set { if (SetProperty(ref _isAttackMenuOpen, value)) { OnPropertyChanged(nameof(IsMainMenuVisible)); OnPropertyChanged(nameof(IsAttackMenuVisible)); } } }

        // 🌟 가방이 열렸을 때 IsBattleInventoryOpen 상태를 갱신하도록 신호(OnPropertyChanged)를 추가했습니다!
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

        [JsonIgnore] public Brush PlayerHpColor => GetHpColor(PlayerHp, PlayerMaxHp);
        [JsonIgnore] public Brush EnemyHpColor => GetHpColor(EnemyHp, EnemyMaxHp);

        private Brush GetHpColor(int hp, int maxHp)
        {
            if (maxHp <= 0) return Brushes.Green;
            double ratio = (double)hp / maxHp;
            if (ratio > 0.5) return (Brush)new BrushConverter().ConvertFrom("#4CAF50")!;
            if (ratio > 0.2) return (Brush)new BrushConverter().ConvertFrom("#FBC02D")!;
            return (Brush)new BrushConverter().ConvertFrom("#E53935")!;
        }

        private bool _isPlayerTakingDamage = false;
        public bool IsPlayerTakingDamage { get => _isPlayerTakingDamage; set => SetProperty(ref _isPlayerTakingDamage, value); }
        private bool _isEnemyTakingDamage = false;
        public bool IsEnemyTakingDamage { get => _isEnemyTakingDamage; set => SetProperty(ref _isEnemyTakingDamage, value); }

        private string _battleMessage = "";
        public string BattleMessage { get => _battleMessage; set => SetProperty(ref _battleMessage, value); }

        private int _restUsesLeft = 2;
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
                bool tmPass = pass == 1;
                for (int i = 0; i < n; i++)
                {
                    int at = SkillDex.GetLearnLevel(EnemySpeciesId, i);
                    if (at > EnemyLevel) continue;
                    if (tmPass != (at == 0)) continue;

                    int mv = SkillDex.GetLearnMove(EnemySpeciesId, i);
                    if (mv == 0 || EnemySkills.Contains(mv)) continue;
                    var m = SkillDex.GetSkill(mv);
                    if (m == null || (tmPass && EnemyLevel < 40)) continue;

                    int sc = (m.Category == SkillCategory.Status) ? 10 : m.Power + 20;
                    if (m.Category != SkillCategory.Status && (m.Type == d.Type1 || m.Type == d.Type2)) sc += 40;
                    sc += at;

                    int slot = -1;
                    for (int s = 0; s < 4; s++) { if (sc > score[s]) { slot = s; break; } }
                    if (slot < 0) continue;
                    for (int s = 3; s > slot; s--) { score[s] = score[s - 1]; EnemySkills[s] = EnemySkills[s - 1]; }
                    score[slot] = sc; EnemySkills[slot] = mv;
                }
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

                    if (m.Effect == SkillEffect.Recharge) score -= expectedDamage / 4;
                    if (m.Effect == SkillEffect.Recoil) score -= expectedDamage / 6;
                }

                score += rand.Next(0, 5);

                if (score > bestScore) { bestScore = score; bestSkill = m; }
            }
            return bestSkill;
        }
        #endregion

        #region 포켓몬 스킬 시스템 (Skills)
        private int[] _skills = new int[4] { 0, 0, 0, 0 };
        public int[] Skills
        {
            get => _skills;
            set => SetProperty(ref _skills, value);
        }

        public bool KnowsSkill(int skillId) => skillId != 0 && Array.Exists(Skills, s => s == skillId);
        [JsonIgnore] public int SkillCount => Skills.Count(s => s != 0);

        public void RelearnFromLevel()
        {
            for (int i = 0; i < 4; i++) Skills[i] = 0;

            if (IsEgg)
            {
                OnPropertyChanged(nameof(CurrentSkills));
                return;
            }

            var d = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            if (d == null) return;

            int lvl = Level;
            int n = SkillDex.GetLearnCount(SpeciesId);

            if (n == 0)
            {
                Skills[0] = 1;
                int fallbackMove = SkillDex.GetFallbackMove(d.Type1);
                if (fallbackMove != 1) Skills[1] = fallbackMove;

                OnPropertyChanged(nameof(CurrentSkills));
                return;
            }

            int[] score = new int[4] { 0, 0, 0, 0 };

            for (int pass = 0; pass < 2; pass++)
            {
                bool tmPass = (pass == 1);
                for (int i = 0; i < n; i++)
                {
                    int at = SkillDex.GetLearnLevel(SpeciesId, i);
                    if (at > lvl) continue;
                    if (tmPass != (at == 0)) continue;

                    int mv = SkillDex.GetLearnMove(SpeciesId, i);
                    if (mv == 0 || mv >= SkillDex.MoveTable.Length || KnowsSkill(mv)) continue;

                    var m = SkillDex.GetSkill(mv);
                    if (m == null) continue;

                    if (tmPass && lvl < 40) continue;

                    int sc = (m.Category == SkillCategory.Status) ? 10 : m.Power + 20;

                    if (m.Category != SkillCategory.Status && (m.Type == d.Type1 || m.Type == d.Type2)) sc += 40;

                    if (m.Effect == SkillEffect.Recharge) sc -= 35;
                    if (m.Effect == SkillEffect.Recoil) sc -= 20;

                    sc += at;
                    if (sc < 1) sc = 1;

                    int slot = -1;
                    for (int s = 0; s < 4; s++)
                    {
                        if (sc > score[s]) { slot = s; break; }
                    }
                    if (slot < 0) continue;

                    for (int s = 3; s > slot; s--)
                    {
                        score[s] = score[s - 1];
                        Skills[s] = Skills[s - 1];
                    }
                    score[slot] = sc;
                    Skills[slot] = mv;
                }
                if (SkillCount >= 4) break;
            }

            bool hasStab = false;
            for (int i = 0; i < 4; i++)
            {
                if (Skills[i] == 0) continue;
                var m = SkillDex.GetSkill(Skills[i]);
                if (m != null && m.Category != SkillCategory.Status && (m.Type == d.Type1 || m.Type == d.Type2))
                {
                    hasStab = true;
                    break;
                }
            }

            if (!hasStab)
            {
                int best = 0;
                int bestSc = 0;
                for (int i = 0; i < n; i++)
                {
                    int at = SkillDex.GetLearnLevel(SpeciesId, i);
                    if (at > lvl) continue;
                    int mv = SkillDex.GetLearnMove(SpeciesId, i);
                    var m = SkillDex.GetSkill(mv);

                    if (m == null || m.Category == SkillCategory.Status || (m.Type != d.Type1 && m.Type != d.Type2)) continue;
                    if (at == 0 && lvl < 40) continue;

                    int sc = m.Power;
                    if (m.Effect == SkillEffect.Recharge) sc -= 35;
                    if (m.Effect == SkillEffect.Recoil) sc -= 20;

                    if (sc > bestSc) { bestSc = sc; best = mv; }
                }
                if (best != 0) Skills[3] = best;
            }

            OnPropertyChanged(nameof(CurrentSkills));
        }

        [JsonIgnore]
        public ObservableCollection<SkillInfo> CurrentSkills
        {
            get
            {
                var list = new ObservableCollection<SkillInfo>();
                foreach (int skillId in Skills)
                {
                    if (skillId != 0)
                    {
                        var skill = SkillDex.GetSkill(skillId);
                        if (skill != null) list.Add(skill);
                    }
                }
                return list;
            }
        }
        #endregion

        #region 가방(인벤토리) 시스템 (Bag System)

        public ObservableCollection<ItemInfo> Inventory { get; set; } = new ObservableCollection<ItemInfo>();

        public void InitializeInventory()
        {
            Inventory.Clear();

            Inventory.Add(new ItemInfo
            {
                Name = "몬스터볼",
                Description = "야생 포켓몬을 잡을 때 쓴다.",
                Type = ItemType.monsterball,
                EffectValue = 1,
                Quantity = 5
            });

            Inventory.Add(new ItemInfo
            {
                Name = "상처약",
                Description = "포켓몬의 체력을 20 회복한다.",
                Type = ItemType.Potion,
                EffectValue = 20,
                Quantity = 3
            });
        }

        public void AddItemToInventory(ItemType type, int amount)
        {
            var existingItem = Inventory.FirstOrDefault(i => i.Type == type);
            if (existingItem != null)
            {
                existingItem.Quantity += amount;
            }
            else
            {
                if (type == ItemType.monsterball)
                    Inventory.Add(new ItemInfo { Name = "몬스터볼", Description = "야생 포켓몬을 잡을 때 쓴다.", Type = ItemType.monsterball, EffectValue = 1, Quantity = amount });
                else if (type == ItemType.Potion)
                    Inventory.Add(new ItemInfo { Name = "상처약", Description = "포켓몬의 체력을 20 회복한다.", Type = ItemType.Potion, EffectValue = 20, Quantity = amount });
            }
        }
        #endregion


        #region 배틀 로직 (Battle Logic)
        public async void StartWildBattle()
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;

            IsProfileOpen = false; _restUsesLeft = 2; _isCounterReady = false;
            IsBattleResolved = false; IsCatchOffered = false; IsAttackMenuOpen = false; IsEnemyVisible = true;

            Random rand = new Random();
            if (rand.Next(100) < 1) { EnemySpeciesId = LegendaryIds[rand.Next(LegendaryIds.Length)]; }
            else { var ids = PokemonDex.AllPokemons.Where(p => p.Id <= 493 && !LegendaryIds.Contains(p.Id)).Select(p => p.Id).ToList(); EnemySpeciesId = ids.Count > 0 ? ids[rand.Next(ids.Count)] : 1; }

            EnemyLevel = Math.Max(1, Level + rand.Next(-2, 3));
            PlayerMaxHp = CombatMaxHp; PlayerHp = PlayerMaxHp;

            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            int enemyBaseHp = enemyInfo != null ? enemyInfo.BaseHp : 50;
            EnemyMaxHp = ((enemyBaseHp * 2 + 15) * EnemyLevel / 100) + EnemyLevel + 10;
            EnemyHp = EnemyMaxHp;

            GenerateEnemySkills();
            UpdateEnemyAnimation(ANIM_IDLE);

            IsBattleOpen = true;
            IsPlayerTurn = false;
            BattleMessage = $"앗! 야생 {EnemyName}이(가) 나타났다!";

            await Task.Delay(1500);

            BattleMessage = "행동을 선택하세요.";
            IsPlayerTurn = true;
        }

        public void CloseBattle() { IsBattleOpen = false; BattleMessage = ""; }
        public void LeaveWildBattle() { BattleMessage = $"{EnemyName}을(를) 뒤로하고 길을 떠납니다..."; IsCatchOffered = false; CloseBattle(); }

        public async Task ExecuteTurnAsync(BattleAction playerAction)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || !IsPlayerTurn) return;
            IsPlayerTurn = false; Random rand = new Random();

            if (playerAction == BattleAction.Run) { BattleMessage = "무사히 도망쳤다!"; await Task.Delay(1000); CloseBattle(); IsPlayerTurn = true; return; }

            if (playerAction == BattleAction.Rest)
            {
                if (_restUsesLeft > 0) { PlayerHp = Math.Min(PlayerMaxHp, PlayerHp + PlayerMaxHp / 3); _restUsesLeft--; BattleMessage = $"휴식을 취해 체력을 회복했다!\n(남은 휴식: {_restUsesLeft}회)"; _tempActionId = ANIM_NOD; _tempActionTimer = 45; UpdateAnimation(ANIM_NOD); }
                else { BattleMessage = "더 이상 휴식할 수 없다!"; }
                await Task.Delay(1500);
            }

            bool playerDodged = false;
            if (playerAction == BattleAction.Dodge)
            {
                if (rand.Next(100) < 70) { playerDodged = true; _isCounterReady = true; BattleMessage = "적의 공격을 피할 준비를 했다!\n(카운터 대기)"; }
                else { BattleMessage = "회피 준비에 실패했다..."; }
                await Task.Delay(1500);
            }

            if (playerAction == BattleAction.QuickAttack || playerAction == BattleAction.HeavyAttack)
            {
                IsPlayerTurn = true;
                return;
            }

            if (EnemyHp <= 0) { await CheckBattleEndAsync(); return; }

            await EnemyTurnAction(playerDodged);
        }

        public async Task ExecuteSkillTurnAsync(SkillInfo playerSkill)
        {
            if (!IsBattleOpen || PlayerHp <= 0 || EnemyHp <= 0 || !IsPlayerTurn) return;
            IsPlayerTurn = false;
            IsAttackMenuOpen = false;
            Random rand = new Random();

            var myInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);

            BattleMessage = $"{Name}의 {playerSkill.Name}!";
            await Task.Delay(1000);

            bool isHit = playerSkill.Accuracy == 0 || rand.Next(100) < playerSkill.Accuracy;

            if (isHit && playerSkill.Power > 0)
            {
                PokemonType enemyType1 = enemyInfo?.Type1 ?? PokemonType.Normal;
                PokemonType enemyType2 = enemyInfo?.Type2 ?? PokemonType.None;

                double typeMultiplier = TypeMatchupHelper.GetMultiplier(playerSkill.Type, enemyType1) *
                                        (enemyType2 != PokemonType.None ? TypeMatchupHelper.GetMultiplier(playerSkill.Type, enemyType2) : 1.0);

                double stabMultiplier = (myInfo != null && (myInfo.Type1 == playerSkill.Type || myInfo.Type2 == playerSkill.Type)) ? 1.5 : 1.0;

                int enemyCombatDef = (((enemyInfo?.BaseDef ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                int myCombatAtk = CombatAtk;

                if (playerSkill.Category == SkillCategory.Special)
                {
                    enemyCombatDef = (((enemyInfo?.BaseSpD ?? 50) * 2 + 15) * EnemyLevel / 100) + 5;
                    myCombatAtk = (((myInfo?.BaseSpA ?? 50) * 2 + (Genes.AtkGene - 80)) * Level / 100) + 5 + TrAtk;
                }

                int baseDamage = Math.Max(2, myCombatAtk - (enemyCombatDef / 2));
                int damage = (int)(baseDamage * typeMultiplier * stabMultiplier * (playerSkill.Power / 50.0));

                if (_isCounterReady) { damage *= 2; _isCounterReady = false; }
                damage = Math.Max(1, damage);

                IsEnemyTakingDamage = true;
                EnemyHp = Math.Max(0, EnemyHp - damage);

                string extraMsg = typeMultiplier >= 2.0 ? "효과가 굉장했다!\n" : (typeMultiplier > 0 && typeMultiplier <= 0.5 ? "효과가 별로인 듯하다...\n" : (typeMultiplier == 0 ? "효과가 없는 것 같다...\n" : ""));
                BattleMessage = $"{extraMsg}적에게 {damage} 데미지를 입혔다!";

                _tempActionId = ANIM_ATTACK; _tempActionTimer = 15; UpdateAnimation(ANIM_ATTACK);
                _enemyTempActionTimer = 15; UpdateEnemyAnimation(ANIM_HURT);

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
                int healAmount = selectedItem.EffectValue;
                PlayerHp = Math.Min(PlayerMaxHp, PlayerHp + healAmount);

                BattleMessage = $"{Name}에게 {selectedItem.Name}을(를) 사용했다!\n체력이 {healAmount} 회복되었다!";

                _tempActionId = ANIM_HOP;
                _tempActionTimer = 30;
                CheckStateAndAnimate();

                await Task.Delay(1500);

                await EnemyTurnAction(false);
            }
        }

        private async Task EnemyTurnAction(bool playerDodged)
        {
            var myInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            Random rand = new Random();

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

                int baseEnemyDamage = Math.Max(2, enemyCombatAtk - (myCombatDef / 2));
                int enemyDamage = (int)(baseEnemyDamage * typeMultiplier * stabMultiplier * (enemySkill.Power / 50.0));
                enemyDamage = Math.Max(1, enemyDamage);

                IsPlayerTakingDamage = true;
                PlayerHp = Math.Max(0, PlayerHp - enemyDamage);

                string extraMsg = typeMultiplier >= 2.0 ? "효과가 굉장했다!\n" : (typeMultiplier > 0 && typeMultiplier <= 0.5 ? "효과가 별로인 듯하다...\n" : (typeMultiplier == 0 ? "효과가 없는 것 같다...\n" : ""));
                BattleMessage = $"{extraMsg}{Name}(은)는 {enemyDamage} 데미지를 입었다!";

                _enemyTempActionId = ANIM_ATTACK; _enemyTempActionTimer = 15; UpdateEnemyAnimation(ANIM_ATTACK);
                _tempActionId = ANIM_HURT; _tempActionTimer = 15; UpdateAnimation(ANIM_HURT);

                await Task.Delay(600); IsPlayerTakingDamage = false; await Task.Delay(900);
            }
            else if (playerDodged) { BattleMessage = $"{Name}(은)는 공격을 멋지게 피했다!"; await Task.Delay(1500); }
            else if (!enemyHits) { BattleMessage = $"야생 {EnemyName}의 공격은 빗나갔다!"; await Task.Delay(1500); }

            if (PlayerHp <= 0) await CheckBattleEndAsync(); else BattleMessage = "행동을 선택하세요.";
            IsPlayerTurn = true;
        }

        private async Task CheckBattleEndAsync()
        {
            if (PlayerHp <= 0)
            {
                IsBattleResolved = true;
                BattleMessage = "눈앞이 깜깜해졌다...\n배틀에서 패배했습니다.";
                Joy = Math.Max(0, Joy - 10);
                Energy = Math.Max(0, Energy - 20);
                await Task.Delay(3000);
                CloseBattle();
            }
            else if (EnemyHp <= 0)
            {
                // 🌟 승리 시 메인 메뉴를 자동으로 가려주는 속성입니다.
                IsBattleResolved = true;

                Random rand = new Random();
                string dropMessage = "";

                if (rand.Next(100) < 60)
                {
                    if (rand.Next(100) < 70)
                    {
                        AddItemToInventory(ItemType.monsterball, 1);
                        dropMessage = "\n몬스터볼 1개를 얻었다!";
                    }
                    else
                    {
                        AddItemToInventory(ItemType.Potion, 1);
                        dropMessage = "\n상처약 1개를 얻었다!";
                    }
                }

                BattleMessage = $"배틀에서 승리했다!{dropMessage}\n어떻게 할까?";

                TrAtk = Math.Min(100, TrAtk + 5);
                Bond = Math.Min(100, Bond + 5);

                await Task.Delay(2500);

                // 🌟 여기서 배틀을 강제 종료하지 않고, 포획 선택창(잡아보기/그냥 가기)을 띄웁니다!
                IsCatchOffered = true;
            }
        }

        public async Task ExecuteCatchResultAsync()
        {
            double hpPercent = (double)EnemyHp / EnemyMaxHp;
            int catchRate = 10;

            if (hpPercent <= 0.2)
            {
                catchRate = 70;
            }
            else if (hpPercent <= 0.5)
            {
                catchRate = 35;
            }

            Random rand = new Random();
            bool isCaught = rand.Next(100) < catchRate;

            if (isCaught)
            {
                BattleMessage = $"신난다! {EnemyName}을(를) 잡았다!";
                await Task.Delay(2000);

                IsBattleOpen = false;
            }
            else
            {
                IsEnemyVisible = true;
                BattleMessage = "아아! 포켓몬이 볼에서 빠져나왔다!\n어떻게 할까?";
                await Task.Delay(2000);

                IsCatchOffered = true;
            }
        }
        #endregion
    }
}