using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TamaPoke.Utils;
using TamaPoke.Utils.Service;

namespace TamaPoke.Models
{
    public partial class PokemonState : INotifyPropertyChanged, IJsonOnDeserializing, IJsonOnDeserialized
    {
        [JsonIgnore]
        public ObservableCollection<BadgeRegionGroup> BadgeGroups { get; set; } = new ObservableCollection<BadgeRegionGroup>();

        public static bool IsGamePaused { get; set; } = false;

        public void InitializeBadgesList()
        {
            BadgeGroups.Clear();

            // 배지 이미지 파일 이름에 쓰는 영어 지방명 + 세대 번호 (제목은 Lang의 gen.{n})
            var regions = new (string En, int Gen)[]
            {
                ("Kanto", 1), ("Johto", 2), ("Hoenn", 3), ("Sinnoh", 4),
                ("Unova", 5), ("Kalos", 6), ("Galar", 8), ("Paldea", 9)
            };

            int globalIndex = 1;

            foreach (var region in regions)
            {
                var group = new BadgeRegionGroup { HeaderText = Loc.T($"gen.{region.Gen}") };

                for (int i = 1; i <= 8; i++)
                {
                    group.Badges.Add(new GymBadgeInfo
                    {
                        RegionName = region.En,
                        BadgeNumber = i,
                        GlobalIndex = globalIndex
                    });
                    globalIndex++;
                }
                BadgeGroups.Add(group);
            }
        }

        // 설정은 settings.json 하나만 원본으로 사용합니다. (save.json에는 저장하지 않음)
        [JsonIgnore] public GameSettings Settings => SettingsManager.Current;

        #region 상수 및 열거형 (Constants)

        private const int MAX_POOPS = 3;
        private const int POOP_CHANCE = 5;
        private const int MINUTES_PER_LEVEL = 60;
        private const int RUNAWAY_TICKS = 60;
        private const int CARE_MISTAKE_THRESHOLD = 10;          // 이 값 이하로 떨어진 스탯이 있으면 케어 실수
        private const int CARE_MISTAKE_COOLDOWN_MINUTES = 60;   // 같은 방치를 다시 세기까지 (원작 60분)
        private const int WEIGHT_BURN_EVERY_MINUTES = 3;        // 몸무게 자연 감소 주기

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
            480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 493,
            494, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649,
            716, 717, 718, 719, 720, 721,
            772, 773, 785, 786, 787, 788, 789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807, 808, 809,
            888, 889, 890, 891, 892, 893, 894, 895, 896, 897, 898, 905,
            1001, 1002, 1003, 1004, 1007, 1008, 1014, 1015, 1016, 1017, 1024, 1025
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

        private int _weight = 0;
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

        public event Action<string, string>? TrayNotificationRequested;
        private bool _notifiedHunger = false;
        private bool _notifiedSadness = false;

        #region 포켓몬 정보 및 상태 속성 (Pokemon Info & Status)
        private bool _isGenderless = false;
        public bool IsGenderless
        {
            get => _isGenderless;
            set => SetProperty(ref _isGenderless, value);
        }

        private bool _isFemale = false;
        public bool IsFemale
        {
            get => _isFemale;
            set => SetProperty(ref _isFemale, value);
        }

        [JsonIgnore]
        public string GenderDisplay => IsEgg ? "" : (IsFemale ? "♀" : "♂");

        [JsonIgnore]
        public System.Windows.Media.Brush GenderColor => IsFemale ? BrushCache.Get("#E91E63") : BrushCache.Get("#1976D2");

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

                    OnPropertyChanged(nameof(Type1));
                    OnPropertyChanged(nameof(Type2));
                    OnPropertyChanged(nameof(HasType2));

                    UpdateBackgroundImage();
                    _animationFrames = null; _currentActionId = -1; CheckStateAndAnimate(); CheckMedals();
                }
            }
        }

        // 폼(알로라/가라르 등)별 타입과 설명은 PokemonForms에서 결정합니다.
        [JsonIgnore] public PokemonType Type1 => IsEgg ? PokemonType.Normal : PokemonForms.GetTypes(SpeciesId, SpriteFileName).Type1;
        [JsonIgnore] public PokemonType Type2 => IsEgg ? PokemonType.None : PokemonForms.GetTypes(SpeciesId, SpriteFileName).Type2;
        [JsonIgnore] public string FormDescription => IsEgg ? "" : PokemonForms.GetDescription(SpeciesId, SpriteFileName, IsShiny);

        [JsonIgnore]
        public bool HasType2 => Type2 != PokemonType.None;

        private PokemonGene _genes = new PokemonGene();
        public PokemonGene Genes { get => _genes; set => SetProperty(ref _genes, value); }

        private bool _isShiny = false;
        public bool IsShiny { get => _isShiny; set => SetProperty(ref _isShiny, value); }

        private bool _isSleeping = false;
        public bool IsSleeping { get => _isSleeping; set { if (SetProperty(ref _isSleeping, value)) { if (value) { PosX = 0; PosY = 0; FlipX = 1; TimeOfDay = 3; } else UpdateDayNightCycle(); CheckStateAndAnimate(); OnPropertyChanged(nameof(CanFarewellNow)); OnPropertyChanged(nameof(CanRunawayNow)); OnPropertyChanged(nameof(CanEvolveNow)); } } }

        [JsonIgnore] public bool IsEgg => SpeciesId < 0;

        private string? _overrideName;
        [JsonIgnore]
        public string Name
        {
            get
            {
                if (_overrideName != null) return _overrideName;
                if (IsEgg) return Loc.T("pokemon.egg");

                string baseName = PokemonDex.GetName(SpeciesId).Replace("♀", "").Replace("♂", "");

                return baseName + (IsShiny ? " ✨" : "");
            }
            set { _overrideName = value; OnPropertyChanged(nameof(Name)); }
        }

        private int? _overrideLevel;
        [JsonIgnore]
        public int Level
        {
            get => _overrideLevel ?? (1 + (AgeMinutes / MINUTES_PER_LEVEL));
            set { _overrideLevel = value; OnPropertyChanged(nameof(Level)); OnPropertyChanged(nameof(LevelDisplay)); }
        }

        [JsonIgnore] public int LowestStat => Math.Min(Math.Min(Fullness, Joy), Math.Min(Energy, Hygiene));
        [JsonIgnore] public bool IsFinalEvolution => !IsEgg && (PokemonDex.Get(SpeciesId)?.EvolveTo == 0);
        [JsonIgnore] public string LevelDisplay => IsEgg ? "" : $"Lv.{Level}";

        public int Streak { get; set; } = 0;
        public DateTime LastPlayedDate { get; set; } = DateTime.Now.Date;
        [JsonIgnore] public int RegisteredCount => UnlockedPokemon?.Count ?? 0;
        public int LastEnd { get; set; } = 1;

        private int _timeOfDay = 1;
        [JsonIgnore] public int TimeOfDay
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
            var p = PokemonDex.Get(SpeciesId);
            if (p == null) return 0;

            switch (Type1)
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
        [JsonIgnore] public System.Windows.Media.Brush SkyColor => BrushCache.Get(TimeOfDay == 0 ? "#E29181" : TimeOfDay == 1 ? "#B5DBE8" : TimeOfDay == 2 ? "#DC8457" : "#151C35");
        [JsonIgnore] public System.Windows.Media.Brush GrassColor => BrushCache.Get(IsNight ? "#161C30" : Biome == 0 ? "#7EC07F" : Biome == 1 ? "#DCCA94" : Biome == 2 ? "#4F8A55" : Biome == 3 ? "#8A5544" : Biome == 4 ? "#A8906A" : "#E6EEF5");
        [JsonIgnore] public System.Windows.Media.Brush GroundColor => BrushCache.Get(IsNight ? "#222638" : "#F2EFE1");

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
            string path = Path.Combine(AppPaths.BackgroundFolder, fileName);

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

        #region 메인 UI 상태 (Main UI)

        private bool _isMuted = true;
        public bool IsMuted { get => _isMuted; set { if (SetProperty(ref _isMuted, value)) OnPropertyChanged(nameof(SoundIcon)); } }
        [JsonIgnore] public string SoundIcon => IsMuted ? "🔇" : "🔊";

        private string _evolutionMessage = "";
        [JsonIgnore] public string EvolutionMessage { get => _evolutionMessage; set { SetProperty(ref _evolutionMessage, value); OnPropertyChanged(nameof(MoodText)); } }

        private bool _isEvolvingFlash = false;
        [JsonIgnore] public bool IsEvolvingFlash { get => _isEvolvingFlash; set => SetProperty(ref _isEvolvingFlash, value); }

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

        private string _eventMessage = string.Empty;

        public async Task ShowEventMessageAsync(string message, int displaySeconds = 3)
        {
            _eventMessage = message;
            OnPropertyChanged(nameof(MoodText));

            await Task.Delay(displaySeconds * 1000);

            _eventMessage = string.Empty;
            OnPropertyChanged(nameof(MoodText));
        }

        [JsonIgnore]
        public string MoodText
        {
            get
            {
                if (Ceremony == 4) return string.IsNullOrEmpty(EvolutionMessage) ? Loc.T("mood.evolving") : EvolutionMessage;

                // 잠깐 보여주는 이벤트 메시지 (기술 습득 결과, 산책 요약 등)
                if (!string.IsNullOrEmpty(_eventMessage)) return _eventMessage;

                if (IsBattleOpen) return Loc.T("mood.battle");
                if (IsProfileOpen) return Loc.T("mood.profile");
                if (IsBallGameOpen) return Loc.T("mood.game.ball");
                if (IsCatchGameOpen) return Loc.T("mood.game.catch");
                if (IsMemoGameOpen) return Loc.T("mood.game.memo");
                if (IsCleanGameOpen) return Loc.T("mood.game.clean");
                if (Ceremony == 1) return Loc.T("mood.farewell");
                if (Ceremony == 2) return Loc.T("mood.runaway");
                if (Ceremony == 3) return Loc.T("mood.release");
                if (IsEgg) return Loc.T("mood.egg");
                if (IsSleeping) return Loc.T("mood.sleeping");
                if (CanRunawayNow) return Loc.T("mood.lonely");
                if (CanFarewellNow) return Loc.T("mood.wantsToTalk");
                if (Poops > 0) return Loc.T("mood.poop");
                if (Hygiene < 30) return Loc.T("mood.dirty");
                if (LowestStat < 30) return Loc.T("mood.veryBad");
                if (LowestStat < 50) return Loc.T("mood.tired");
                if (Fullness > 80 && Joy > 80) return Loc.T("mood.veryHappy");
                return Loc.T("mood.good");
            }
        }
        #endregion

        #region 메뉴 UI 상태 (Menus)
        public List<int> UnlockedPokemon { get; set; } = new List<int>();

        private bool _isProfileOpen = false;
        [JsonIgnore] public bool IsProfileOpen
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
        [JsonIgnore] public int ProfilePage { get => _profilePage; set { SetProperty(ref _profilePage, value); OnPropertyChanged(nameof(ProfilePageDisplay)); } }
        [JsonIgnore] public string ProfilePageDisplay => Loc.F("profile.page", ProfilePage + 1);

        private bool _berryKnown = false;
        public bool BerryKnown { get => _berryKnown; set { if (SetProperty(ref _berryKnown, value)) CheckMedals(); } }
        [JsonIgnore] public int FavoriteBerry => Math.Abs(SpeciesId) % 3;
        [JsonIgnore] public string FavoriteBerryName => !BerryKnown ? "???" : (FavoriteBerry == 0 ? Loc.T("berry.red") : FavoriteBerry == 1 ? Loc.T("berry.blue") : Loc.T("berry.green"));

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
                    OnPropertyChanged(nameof(Level));
                    OnPropertyChanged(nameof(LevelDisplay));
                    OnPropertyChanged(nameof(CanFarewellNow));
                    OnPropertyChanged(nameof(CanShowFarewellPrompt));
                    OnPropertyChanged(nameof(CanEvolveNow));
                    OnPropertyChanged(nameof(CanShowEvolvePrompt));
                    OnPropertyChanged(nameof(EvolveProgressText));
                    OnPropertyChanged(nameof(EvolveProgressPercent));
                    OnPropertyChanged(nameof(LevelProgressMinutes));
                    CheckMedals();
                }
            }
        }

        private int _careMistakes = 0;
        private int _mistakeCooldown = 0; // 케어 실수를 다시 세기까지 남은 분
        private int _goodTicks = 0;
        public int CareMistakes { get => _careMistakes; set { if (SetProperty(ref _careMistakes, value)) { OnPropertyChanged(nameof(CanEvolveNow)); OnPropertyChanged(nameof(EvolveProgressText)); } } }

        private int _neglectTicks = 0;
        public int NeglectTicks { get => _neglectTicks; set { SetProperty(ref _neglectTicks, value); OnPropertyChanged(nameof(CanRunawayNow)); } }
        private int _eggTaps = 0;
        [JsonIgnore] public int EggTaps { get => _eggTaps; set => SetProperty(ref _eggTaps, value); }

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

                if (SpeciesId == 415 && !IsFemale) return false;
                if (GetEvolutionTargets().Length == 0) return false; // 진화할 포켓몬의 이미지가 없음

                return Level >= (DexTable[SpeciesId].EvolveLevel + CareMistakes) && LowestStat >= 40;
            }
        }

        [JsonIgnore] public bool CanFarewellNow => !IsEgg && !IsSleeping && Ceremony == 0 && AgeMinutes >= (Settings.FarewellAgeDays * 24 * 60) && !IsAnyMiniGameOpen && !IsProfileOpen && !IsBattleOpen;
        [JsonIgnore] public bool CanRunawayNow => !IsEgg && !IsSleeping && Ceremony == 0 && NeglectTicks >= RUNAWAY_TICKS && !IsAnyMiniGameOpen && !IsProfileOpen && !IsBattleOpen;

        [JsonIgnore]
        public string EvolveProgressText
        {
            get
            {
                if (IsEgg) return "";
                if (IsFinalEvolution) return Loc.T("evolve.progress.final");
                if (!DexTable.ContainsKey(SpeciesId)) return Loc.T("evolve.progress.noInfo");
                int targetLv = DexTable[SpeciesId].EvolveLevel + CareMistakes;
                int remaining = targetLv - Level;
                return remaining <= 0 ? (LowestStat >= 40 ? Loc.T("evolve.progress.ready") : Loc.T("evolve.progress.lowCondition")) : Loc.F("evolve.progress.remaining", remaining);
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

        // 현재 레벨 안에서 지난 시간(분). 프로필의 "다음 레벨까지" 막대용 (0~59)
        [JsonIgnore] public int LevelProgressMinutes => AgeMinutes % MINUTES_PER_LEVEL;

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

        #region 프로필 페이지 (Profile)
        public void NextProfilePage() { ProfilePage = (ProfilePage + 1) % 6; }
        public void PrevProfilePage() { ProfilePage = (ProfilePage + 5) % 6; }
        #endregion

        #region 내부 헬퍼 (Helpers)
        public Action? RequestCatchAnimation;
        // 세이브를 읽거나 파티 멤버를 대표로 불러오는 동안에는 메달을 판정하지 않습니다.
        // (속성이 하나씩 채워지는 중이라, 예: 나이는 읽었는데 케어 실수는 아직 0인 상태로 잘못 판정될 수 있음)
        private bool _suspendMedalCheck = false;
        void IJsonOnDeserializing.OnDeserializing() => _suspendMedalCheck = true;
        void IJsonOnDeserialized.OnDeserialized() => _suspendMedalCheck = false;

        private void CheckMedals()
        {
            if (IsEgg || _suspendMedalCheck) return;
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

        private int Clamp100(int value) => Math.Max(0, Math.Min(value, 100));
        #endregion

        #region 디버그용 (Debug Tools)
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
            IsFarewellPostponed = false;

            TrAtk = Math.Min(100, TrAtk + 10);
            TrDef = Math.Min(100, TrDef + 10);

            OnPropertyChanged(nameof(Level));
            OnPropertyChanged(nameof(LevelDisplay));
            OnPropertyChanged(nameof(CanEvolveNow));
            OnPropertyChanged(nameof(CanShowEvolvePrompt));
            OnPropertyChanged(nameof(EvolveProgressText));
            OnPropertyChanged(nameof(EvolveProgressPercent));

            if (Party != null && Party.Count > 0)
            {
                SyncMainToLeader();
            }

            CheckStateAndAnimate();
        }
        #endregion

        #region 🌟 INotifyPropertyChanged 구현 (UI Update Notifications)
        public event PropertyChangedEventHandler? PropertyChanged;

        // 언어가 바뀌었을 때: 모든 바인딩을 다시 읽게 해서 코드에서 만든 문구(기분, 진화 진행 등)를 새 언어로 갱신
        public void RefreshLocalizedTexts()
        {
            OnPropertyChanged(string.Empty);
            InitializeBadgesList();                          // 배지 지역 제목
            foreach (var item in Inventory) item.RefreshTexts(); // 아이템 이름/설명
            foreach (var member in Party) member.RefreshTexts();  // 파티 포켓몬 이름
            RefreshPokedex();                                     // 도감 종 이름
        }

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue)) return false;
            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}