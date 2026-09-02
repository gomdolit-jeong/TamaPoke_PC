using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

using TamaPoke.Utils;
using TamaPoke.Utils.Service;

namespace TamaPoke.Models
{
    public enum BattleAction { QuickAttack, HeavyAttack, Dodge, Rest, Run }

    public class DirtItem
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class PokemonGene
    {
        public int HpGene { get; set; } = 100;
        public int AtkGene { get; set; } = 100;
        public int DefGene { get; set; } = 100;
        public int SpeGene { get; set; } = 100;
    }

    public class PokedexEntry : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string SpeciesName { get; set; } = string.Empty;
        private bool _isUnlocked;
        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                if (_isUnlocked != value)
                {
                    _isUnlocked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUnlocked)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }
        public string DisplayName => IsUnlocked ? SpeciesName : "???";
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class PokemonState : INotifyPropertyChanged
    {
        #region 상수 및 열거형 (Constants)
        private const int ANIM_IDLE = 0;
        private const int ANIM_WALK = 1;
        private const int ANIM_SLEEP = 3;
        private const int ANIM_EAT = 4;
        private const int ANIM_HURT = 5;
        private const int ANIM_ATTACK = 6;
        private const int ANIM_POSE = 7;
        private const int ANIM_HOP = 8;
        private const int ANIM_NOD = 9;
        private const int ANIM_BREATH = 10;

        private const int MAX_POOPS = 3;
        private const int POOP_CHANCE = 5;
        private const int MINUTES_PER_LEVEL = 60;
        private const int FAREWELL_AGE_MIN = 3 * 24 * 60;
        private const int RUNAWAY_TICKS = 60;

        private const int MED_LV10 = 1 << 0;
        private const int MED_LV25 = 1 << 1;
        private const int MED_LV50 = 1 << 2;
        private const int MED_BERRY = 1 << 3;
        private const int MED_STREAK7 = 1 << 4;
        private const int MED_BOND = 1 << 5;
        private const int MED_FINAL = 1 << 6;
        private const int MED_FIT = 1 << 7;

        private static readonly int[] LegendaryIds = {
            144, 145, 146, 150, 151,
            243, 244, 245, 249, 250, 251,
            377, 378, 379, 380, 381, 382, 383, 384, 385, 386,
            480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 493
        };
        #endregion

        #region 기본 육성 스탯 (Basic Stats)
        private int _fullness = 80;
        public int Fullness { get => _fullness; set { if (SetProperty(ref _fullness, Clamp100(value))) CheckStateAndAnimate(); } }

        private int _joy = 80;
        public int Joy { get => _joy; set { if (SetProperty(ref _joy, Clamp100(value))) CheckStateAndAnimate(); } }

        private int _energy = 80;
        public int Energy { get => _energy; set { if (SetProperty(ref _energy, Clamp100(value))) CheckStateAndAnimate(); } }

        private int _hygiene = 100;
        public int Hygiene { get => _hygiene; set { if (SetProperty(ref _hygiene, Clamp100(value))) CheckStateAndAnimate(); } }

        private int _weight = 10;
        public int Weight { get => _weight; set { if (SetProperty(ref _weight, Clamp100(value))) CheckMedals(); } }

        private int _bond = 0;
        public int Bond { get => _bond; set { if (SetProperty(ref _bond, Clamp100(value))) CheckMedals(); } }

        public int TrSpeed { get; set; } = 0;
        public int TrAtk { get; set; } = 0;
        public int TrDef { get; set; } = 0;

        private int _poops = 0;
        public int Poops { get => _poops; set { if (SetProperty(ref _poops, Math.Max(0, Math.Min(value, MAX_POOPS)))) { OnPropertyChanged(nameof(PoopDisplay)); CheckStateAndAnimate(); } } }
        [JsonIgnore] public string PoopDisplay { get { string result = ""; for (int i = 0; i < Poops; i++) result += "💩"; return result; } }

        private int _medals = 0;
        public int Medals
        {
            get => _medals;
            set
            {
                if (SetProperty(ref _medals, value))
                {
                    OnPropertyChanged(nameof(MedalCount));
                    OnPropertyChanged(nameof(HasMedalLv10)); OnPropertyChanged(nameof(HasMedalLv25));
                    OnPropertyChanged(nameof(HasMedalLv50)); OnPropertyChanged(nameof(HasMedalBerry));
                    OnPropertyChanged(nameof(HasMedalStreak7)); OnPropertyChanged(nameof(HasMedalBond));
                    OnPropertyChanged(nameof(HasMedalFinal)); OnPropertyChanged(nameof(HasMedalFit));
                }
            }
        }
        #endregion

        #region 인벤토리 (아이템)
        private bool _isInventoryOpen;
        public bool IsInventoryOpen
        {
            get => _isInventoryOpen;
            set { _isInventoryOpen = value; OnPropertyChanged(nameof(IsInventoryOpen)); }
        }

        private int _monsterBalls;
        public int MonsterBalls
        {
            get => _monsterBalls;
            set { _monsterBalls = value; OnPropertyChanged(nameof(MonsterBalls)); }
        }

        private int _potions;
        public int Potions
        {
            get => _potions;
            set { _potions = value; OnPropertyChanged(nameof(Potions)); }
        }

        public void ResetIdleMenus()
        {
            IsInventoryOpen = false;
            IsFeedMenuOpen = false;
            IsPlayMenuOpen = false;
        }
        #endregion

        #region 포켓몬 정보 및 상태 속성 (Pokemon Info & Status)
        private int _speciesId = -1;
        public int SpeciesId
        {
            get => _speciesId;
            set
            {
                if (SetProperty(ref _speciesId, value))
                {
                    OnPropertyChanged(nameof(IsEgg)); OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(IsFinalEvolution)); OnPropertyChanged(nameof(LevelDisplay)); OnPropertyChanged(nameof(CanFarewellNow));
                    OnPropertyChanged(nameof(CanRunawayNow)); OnPropertyChanged(nameof(CanEvolveNow)); OnPropertyChanged(nameof(Biome));
                    OnPropertyChanged(nameof(GrassColor)); OnPropertyChanged(nameof(SkyColor)); OnPropertyChanged(nameof(GroundColor));
                    OnPropertyChanged(nameof(FavoriteBerryName));

                    UpdateBackgroundImage();
                    _animationFrames = null; _currentActionId = -1; CheckStateAndAnimate(); CheckMedals();
                }
            }
        }

        private PokemonGene _genes = new PokemonGene();
        public PokemonGene Genes { get => _genes; set => SetProperty(ref _genes, value); }

        private bool _isShiny = false;
        public bool IsShiny { get => _isShiny; set => SetProperty(ref _isShiny, value); }

        private bool _isSleeping = false;
        public bool IsSleeping { get => _isSleeping; set { if (SetProperty(ref _isSleeping, value)) { if (value) { PosX = 0; PosY = 0; FlipX = 1; TimeOfDay = 3; } else UpdateDayNightCycle(); CheckStateAndAnimate(); OnPropertyChanged(nameof(CanFarewellNow)); OnPropertyChanged(nameof(CanRunawayNow)); OnPropertyChanged(nameof(CanEvolveNow)); } } }

        [JsonIgnore] public bool IsEgg => SpeciesId < 0;

        // 🌟 수정됨: 외부 파티 교체 로직에서 덮어씌울 수 있도록 set 개방
        private string? _overrideName;
        [JsonIgnore]
        public string Name
        {
            get => _overrideName ?? (IsEgg ? "알" : PokemonDex.GetName(SpeciesId) + (IsShiny ? " ✨" : ""));
            set { _overrideName = value; OnPropertyChanged(nameof(Name)); }
        }

        // 🌟 수정됨: 외부 파티 교체 로직에서 덮어씌울 수 있도록 set 개방
        private int? _overrideLevel;
        [JsonIgnore]
        public int Level
        {
            get => _overrideLevel ?? (1 + (AgeMinutes / MINUTES_PER_LEVEL));
            set { _overrideLevel = value; OnPropertyChanged(nameof(Level)); OnPropertyChanged(nameof(LevelDisplay)); }
        }

        [JsonIgnore] public int LowestStat => Math.Min(Math.Min(Fullness, Joy), Math.Min(Energy, Hygiene));
        [JsonIgnore] public bool IsFinalEvolution => !IsEgg && (PokemonDex.AllPokemons.FirstOrDefault(p => p.Id == SpeciesId)?.EvolveTo == 0);
        [JsonIgnore] public string LevelDisplay => IsEgg ? "" : $"Lv.{Level}";

        public int Streak { get; set; } = 0;
        public DateTime LastPlayedDate { get; set; } = DateTime.Now.Date;
        public int RegisteredCount { get; set; } = 25;
        public int LastEnd { get; set; } = 1;

        private int _timeOfDay = 1;
        public int TimeOfDay
        {
            get => _timeOfDay;
            set
            {
                if (SetProperty(ref _timeOfDay, value))
                {
                    OnPropertyChanged(nameof(SkyColor)); OnPropertyChanged(nameof(GrassColor)); OnPropertyChanged(nameof(GroundColor)); OnPropertyChanged(nameof(IsNight));
                    UpdateBackgroundImage();
                }
            }
        }
        [JsonIgnore] public bool IsNight => _timeOfDay == 3;

        private int GetBiomeFromType()
        {
            var p = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            if (p == null) return 0;

            switch (p.Type1)
            {
                case PokemonType.Water: return 1;
                case PokemonType.Normal: case PokemonType.Flying: case PokemonType.Electric: return 2;
                case PokemonType.Fire: case PokemonType.Fighting: case PokemonType.Rock: case PokemonType.Ground: return 3;
                case PokemonType.Ice: return 4;
                case PokemonType.Dark: case PokemonType.Ghost: case PokemonType.Dragon: case PokemonType.Steel: return 5;
                case PokemonType.Grass: case PokemonType.Bug: case PokemonType.Poison: case PokemonType.Psychic: case PokemonType.Fairy: default: return 0;
            }
        }

        [JsonIgnore] public int Biome => IsEgg ? 0 : GetBiomeFromType();
        [JsonIgnore] public Brush SkyColor => (Brush)new BrushConverter().ConvertFrom(TimeOfDay == 0 ? "#E29181" : TimeOfDay == 1 ? "#B5DBE8" : TimeOfDay == 2 ? "#DC8457" : "#151C35")!;
        [JsonIgnore] public Brush GrassColor => (Brush)new BrushConverter().ConvertFrom(IsNight ? "#161C30" : Biome == 0 ? "#7EC07F" : Biome == 1 ? "#DCCA94" : Biome == 2 ? "#4F8A55" : Biome == 3 ? "#8A5544" : Biome == 4 ? "#A8906A" : "#E6EEF5")!;
        [JsonIgnore] public Brush GroundColor => (Brush)new BrushConverter().ConvertFrom(IsNight ? "#222638" : "#F2EFE1")!;

        private ImageSource? _backgroundImage;
        [JsonIgnore]
        public ImageSource? BackgroundImage
        {
            get => _backgroundImage;
            set => SetProperty(ref _backgroundImage, value);
        }

        public void UpdateBackgroundImage()
        {
            string[] biomeNames = { "TALL GRASS", "BEACH", "PATH", "MOUNTAIN", "SNOW", "CAVE" };
            int b = Biome;
            if (b < 0 || b > 5) b = 0;

            string timeSuffix = IsNight ? " NIGHT" : "";
            string fileName = $"{biomeNames[b]}{timeSuffix}.png";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "Backgrounds", fileName);

            if (File.Exists(path))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                bmp.Freeze();
                BackgroundImage = bmp;
            }
        }
        #endregion

        #region 애니메이션 및 메인 UI 상태 (Animations & UI)
        private BitmapSource[]? _animationFrames;
        private int _currentFrameIndex = 0;
        private DispatcherTimer? _mainLoopTimer;
        private BitmapSource? _currentFrame;

        [JsonIgnore] public BitmapSource? CurrentFrame { get => _currentFrame; set => SetProperty(ref _currentFrame, value); }

        private BitmapSource? _enemyCurrentFrame;
        [JsonIgnore] public BitmapSource? EnemyCurrentFrame { get => _enemyCurrentFrame; set => SetProperty(ref _enemyCurrentFrame, value); }

        private double _posX = 0;
        public double PosX { get => _posX; set => SetProperty(ref _posX, value); }
        private double _posY = 0;
        public double PosY { get => _posY; set => SetProperty(ref _posY, value); }
        private double _flipX = 1;
        public double FlipX { get => _flipX; set => SetProperty(ref _flipX, value); }

        private bool _isMuted = true;
        public bool IsMuted { get => _isMuted; set { if (SetProperty(ref _isMuted, value)) OnPropertyChanged(nameof(SoundIcon)); } }
        [JsonIgnore] public string SoundIcon => IsMuted ? "🔇" : "🔊";

        private string _evolutionMessage = "";
        public string EvolutionMessage { get => _evolutionMessage; set { SetProperty(ref _evolutionMessage, value); OnPropertyChanged(nameof(MoodText)); } }

        private bool _isEvolvingFlash = false;
        public bool IsEvolvingFlash { get => _isEvolvingFlash; set => SetProperty(ref _isEvolvingFlash, value); }

        private int _ceremony = 0;
        public int Ceremony
        {
            get => _ceremony;
            set
            {
                if (SetProperty(ref _ceremony, value))
                {
                    OnPropertyChanged(nameof(MoodText)); OnPropertyChanged(nameof(IsAlive)); OnPropertyChanged(nameof(IsAliveAndNotEgg));
                    OnPropertyChanged(nameof(IsCeremony)); OnPropertyChanged(nameof(CanFarewellNow)); OnPropertyChanged(nameof(CanRunawayNow)); OnPropertyChanged(nameof(CanEvolveNow));
                    CheckStateAndAnimate();
                }
            }
        }
        [JsonIgnore] public bool IsCeremony => Ceremony != 0;
        [JsonIgnore] public bool IsAlive => Ceremony == 0;
        [JsonIgnore] public bool IsAliveAndNotEgg => Ceremony == 0 && !IsEgg && !IsAnyMiniGameOpen && !IsProfileOpen && !IsBattleOpen;

        [JsonIgnore]
        public string MoodText
        {
            get
            {
                if (Ceremony == 4) return string.IsNullOrEmpty(EvolutionMessage) ? "✨ 진화하는 중입니다... ✨" : EvolutionMessage;

                if (IsBattleOpen) return "야생 포켓몬과 배틀 중!";
                if (IsProfileOpen) return "프로필 확인 중...";
                if (IsBallGameOpen) return "집중해서 공을 튕기세요!";
                if (IsCatchGameOpen) return "과일을 빠르게 받으세요!";
                if (IsMemoGameOpen) return "순서를 잘 기억하세요!";
                if (IsCleanGameOpen) return "먼지를 깨끗하게 청소하세요!";
                if (Ceremony == 1) return "마지막 인사를 하고 있어요...";
                if (Ceremony == 2) return "너무 슬퍼서 집을 나갔어요...";
                if (Ceremony == 3) return "자연으로 돌아가고 있어요...\n안녕!";
                if (IsEgg) return "부화를 기다리는 중...";
                if (IsSleeping) return "쿨쿨 자고 있어요";
                if (CanRunawayNow) return "아무도 돌봐주지 않아\n외로워요...";
                if (CanFarewellNow) return "당신에게 할 말이 있나 봐요!";
                if (Poops > 0) return "응가를 했어요!\n청소해줘";
                if (Hygiene < 30) return "몸이 더러워서 가려워요";
                if (LowestStat < 30) return "기분이 아주 안 좋아요...";
                if (LowestStat < 50) return "조금 기운이 없어요";
                if (Fullness > 80 && Joy > 80) return "정말 행복해요!";
                return "기분이 좋아요";
            }
        }
        #endregion

        #region 도감 및 메뉴 UI 상태 (Menus & Pokedex UI)
        public List<int> UnlockedPokemon { get; set; } = new List<int>();

        [JsonIgnore]
        public ObservableCollection<PokedexEntry> FullPokedex { get; set; } = new ObservableCollection<PokedexEntry>();

        [JsonIgnore]
        public ObservableCollection<PokedexEntry> FilteredPokedex { get; set; } = new ObservableCollection<PokedexEntry>();

        private int _currentRegionIndex = 0;
        public int CurrentRegionIndex
        {
            get => _currentRegionIndex;
            set
            {
                if (SetProperty(ref _currentRegionIndex, value))
                {
                    UpdateFilteredPokedex();
                }
            }
        }

        public void UpdateFilteredPokedex()
        {
            FilteredPokedex.Clear();
            int startId = 1, endId = 493;

            switch (CurrentRegionIndex)
            {
                case 0: startId = 1; endId = 151; break;
                case 1: startId = 152; endId = 251; break;
                case 2: startId = 252; endId = 386; break;
                case 3: startId = 387; endId = 493; break;
                default: startId = 1; endId = 493; break;
            }

            foreach (var entry in FullPokedex.Where(p => p.Id >= startId && p.Id <= endId))
            {
                FilteredPokedex.Add(entry);
            }
        }

        private bool _isDexOpen = false;
        public bool IsDexOpen
        {
            get => _isDexOpen;
            set
            {
                if (SetProperty(ref _isDexOpen, value))
                {
                    OnPropertyChanged(nameof(IsAliveAndNotEgg));
                    OnPropertyChanged(nameof(MoodText));
                    if (value) ResetIdleMenus();
                }
            }
        }

        private bool _isFeedMenuOpen = false;
        public bool IsFeedMenuOpen { get => _isFeedMenuOpen; set => SetProperty(ref _isFeedMenuOpen, value); }
        private bool _isPlayMenuOpen = false;
        public bool IsPlayMenuOpen { get => _isPlayMenuOpen; set => SetProperty(ref _isPlayMenuOpen, value); }

        private bool _isProfileOpen = false;
        public bool IsProfileOpen
        {
            get => _isProfileOpen;
            set
            {
                if (SetProperty(ref _isProfileOpen, value))
                {
                    OnPropertyChanged(nameof(IsAliveAndNotEgg));
                    OnPropertyChanged(nameof(MoodText));
                    if (value) ResetIdleMenus();
                }
            }
        }

        private int _profilePage = 0;
        public int ProfilePage { get => _profilePage; set { SetProperty(ref _profilePage, value); OnPropertyChanged(nameof(ProfilePageDisplay)); } }
        [JsonIgnore] public string ProfilePageDisplay => $"페이지 {ProfilePage + 1} / 5";

        private string _currentFoodIcon = "🍎";
        public string CurrentFoodIcon { get => _currentFoodIcon; set => SetProperty(ref _currentFoodIcon, value); }
        private string _currentFoodColor = "#E53935";
        public string CurrentFoodColor { get => _currentFoodColor; set => SetProperty(ref _currentFoodColor, value); }
        private bool _isBathing = false;
        public bool IsBathing { get => _isBathing; set => SetProperty(ref _isBathing, value); }

        private bool _isPetting = false;
        public bool IsPetting { get => _isPetting; set => SetProperty(ref _isPetting, value); }

        private bool _berryKnown = false;
        public bool BerryKnown { get => _berryKnown; set { if (SetProperty(ref _berryKnown, value)) CheckMedals(); } }
        [JsonIgnore] public int FavoriteBerry => Math.Abs(SpeciesId) % 3;
        [JsonIgnore] public string FavoriteBerryName => !BerryKnown ? "???" : (FavoriteBerry == 0 ? "🍒 빨간 열매" : FavoriteBerry == 1 ? "🫐 파란 열매" : "🍏 초록 열매");
        #endregion

        #region 내부 시스템 로직 (Internal Logic & Timing)
        private int _ageSeconds = 0;
        private int _ageMinutes = 0;
        public int AgeMinutes
        {
            get => _ageMinutes;
            set
            {
                if (SetProperty(ref _ageMinutes, value))
                {
                    OnPropertyChanged(nameof(Level)); OnPropertyChanged(nameof(LevelDisplay)); OnPropertyChanged(nameof(CanFarewellNow)); OnPropertyChanged(nameof(CanEvolveNow));
                    OnPropertyChanged(nameof(EvolveProgressText)); OnPropertyChanged(nameof(EvolveProgressPercent));
                    CheckMedals();
                }
            }
        }

        private int _careMistakes = 0;
        private int _goodTicks = 0;
        public int CareMistakes { get => _careMistakes; set { if (SetProperty(ref _careMistakes, value)) { OnPropertyChanged(nameof(CanEvolveNow)); OnPropertyChanged(nameof(EvolveProgressText)); } } }

        private int _tempActionId = ANIM_IDLE;
        private int _tempActionTimer = 0;
        private int _currentActionId = ANIM_IDLE;
        private int _enemyTempActionId = ANIM_IDLE;
        private int _enemyTempActionTimer = 0;

        [JsonIgnore] public bool IsEating => _tempActionTimer > 0 && _tempActionId == ANIM_EAT;
        [JsonIgnore] public bool IsPlaying => _tempActionTimer > 0 && (_tempActionId == ANIM_POSE || _tempActionId == ANIM_HOP);

        private int _neglectTicks = 0;
        public int NeglectTicks { get => _neglectTicks; set { SetProperty(ref _neglectTicks, value); OnPropertyChanged(nameof(CanRunawayNow)); } }
        private int _eggTaps = 0;
        public int EggTaps { get => _eggTaps; set => SetProperty(ref _eggTaps, value); }

        private bool _isEvolutionPostponed = false;
        public bool IsEvolutionPostponed { get => _isEvolutionPostponed; set { SetProperty(ref _isEvolutionPostponed, value); OnPropertyChanged(nameof(CanShowEvolvePrompt)); } }

        private bool _isFarewellPostponed = false;
        public bool IsFarewellPostponed { get => _isFarewellPostponed; set { SetProperty(ref _isFarewellPostponed, value); OnPropertyChanged(nameof(CanShowFarewellPrompt)); } }

        [JsonIgnore] public bool CanShowEvolvePrompt => CanEvolveNow && !IsEvolutionPostponed;
        [JsonIgnore] public bool CanShowFarewellPrompt => CanFarewellNow && !IsFarewellPostponed;

        [JsonIgnore]
        public bool CanEvolveNow
        {
            get
            {
                if (IsEgg || IsSleeping || Ceremony != 0 || IsFinalEvolution || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return false;
                if (!DexTable.ContainsKey(SpeciesId)) return false;
                return Level >= (DexTable[SpeciesId].EvolveLevel + CareMistakes) && LowestStat >= 40;
            }
        }

        [JsonIgnore] public bool CanFarewellNow => !IsEgg && !IsSleeping && Ceremony == 0 && IsFinalEvolution && AgeMinutes >= FAREWELL_AGE_MIN && !IsAnyMiniGameOpen && !IsProfileOpen && !IsBattleOpen;
        [JsonIgnore] public bool CanRunawayNow => !IsEgg && !IsSleeping && Ceremony == 0 && NeglectTicks >= RUNAWAY_TICKS && !IsAnyMiniGameOpen && !IsProfileOpen && !IsBattleOpen;

        [JsonIgnore]
        public string EvolveProgressText
        {
            get
            {
                if (IsEgg) return "";
                if (IsFinalEvolution) return "최종 진화 형태 달성";
                if (!DexTable.ContainsKey(SpeciesId)) return "진화 정보 없음";
                int targetLv = DexTable[SpeciesId].EvolveLevel + CareMistakes;
                int remaining = targetLv - Level;
                return remaining <= 0 ? (LowestStat >= 40 ? "진화 조건 충족!" : "컨디션(40) 부족으로 보류됨") : $"진화까지 {remaining} 레벨 남음";
            }
        }

        [JsonIgnore]
        public double EvolveProgressPercent
        {
            get
            {
                if (IsEgg || IsFinalEvolution || !DexTable.ContainsKey(SpeciesId)) return 100.0;
                int targetLv = DexTable[SpeciesId].EvolveLevel + CareMistakes;
                return Math.Min(100.0, (double)Level / targetLv * 100.0);
            }
        }

        [JsonIgnore] public bool HasMedalLv10 => (Medals & MED_LV10) != 0;
        [JsonIgnore] public bool HasMedalLv25 => (Medals & MED_LV25) != 0;
        [JsonIgnore] public bool HasMedalLv50 => (Medals & MED_LV50) != 0;
        [JsonIgnore] public bool HasMedalBerry => (Medals & MED_BERRY) != 0;
        [JsonIgnore] public bool HasMedalStreak7 => (Medals & MED_STREAK7) != 0;
        [JsonIgnore] public bool HasMedalBond => (Medals & MED_BOND) != 0;
        [JsonIgnore] public bool HasMedalFinal => (Medals & MED_FINAL) != 0;
        [JsonIgnore] public bool HasMedalFit => (Medals & MED_FIT) != 0;

        [JsonIgnore]
        public int MedalCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 8; i++) if ((Medals & (1 << i)) != 0) count++;
                return count;
            }
        }
        #endregion

        #region 초기화 및 게임 루프 (Init & Main Loop)
        public void InitializeAfterLoad()
        {
            IsPartyOpen = false;

            IsProfileOpen = false; IsFeedMenuOpen = false; IsPlayMenuOpen = false; IsDexOpen = false; IsBattleOpen = false;
            IsBallGameOpen = false; IsCatchGameOpen = false; IsMemoGameOpen = false; IsCleanGameOpen = false;
            IsBathing = false; IsPetting = false;

            if (Ceremony == 4) Ceremony = 0;
            if (UnlockedPokemon == null) UnlockedPokemon = new List<int>();
            if (UnlockedPokemon.Count == 0 && SpeciesId > 0) { UnlockedPokemon.Add(SpeciesId); RegisteredCount = UnlockedPokemon.Count; }

            if (DexTable.Count == 0)
            {
                InitializeEvolutionTable();
            }

            RefreshPokedex(); UpdateDayNightCycle(); CheckDailyStreak(); UpdateBackgroundImage();

            if (_mainLoopTimer == null)
            {
                _mainLoopTimer = new DispatcherTimer(DispatcherPriority.Render);
                _mainLoopTimer.Interval = TimeSpan.FromMilliseconds(33);
                _mainLoopTimer.Tick += MainLoopTimer_Tick;
            }
            CheckStateAndAnimate(); _mainLoopTimer.Start(); Save();
        }

        private int _frameTickCounter = 0;
        private void MainLoopTimer_Tick(object? sender, EventArgs e)
        {
            _frameTickCounter++;
            if (_frameTickCounter >= 6) { _frameTickCounter = 0; if (_animationFrames != null && _animationFrames.Length > 0) { _currentFrameIndex = (_currentFrameIndex + 1) % _animationFrames.Length; CurrentFrame = _animationFrames[_currentFrameIndex]; } }

            if (_tempActionId == ANIM_WALK && _tempActionTimer > 0 && !IsBattleOpen)
            {
                PosX = Math.Max(-80, Math.Min(80, PosX + (-FlipX * 1.5)));
            }

            if (_tempActionTimer > 0)
            {
                _tempActionTimer--;
                if (_tempActionTimer <= 0)
                {
                    IsBathing = false;
                    CheckStateAndAnimate();
                }
            }

            if (_enemyTempActionTimer > 0)
            {
                _enemyTempActionTimer--;
                if (_enemyTempActionTimer <= 0 && IsBattleOpen) UpdateEnemyAnimation(ANIM_IDLE);
            }

            if (IsBallGameOpen) StepBallGame();
            if (IsCatchGameOpen) StepCatchGame();
            if (IsCleanGameOpen) StepCleanGame();
        }

        public void Tick()
        {
            if (IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;
            _ageSeconds++; UpdateDayNightCycle();

            if (DateTime.Now.Date > LastPlayedDate.Date) CheckDailyStreak();
            if (IsEgg) { if (_ageSeconds >= 180) Hatch(); return; }

            if (!IsSleeping && !IsCeremony && !IsBattleOpen && _tempActionTimer <= 0)
            {
                if (_currentActionId == ANIM_IDLE)
                {
                    Random moveRand = new Random(); int r = moveRand.Next(100);
                    if (r < 35)
                    {
                        if (moveRand.Next(10000) < 3)
                        {
                            StartWildBattle();
                        }
                        else
                        {
                            FlipX = moveRand.Next(0, 2) == 0 ? -1 : 1;
                            _tempActionId = ANIM_WALK;
                            _tempActionTimer = moveRand.Next(120, 240);
                        }
                    }
                    else if (r < 60) { int[] flair = { ANIM_POSE, ANIM_NOD, ANIM_BREATH }; _tempActionId = flair[moveRand.Next(flair.Length)]; _tempActionTimer = 90; }
                    else { _tempActionId = ANIM_IDLE; _tempActionTimer = moveRand.Next(60, 150); }
                    CheckStateAndAnimate();
                }
            }

            if (_ageSeconds % 60 == 0)
            {
                AgeMinutes++;
                if (AgeMinutes % MINUTES_PER_LEVEL == 0) IsEvolutionPostponed = false;
                if (LowestStat >= 40) { _goodTicks++; if (_goodTicks >= 720) { _goodTicks = 0; if (TrDef < 100) TrDef++; } } else { _goodTicks = 0; }

                if (IsSleeping)
                {
                    Energy = Clamp100(Energy + 6);
                    if (AgeMinutes % 30 == 0) Hygiene = Math.Max(0, Hygiene - 1);
                    CheckStateAndAnimate();
                    return;
                }

                if (AgeMinutes % 20 == 0) { Fullness = Math.Max(0, Fullness - 2); Energy = Math.Max(0, Energy - 1); }

                Random rand = new Random();
                if (Fullness > 40 && Poops < MAX_POOPS && rand.Next(100) < POOP_CHANCE) { Poops++; Hygiene = Clamp100(Hygiene - (10 * Poops)); }
                if (AgeMinutes % 10 == 0) { int dJoy = 0; if (Fullness < 30) dJoy -= 2; if (Hygiene < 30) dJoy -= 3; Joy = Clamp100(Joy + dJoy); }
                if (Fullness == 0 && Joy == 0 && Energy == 0 && Hygiene == 0) { if (NeglectTicks < RUNAWAY_TICKS) NeglectTicks++; } else { NeglectTicks = 0; }
                CheckMedals(); CheckStateAndAnimate();
            }
        }
        #endregion

        #region 액션 및 상호작용 (Actions)
        public async void Feed(int foodType)
        {
            if (IsEgg || IsSleeping || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;

            if (IsEating)
            {
                _tempActionId = ANIM_IDLE;
                OnPropertyChanged(nameof(IsEating));
                await Task.Delay(50);
            }

            switch (foodType) { case 0: CurrentFoodIcon = "🍒"; CurrentFoodColor = "#E53935"; break; case 1: CurrentFoodIcon = "🫐"; CurrentFoodColor = "#1E88E5"; break; case 2: CurrentFoodIcon = "🍏"; CurrentFoodColor = "#43A047"; break; case 3: CurrentFoodIcon = "🍬"; CurrentFoodColor = "#F06292"; break; }
            if (foodType == 3) { Fullness = Clamp100(Fullness + 10); Joy = Clamp100(Joy + 12); Weight = Clamp100(Weight + 12); }
            else { if (foodType == FavoriteBerry) { Fullness = Clamp100(Fullness + 35); Joy = Clamp100(Joy + 10); Bond = Clamp100(Bond + 2); BerryKnown = true; } else { Fullness = Clamp100(Fullness + 25); } }
            NeglectTicks = 0; IsFeedMenuOpen = false;

            ResetPosition();
            _tempActionId = ANIM_EAT;
            _tempActionTimer = 90;

            OnPropertyChanged(nameof(CurrentFoodIcon));
            OnPropertyChanged(nameof(CurrentFoodColor));
            OnPropertyChanged(nameof(IsEating));
            CheckStateAndAnimate();

            await Task.Delay(3000);

            if (!IsAnyMiniGameOpen && !IsBattleOpen)
            {
                _tempActionId = ANIM_HOP;
                _tempActionTimer = 30;
                CheckStateAndAnimate();
            }
        }

        public async void Clean()
        {
            if (IsEgg || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen || IsSleeping) return;

            Poops = 0; Hygiene = 100; Bond = Clamp100(Bond + 1); NeglectTicks = 0;
            IsBathing = true;

            ResetPosition();

            _tempActionId = ANIM_POSE;
            _tempActionTimer = 60;

            await Task.Delay(2000);
            IsBathing = false;

            if (!IsAnyMiniGameOpen && !IsBattleOpen)
            {
                _tempActionId = ANIM_HOP;
                _tempActionTimer = 30;
                CheckStateAndAnimate();
            }
        }

        public void ToggleSleep() { if (IsEgg || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return; IsSleeping = !IsSleeping; _tempActionTimer = 0; CheckStateAndAnimate(); }
        public void ToggleMute() => IsMuted = !IsMuted;

        public async void Pet()
        {
            if (IsEgg || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsDexOpen || IsBattleOpen) return;
            Joy = Clamp100(Joy + 5); Bond = Clamp100(Bond + 1);

            ResetPosition();
            _tempActionId = ANIM_HOP; _tempActionTimer = 30;

            IsPetting = true;
            await Task.Delay(1000);
            IsPetting = false;
        }

        public void TapEgg() { if (!IsEgg) return; EggTaps++; if (EggTaps >= 3) Hatch(); }
        public void PostponeEvolve() => IsEvolutionPostponed = true;
        public void PostponeFarewell() => IsFarewellPostponed = true;

        public void NextProfilePage() { ProfilePage = (ProfilePage + 1) % 5; }
        public void PrevProfilePage() { ProfilePage = (ProfilePage + 4) % 5; }
        #endregion

        #region 내부 헬퍼 (Helpers)
        public Action? RequestCatchAnimation;
        private void CheckMedals()
        {
            if (IsEgg) return;
            int newMedals = Medals;
            if (Level >= 10) newMedals |= MED_LV10;
            if (Level >= 25) newMedals |= MED_LV25;
            if (Level >= 50) newMedals |= MED_LV50;
            if (BerryKnown) newMedals |= MED_BERRY;
            if (Streak >= 7) newMedals |= MED_STREAK7;
            if (Bond >= 100) newMedals |= MED_BOND;
            if (IsFinalEvolution) newMedals |= MED_FINAL;
            if (Weight == 0 && Level >= 5 && CareMistakes == 0) newMedals |= MED_FIT;
            Medals = newMedals;
        }

        private int GetTargetAction()
        {
            if (IsProfileOpen || IsBattleOpen) return ANIM_IDLE;
            if (Ceremony == 1) return ANIM_POSE;
            if (Ceremony == 2) return ANIM_WALK;
            if (Ceremony == 3) return ANIM_HOP;
            if (Ceremony == 4) return ANIM_POSE;

            if (IsAnyMiniGameOpen) return ANIM_IDLE;
            if (_tempActionTimer > 0) return _tempActionId;
            if (IsSleeping) return ANIM_SLEEP;
            if (Poops > 0 || Hygiene < 30 || LowestStat < 30) return ANIM_HURT;
            return ANIM_IDLE;
        }

        // 🌟 수정됨: 외부(파티 UI 등)에서 애니메이션을 갱신할 수 있도록 public 선언
        public void CheckStateAndAnimate()
        {
            OnPropertyChanged(nameof(MoodText));
            OnPropertyChanged(nameof(CanEvolveNow));
            OnPropertyChanged(nameof(IsEating));
            OnPropertyChanged(nameof(IsPlaying));

            int targetAction = GetTargetAction();
            if (_currentActionId != targetAction || _animationFrames == null)
                UpdateAnimation(targetAction);
        }

        private void UpdateAnimation(int actionToLoad)
        {
            _currentActionId = actionToLoad;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (IsEgg)
            {
                string eggPath = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", "egg.png");
                if (File.Exists(eggPath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(eggPath);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    _animationFrames = new BitmapSource[] { bitmap };
                    CurrentFrame = bitmap;
                }
                return;
            }

            BitmapSource[]? frames = null;
            string normalPath = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"p{SpeciesId:D3}.bin");
            string shinyPath = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"ps{SpeciesId:D3}.bin");

            if (IsShiny && File.Exists(shinyPath)) frames = Tpk2Decoder.LoadAnimation(shinyPath, actionToLoad);
            if (frames == null && File.Exists(normalPath)) frames = Tpk2Decoder.LoadAnimation(normalPath, actionToLoad);

            if (frames == null && actionToLoad != ANIM_IDLE)
            {
                if (IsShiny && File.Exists(shinyPath)) frames = Tpk2Decoder.LoadAnimation(shinyPath, ANIM_IDLE);
                if (frames == null && File.Exists(normalPath)) frames = Tpk2Decoder.LoadAnimation(normalPath, ANIM_IDLE);
            }

            if (frames != null && frames.Length > 0) { _animationFrames = frames; _currentFrameIndex = 0; CurrentFrame = frames[0]; }
        }

        private void UpdateEnemyAnimation(int actionToLoad)
        {
            _enemyTempActionId = actionToLoad;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string enemyPath = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"p{EnemySpeciesId:D3}.bin");

            if (File.Exists(enemyPath))
            {
                var frames = Tpk2Decoder.LoadAnimation(enemyPath, actionToLoad);
                if (frames != null && frames.Length > 0) EnemyCurrentFrame = frames[0];
            }
        }

        private void UpdateDayNightCycle() { if (IsSleeping) { TimeOfDay = 3; return; } int hour = DateTime.Now.Hour; if (hour < 6 || hour >= 20) TimeOfDay = 3; else if (hour < 8) TimeOfDay = 0; else if (hour < 18) TimeOfDay = 1; else TimeOfDay = 2; }

        public void CheckDailyStreak()
        {
            DateTime today = DateTime.Now.Date;
            int daysPassed = (today - LastPlayedDate.Date).Days;
            if (daysPassed == 1) Streak++;
            else if (daysPassed > 1) Streak = 0;
            LastPlayedDate = today;
        }

        public int CareBonus() { int s = Streak > 30 ? 30 : Streak; return (s / 3) + (Bond / 25); }

        #endregion

        #region 저장 및 불러오기 (Save & Load)
        private static readonly string SaveFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save.json");
        public static bool CheckSaveFileExists() => File.Exists(SaveFilePath);

        public void Save()
        {
            try { var options = new JsonSerializerOptions { WriteIndented = true }; File.WriteAllText(SaveFilePath, JsonSerializer.Serialize(this, options)); }
            catch (Exception ex) { Console.WriteLine($"저장 실패: {ex.Message}"); }
        }

        public static PokemonState Load()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    var pet = JsonSerializer.Deserialize<PokemonState>(File.ReadAllText(SaveFilePath));
                    if (pet != null)
                    {
                        pet.InitializeAfterLoad();
                        return pet;
                    }
                }
                catch { }
            }

            PokemonState newPet = new PokemonState();
            newPet.MonsterBalls = 5;
            newPet.Potions = 3;

            newPet.InitializeAfterLoad();
            return newPet;
        }
        #endregion

        #region 디버그용
        [JsonIgnore]
        public bool IsDebugMode
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public void SkipTimeForTest()
        {
            if (IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;

            _ageSeconds += 3600;
            AgeMinutes += MINUTES_PER_LEVEL;
            IsEvolutionPostponed = false;

            TrAtk = Math.Min(100, TrAtk + 10);

            OnPropertyChanged(nameof(Level));
            OnPropertyChanged(nameof(LevelDisplay));
            OnPropertyChanged(nameof(CanEvolveNow));
            OnPropertyChanged(nameof(CanShowEvolvePrompt));
            OnPropertyChanged(nameof(EvolveProgressText));
            OnPropertyChanged(nameof(EvolveProgressPercent));

            // 🌟 추가된 부분: 시간이 지나서 레벨과 스탯이 변했으니, 파티 리스트의 0번(대표) 자리도 최신 상태로 갱신해 줍니다!
            if (Party != null && Party.Count > 0)
            {
                SyncMainToLeader();
            }

            CheckStateAndAnimate();
        }
        #endregion

        private int Clamp100(int value) => Math.Max(0, Math.Min(value, 100));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null) { if (EqualityComparer<T>.Default.Equals(field, newValue)) return false; field = newValue; OnPropertyChanged(propertyName); return true; }
    }
}