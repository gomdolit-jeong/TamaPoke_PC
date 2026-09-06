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
        // 🌟 패배 시 화면이 어두워지는 효과를 제어하는 상태 값
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

        #region 🌟 스킬 학습 및 교체 시스템 로직 (Skill Learning Logic)
        private bool _isSkillLearnMenuOpen = false;
        public bool IsSkillLearnMenuOpen { get => _isSkillLearnMenuOpen; set => SetProperty(ref _isSkillLearnMenuOpen, value); }

        private bool _isSkillReplaceMenuOpen = false;
        public bool IsSkillReplaceMenuOpen { get => _isSkillReplaceMenuOpen; set => SetProperty(ref _isSkillReplaceMenuOpen, value); }

        private SkillInfo? _recommendedSkill1;
        public SkillInfo? RecommendedSkill1 { get => _recommendedSkill1; set { SetProperty(ref _recommendedSkill1, value); OnPropertyChanged(nameof(HasRecommendedSkill1)); } }
        public bool HasRecommendedSkill1 => RecommendedSkill1 != null;

        private SkillInfo? _recommendedSkill2;
        public SkillInfo? RecommendedSkill2 { get => _recommendedSkill2; set { SetProperty(ref _recommendedSkill2, value); OnPropertyChanged(nameof(HasRecommendedSkill2)); } }
        public bool HasRecommendedSkill2 => RecommendedSkill2 != null;

        private SkillInfo? _skillToLearn;

        public void OnBattleWon()
        {
            int n = SkillDex.GetLearnCount(SpeciesId);
            List<SkillInfo> availableSkills = new List<SkillInfo>();

            for (int i = 0; i < n; i++)
            {
                int at = SkillDex.GetLearnLevel(SpeciesId, i);
                if (at > Level) continue;

                int mv = SkillDex.GetLearnMove(SpeciesId, i);
                if (mv == 0 || KnowsSkill(mv)) continue;

                var skill = SkillDex.GetSkill(mv);
                if (skill != null) availableSkills.Add(skill);
            }

            if (availableSkills.Count > 0)
            {
                var rand = new Random();
                var picked = availableSkills.OrderBy(x => rand.Next()).Take(2).ToList();

                RecommendedSkill1 = picked.Count > 0 ? picked[0] : null;
                RecommendedSkill2 = picked.Count > 1 ? picked[1] : null;

                IsSkillLearnMenuOpen = true;
                BattleMessage = "실전 경험을 통해 새로운 스킬을 떠올렸다!\n어떤 스킬을 배울까?";
            }
            else
            {
                // 🌟 체육관 배틀일 경우 포획창을 스킵하고 배틀 닫기
                if (IsGymBattle)
                {
                    IsGymBattle = false;
                    CloseBattle();
                }
                else
                {
                    IsCatchOffered = true;
                }
            }
        }

        public void SelectRecommendedSkill(int optionNumber)
        {
            _skillToLearn = (optionNumber == 1) ? RecommendedSkill1 : RecommendedSkill2;
            if (_skillToLearn == null) return;

            if (SkillCount < 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Skills[i] == 0)
                    {
                        Skills[i] = _skillToLearn.Id;
                        break;
                    }
                }
                OnPropertyChanged(nameof(CurrentSkills));
                FinishSkillLearning($"{_skillToLearn.Name}을(를) 깨우쳤다!");
            }
            else
            {
                IsSkillLearnMenuOpen = false;
                IsSkillReplaceMenuOpen = true;
                BattleMessage = $"기술이 4개라 꽉 찼다!\n{_skillToLearn.Name}을(를) 위해 어떤 기술을 지울까?";
            }
        }

        public void ReplaceExistingSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 3 || _skillToLearn == null) return;

            var oldSkill = SkillDex.GetSkill(Skills[slotIndex]);
            string oldSkillName = oldSkill?.Name ?? "기술";

            Skills[slotIndex] = _skillToLearn.Id;
            OnPropertyChanged(nameof(CurrentSkills));

            FinishSkillLearning($"1, 2, 3... 짠!\n{oldSkillName}을(를) 잊고\n{_skillToLearn.Name}을(를) 배웠다!");
        }

        public void SkipSkillLearning()
        {
            FinishSkillLearning("새로운 스킬을 배우는 것을 포기했다.");
        }

        private async void FinishSkillLearning(string finalMessage)
        {
            IsSkillLearnMenuOpen = false;
            IsSkillReplaceMenuOpen = false;

            _ = ShowEventMessageAsync(finalMessage);
            await Task.Delay(2000);

            // 🌟 체육관 배틀일 경우 스킬을 배운 뒤 포획창을 스킵하고 즉시 배틀 종료
            if (IsGymBattle)
            {
                IsGymBattle = false;
                CloseBattle();
            }
            else
            {
                IsCatchOffered = true;
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
            if (type == ItemType.monsterball)
            {
                // 속성에 직접 더해주면 SetProperty가 작동하여 화면의 숫자가 즉시 올라갑니다!
                MonsterBalls += amount;
            }
            else if (type == ItemType.Potion)
            {
                Potions += amount;
            }

            // 아이템을 획득했으니 데이터가 날아가지 않도록 즉시 저장합니다.
            Save();
        }
        #endregion

        #region 배틀 로직 (Battle Logic)
        public async void StartWildBattle()
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;

            IsProfileOpen = false; _restUsesLeft = 2; _isCounterReady = false;
            IsBattleResolved = false; IsCatchOffered = false; IsAttackMenuOpen = false; IsEnemyVisible = true;
            IsSkillLearnMenuOpen = false; IsSkillReplaceMenuOpen = false; // 전투 시작 시 스킬 메뉴 초기화
            IsGymBattle = false;

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

        public void CloseBattle()
        {
            IsBattleOpen = false;
            IsGymBattle = false;
            BattleMessage = "";
        }
        public async void LeaveWildBattle()
        {
            IsCatchOffered = false;
            BattleMessage = $"{EnemyName}을(를) 뒤로하고 길을 떠납니다...";
            await Task.Delay(1500);

            await ProcessWildBattleRewardsAsync(); // 전리품 챙기기!
        }

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

                // 스킬 카테고리에 따른 모션 연출
                if (playerSkill.Category == SkillCategory.Physical)
                {
                    _tempActionId = ANIM_ATTACK;
                }
                else if (playerSkill.Category == SkillCategory.Special)
                {
                    _tempActionId = ANIM_BREATH;
                }
                else
                {
                    _tempActionId = ANIM_POSE;
                }

                _tempActionTimer = 15;
                UpdateAnimation(_tempActionId);

                _enemyTempActionTimer = 15;
                UpdateEnemyAnimation(ANIM_HURT);

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
                BattleMessage = IsGymBattle ? "관장에게 패배했습니다...\n수행이 더 필요합니다." : "눈앞이 깜깜해졌다...\n배틀에서 패배했습니다.";
                Joy = Math.Max(0, Joy - 10);
                Energy = Math.Max(0, Energy - 20);

                // 🌟 화면이 서서히 어두워지는 애니메이션 스위치 ON!
                IsDefeatedFadeOut = true;

                // 화면이 완전히 까매지도록 3초간 넉넉히 대기합니다.
                await Task.Delay(3000);

                // 상태 초기화 및 배틀 닫기
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
                    // 🌟 체육관 승리 로직
                    var leader = GymLeaders[GymBadges];
                    GymBadges++; // 배지 획득!
                    Save(); // 진행도 저장

                    BattleMessage = $"대단한 승부였다!\n{leader.LeaderName}에게서\n[{leader.BadgeName}]을(를) 얻었다!";
                    await Task.Delay(3000);

                    // 🌟 기존에 여기서 IsGymBattle = false; 를 처리해서 포획창이 떴습니다. 
                    // теперь OnBattleWon() 내부에서 IsGymBattle을 확인하도록 이 줄을 지웠습니다!
                    OnBattleWon(); // 스킬 학습 기회 제공
                }
                else
                {
                    BattleMessage = "배틀에서 승리했다!";
                    await Task.Delay(2000);
                    OnBattleWon(); 
                }
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
                UnlockPokemonInPokedex(EnemySpeciesId);

                var newMember = new PartyMember
                {
                    SpeciesId = EnemySpeciesId,
                    Name = EnemyName,
                    Level = EnemyLevel,
                    AgeMinutes = (EnemyLevel - 1) * MINUTES_PER_LEVEL,
                    IsShiny = false,
                    TrAtk = 0,
                    TrDef = 0,
                    TrSpeed = 0,
                    Skills = (int[])EnemySkills.Clone(),
                    Genes = new PokemonGene()
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

                await ProcessWildBattleRewardsAsync();
            }
            else
            {
                IsEnemyVisible = true;
                BattleMessage = "아아! 포켓몬이 볼에서 빠져나왔다!\n어떻게 할까?";
                await Task.Delay(2000);

                IsCatchOffered = true;
            }
        }

        // 🌟 1. 야생 배틀 종료 후 아이템을 획득하고 배틀을 닫는 공통 메서드
        private async Task ProcessWildBattleRewardsAsync()
        {
            Random rand = new Random();
            string dropMessage = "";

            if (rand.Next(100) < 60)
            {
                if (rand.Next(100) < 70) { AddItemToInventory(ItemType.monsterball, 1); dropMessage = "몬스터볼 1개를 얻었다!"; }
                else { AddItemToInventory(ItemType.Potion, 1); dropMessage = "상처약 1개를 얻었다!"; }
            }

            if (!string.IsNullOrEmpty(dropMessage))
            {
                BattleMessage = dropMessage;
                await Task.Delay(2000); // 사용자가 메시지를 읽을 시간을 줍니다.
            }

            CloseBattle(); // 모든 연출이 끝났으므로 배틀을 완전히 종료합니다.
        }
        #endregion

        #region 체육관 시스템 (Gym System)

        public class GymLeaderInfo
        {
            public string? GymName { get; set; }
            public string? LeaderName { get; set; }
            public string? BadgeName { get; set; }
            public int PokemonSpeciesId { get; set; }
            public int Level { get; set; }
            public int[]? SpecificSkills { get; set; }
        }

        public static readonly GymLeaderInfo[] GymLeaders = new GymLeaderInfo[]
        {
            // === 1세대 관동지방 ===
            new GymLeaderInfo { GymName = "회색 체육관", LeaderName = "웅이", BadgeName = "회색배지", PokemonSpeciesId = 95, Level = 14, SpecificSkills = new int[] { 60, 44, 1, 0 } },
            new GymLeaderInfo { GymName = "블루 체육관", LeaderName = "이슬", BadgeName = "블루배지", PokemonSpeciesId = 121, Level = 21, SpecificSkills = new int[] { 19, 32, 86, 0 } },
            new GymLeaderInfo { GymName = "갈색 체육관", LeaderName = "마티스", BadgeName = "오렌지배지", PokemonSpeciesId = 26, Level = 24, SpecificSkills = new int[] { 24, 23, 5, 0 } },
            new GymLeaderInfo { GymName = "무지개 체육관", LeaderName = "민화", BadgeName = "무지개배지", PokemonSpeciesId = 45, Level = 29, SpecificSkills = new int[] { 29, 41, 28, 0 } },
            new GymLeaderInfo { GymName = "연분홍 체육관", LeaderName = "독수", BadgeName = "핑크배지", PokemonSpeciesId = 110, Level = 43, SpecificSkills = new int[] { 41, 64, 8, 0 } },
            new GymLeaderInfo { GymName = "노랑 체육관", LeaderName = "초련", BadgeName = "골드배지", PokemonSpeciesId = 65, Level = 43, SpecificSkills = new int[] { 51, 64, 86, 0 } },
            new GymLeaderInfo { GymName = "홍련 체육관", LeaderName = "강연", BadgeName = "진홍배지", PokemonSpeciesId = 59, Level = 47, SpecificSkills = new int[] { 14, 8, 68, 0 } },
            new GymLeaderInfo { GymName = "상록 체육관", LeaderName = "비주기", BadgeName = "그린배지", PokemonSpeciesId = 112, Level = 50, SpecificSkills = new int[] { 44, 60, 55, 9 } },

            // === 2세대 성도지방 ===
            new GymLeaderInfo { GymName = "도라지 체육관", LeaderName = "비상", BadgeName = "윙배지", PokemonSpeciesId = 18, Level = 55, SpecificSkills = new int[] { 47, 45, 5, 0 } },
            new GymLeaderInfo { GymName = "고동 체육관", LeaderName = "호일", BadgeName = "인세트배지", PokemonSpeciesId = 123, Level = 58, SpecificSkills = new int[] { 57, 45, 75, 0 } },
            new GymLeaderInfo { GymName = "금빛 체육관", LeaderName = "꼭두", BadgeName = "레귤러배지", PokemonSpeciesId = 241, Level = 62, SpecificSkills = new int[] { 7, 86, 68, 0 } },
            new GymLeaderInfo { GymName = "인주 체육관", LeaderName = "유빈", BadgeName = "팬텀배지", PokemonSpeciesId = 94, Level = 65, SpecificSkills = new int[] { 64, 41, 89, 0 } },
            new GymLeaderInfo { GymName = "진청 체육관", LeaderName = "사도", BadgeName = "쇼크배지", PokemonSpeciesId = 62, Level = 68, SpecificSkills = new int[] { 37, 18, 44, 0 } },
            new GymLeaderInfo { GymName = "담청 체육관", LeaderName = "규리", BadgeName = "스틸배지", PokemonSpeciesId = 208, Level = 72, SpecificSkills = new int[] { 70, 44, 69, 0 } },
            new GymLeaderInfo { GymName = "황토 체육관", LeaderName = "류옹", BadgeName = "아이스배지", PokemonSpeciesId = 221, Level = 75, SpecificSkills = new int[] { 33, 44, 7, 0 } },
            new GymLeaderInfo { GymName = "검은먹 체육관", LeaderName = "이향", BadgeName = "라이징배지", PokemonSpeciesId = 230, Level = 80, SpecificSkills = new int[] { 67, 19, 32, 80 } },

            // === 3세대 호연지방 ===
            new GymLeaderInfo { GymName = "금탄 체육관", LeaderName = "원규", BadgeName = "스톤배지", PokemonSpeciesId = 306, Level = 82, SpecificSkills = new int[] { 60, 44, 8, 9 } },
            new GymLeaderInfo { GymName = "무로 체육관", LeaderName = "철구", BadgeName = "너클배지", PokemonSpeciesId = 297, Level = 84, SpecificSkills = new int[] { 44, 8, 5, 0 } },
            new GymLeaderInfo { GymName = "보라 체육관", LeaderName = "암전", BadgeName = "다이나모배지", PokemonSpeciesId = 310, Level = 86, SpecificSkills = new int[] { 24, 23, 5, 0 } },
            new GymLeaderInfo { GymName = "용암 체육관", LeaderName = "민지", BadgeName = "히트배지", PokemonSpeciesId = 324, Level = 88, SpecificSkills = new int[] { 14, 44, 8, 0 } },
            new GymLeaderInfo { GymName = "등화 체육관", LeaderName = "종길", BadgeName = "밸런스배지", PokemonSpeciesId = 289, Level = 90, SpecificSkills = new int[] { 44, 64, 8, 9 } },
            new GymLeaderInfo { GymName = "검방울 체육관", LeaderName = "은송", BadgeName = "깃털배지", PokemonSpeciesId = 334, Level = 92, SpecificSkills = new int[] { 47, 45, 44, 0 } },
            new GymLeaderInfo { GymName = "이끼 체육관", LeaderName = "풍&란", BadgeName = "마인드배지", PokemonSpeciesId = 338, Level = 94, SpecificSkills = new int[] { 51, 60, 64, 86 } },
            new GymLeaderInfo { GymName = "루네 체육관", LeaderName = "아단", BadgeName = "레인배지", PokemonSpeciesId = 350, Level = 96, SpecificSkills = new int[] { 19, 32, 86, 9 } },

            // === 4세대 신오지방 ===
            new GymLeaderInfo { GymName = "무쇠 체육관", LeaderName = "강석", BadgeName = "콜배지", PokemonSpeciesId = 409, Level = 98, SpecificSkills = new int[] { 60, 44, 8, 9 } },
            new GymLeaderInfo { GymName = "영원 체육관", LeaderName = "유채", BadgeName = "포레스트배지", PokemonSpeciesId = 407, Level = 100, SpecificSkills = new int[] { 29, 41, 86, 0 } },
            new GymLeaderInfo { GymName = "연고 체육관", LeaderName = "멜리사", BadgeName = "레릭배지", PokemonSpeciesId = 429, Level = 102, SpecificSkills = new int[] { 64, 51, 23, 0 } },
            new GymLeaderInfo { GymName = "장막 체육관", LeaderName = "자망", BadgeName = "코블배지", PokemonSpeciesId = 448, Level = 105, SpecificSkills = new int[] { 44, 64, 5, 9 } },
            new GymLeaderInfo { GymName = "들초 체육관", LeaderName = "맥실러", BadgeName = "펜배지", PokemonSpeciesId = 419, Level = 108, SpecificSkills = new int[] { 19, 32, 68, 5 } },
            new GymLeaderInfo { GymName = "운하 체육관", LeaderName = "동관", BadgeName = "마인배지", PokemonSpeciesId = 411, Level = 110, SpecificSkills = new int[] { 60, 44, 8, 86 } },
            new GymLeaderInfo { GymName = "선단 체육관", LeaderName = "무청", BadgeName = "글레이셔배지", PokemonSpeciesId = 460, Level = 115, SpecificSkills = new int[] { 32, 44, 29, 0 } },
            new GymLeaderInfo { GymName = "물가 체육관", LeaderName = "전진", BadgeName = "비컨배지", PokemonSpeciesId = 466, Level = 120, SpecificSkills = new int[] { 24, 23, 44, 9 } }
        };

        private int _gymBadges = 0;
        public int GymBadges { get => _gymBadges; set => SetProperty(ref _gymBadges, value); }

        private bool _isGymBattle = false;
        public bool IsGymBattle { get => _isGymBattle; set => SetProperty(ref _isGymBattle, value); }

        // ==========================================
        // 🌟 인게임 체육관 도전 확인창 상태 관리
        // ==========================================
        private bool _isGymConfirmOpen;
        public bool IsGymConfirmOpen
        {
            get => _isGymConfirmOpen;
            set => SetProperty(ref _isGymConfirmOpen, value);
        }

        private GymLeaderInfo? _selectedGymLeader;
        public GymLeaderInfo? SelectedGymLeader
        {
            get => _selectedGymLeader;
            set => SetProperty(ref _selectedGymLeader, value);
        }

        private int _selectedGymIndex;

        // 뱃지 버튼을 눌렀을 때 시스템 팝업 대신 인게임 확인창을 띄우는 메서드
        public void PromptGymChallenge(int gymIndex)
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;
            if (gymIndex < 0 || gymIndex >= GymLeaders.Length) return;

            // 🌟 버그 수정: 내가 가진 뱃지 개수보다 높은 번호의 체육관은 도전할 수 없도록 막습니다.
            // (예: 뱃지가 0개면 gymIndex 0(첫번째)만 도전 가능, 1 이상은 차단)
            if (gymIndex > GymBadges)
            {
                return; // 아무 일도 일어나지 않고 무시됩니다.
            }

            _selectedGymIndex = gymIndex;
            SelectedGymLeader = GymLeaders[gymIndex];
            IsGymConfirmOpen = true;
        }

        // 확인창에서 '도전하기'를 눌렀을 때 실제 배틀을 시작하는 메서드
        public void ConfirmGymChallenge()
        {
            IsGymConfirmOpen = false;
            StartSpecificGymBattle(_selectedGymIndex);
        }

        // 확인창에서 '취소'를 눌렀을 때
        public void CancelGymChallenge()
        {
            IsGymConfirmOpen = false;
        }

        // 🌟 특정 뱃지(체육관)를 직접 선택해서 도전하는 메서드
        // 특정 뱃지(체육관)를 직접 선택해서 도전하는 메서드
        public async void StartSpecificGymBattle(int gymIndex)
        {
            if (IsEgg || IsSleeping || Ceremony != 0 || IsAnyMiniGameOpen || IsBattleOpen) return;
            if (gymIndex < 0 || gymIndex >= GymLeaders.Length) return;

            // 🌟 버그 수정: 이 부분에 있던 GymBadges = gymIndex; 코드를 삭제했습니다!
            var leader = GymLeaders[gymIndex];

            IsProfileOpen = false; _restUsesLeft = 2; _isCounterReady = false;
            IsBattleResolved = false; IsCatchOffered = false; IsAttackMenuOpen = false; IsEnemyVisible = true;
            IsSkillLearnMenuOpen = false; IsSkillReplaceMenuOpen = false;

            IsGymBattle = true;

            EnemySpeciesId = leader.PokemonSpeciesId;
            EnemyLevel = leader.Level;

            // ... (아래 체력 계산 및 애니메이션 초기화 로직은 기존과 동일하게 유지) ...

            PlayerMaxHp = CombatMaxHp; PlayerHp = PlayerMaxHp;

            var enemyInfo = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == EnemySpeciesId);
            int enemyBaseHp = enemyInfo != null ? enemyInfo.BaseHp : 50;

            EnemyMaxHp = (((enemyBaseHp * 2 + 100) * EnemyLevel / 100) + EnemyLevel + 50) * 2;
            EnemyHp = EnemyMaxHp;

            EnemySkills = (int[])(leader.SpecificSkills?.Clone() ?? new int[4]);
            UpdateEnemyAnimation(0);

            IsBattleOpen = true;
            IsPlayerTurn = false;
            BattleMessage = $"체육관 관장 {leader.LeaderName}이(가)\n승부를 걸어왔다!";

            await Task.Delay(2000);

            BattleMessage = "행동을 선택하세요.";
            IsPlayerTurn = true;
        }
        #endregion
    }
}