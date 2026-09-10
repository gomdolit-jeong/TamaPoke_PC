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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpriteImage)));
                }
            }
        }
        public string DisplayName => IsUnlocked ? SpeciesName : "???";

        [JsonIgnore]
        public ImageSource? SpriteImage
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string spriteFolder = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites");

                if (Directory.Exists(spriteFolder))
                {
                    string pattern = $"p{Id:D4}*.bin";
                    var matchedFiles = Directory.GetFiles(spriteFolder, pattern);

                    if (matchedFiles.Length > 0)
                    {
                        return BinSpriteReader.LoadPokedexThumbnail(matchedFiles[0]);
                    }
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class PokemonState : INotifyPropertyChanged
    {
        [JsonIgnore]
        public ObservableCollection<BadgeRegionGroup> BadgeGroups { get; set; } = new ObservableCollection<BadgeRegionGroup>();

        public void InitializeBadgesList()
        {
            BadgeGroups.Clear();

            var regions = new (string En, string Ko)[]
            {
                ("Kanto", "1세대 관동지방"),
                ("Johto", "2세대 성도지방"),
                ("Hoenn", "3세대 호연지방"),
                ("Sinnoh", "4세대 신오지방"),
                ("Unova", "5세대 하나지방"),
                ("Kalos", "6세대 칼로스지방"),
                ("Galar", "8세대 가라르지방"),
                ("Paldea", "9세대 팔데아지방")
            };

            int globalIndex = 1;

            foreach (var region in regions)
            {
                var group = new BadgeRegionGroup { HeaderText = region.Ko };

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

        private GameSettings _settings = new GameSettings();
        public GameSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        #region 상수 및 열거형 (Constants)
        public const int ANIM_WALK = 0;
        public const int ANIM_ATTACK = 1;
        public const int ANIM_STRIKE = 2;
        public const int ANIM_SHOOT = 3;
        public const int ANIM_SHAKE = 4;
        public const int ANIM_SLEEP = 5;
        public const int ANIM_HURT = 6;
        public const int ANIM_IDLE = 7;
        public const int ANIM_SWING = 8;
        public const int ANIM_DOUBLE = 9;
        public const int ANIM_HOP = 10;
        public const int ANIM_CHARGE = 11;
        public const int ANIM_ROTATE = 12;
        public const int ANIM_EVENTSLEEP = 13;
        public const int ANIM_WAKE = 14;
        public const int ANIM_EAT = 15;
        public const int ANIM_TUMBLE = 16;
        public const int ANIM_POSE = 17;
        public const int ANIM_PULL = 18;
        public const int ANIM_PAIN = 19;
        public const int ANIM_FLOAT = 20;
        public const int ANIM_DEEPBREATH = 21;
        public const int ANIM_NOD = 22;
        public const int ANIM_SIT = 23;
        public const int ANIM_LOOKUP = 24;
        public const int ANIM_SINK = 25;
        public const int ANIM_TRIP = 26;
        public const int ANIM_LAYING = 27;
        public const int ANIM_LEAPFORTH = 28;
        public const int ANIM_HEAD = 29;
        public const int ANIM_CRINGE = 30;
        public const int ANIM_LOSTBALANCE = 31;
        public const int ANIM_TUMBLEBACK = 32;
        public const int ANIM_FAINT = 33;
        public const int ANIM_HITGROUND = 34;

        public static int GetTotalFramesForMotion(int animIndex)
        {
            switch (animIndex)
            {
                case ANIM_WALK: return 48;
                case ANIM_ATTACK: return 88;
                case ANIM_STRIKE: return 88;
                case ANIM_SHOOT: return 48;
                case ANIM_SHAKE: return 48;
                case ANIM_SLEEP: return 16;
                case ANIM_HURT: return 16;
                case ANIM_IDLE: return 24;
                case ANIM_SWING: return 72;
                case ANIM_DOUBLE: return 128;
                case ANIM_HOP: return 80;
                case ANIM_CHARGE: return 80;
                case ANIM_ROTATE: return 72;
                case ANIM_EVENTSLEEP: return 16;
                case ANIM_WAKE: return 48;
                case ANIM_EAT: return 32;
                case ANIM_TUMBLE: return 64;
                case ANIM_POSE: return 40;
                case ANIM_PULL: return 56;
                case ANIM_PAIN: return 96;
                case ANIM_FLOAT: return 32;
                case ANIM_DEEPBREATH: return 72;
                case ANIM_NOD: return 24;
                case ANIM_SIT: return 24;
                case ANIM_LOOKUP: return 16;
                case ANIM_SINK: return 96;
                case ANIM_TRIP: return 40;
                case ANIM_LAYING: return 8;
                case ANIM_LEAPFORTH: return 48;
                case ANIM_HEAD: return 8;
                case ANIM_CRINGE: return 16;
                case ANIM_LOSTBALANCE: return 16;
                case ANIM_TUMBLEBACK: return 80;
                case ANIM_FAINT: return 32;
                case ANIM_HITGROUND: return 64;
                default: return 24;
            }
        }

        private const int MAX_POOPS = 3;
        private const int POOP_CHANCE = 5;
        private const int MINUTES_PER_LEVEL = 60;
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
            // 1세대 (관동)
            144, 145, 146, 150, 151,
            
            // 2세대 (성도)
            243, 244, 245, 249, 250, 251,
            
            // 3세대 (호연)
            377, 378, 379, 380, 381, 382, 383, 384, 385, 386,
            
            // 4세대 (신오)
            480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 493,
            
            // 5세대 (하나) - 비크티니 ~ 게노세크트
            494, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649,
            
            // 6세대 (칼로스) - 제르네아스 ~ 볼케니온
            716, 717, 718, 719, 720, 721,
            
            // 7세대 (알로라) - 타입:널, 수호신, 울트라비스트, 환상 등
            772, 773, 785, 786, 787, 788, 789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807, 808, 809,
            
            // 8세대 (가라르) - 자시안, 무한다이노, 러스트보이 등 + 러브로스(히스이)
            888, 889, 890, 891, 892, 893, 894, 895, 896, 897, 898, 905,
            
            // 9세대 (팔데아) - 사흉수, 코라이돈/미라이돈, DLC 전설/환상
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
        // 성별 여부 프로퍼티
        private bool _isFemale = false;
        public bool IsFemale
        {
            get => _isFemale;
            set => SetProperty(ref _isFemale, value);
        }

        // UI에 텍스트로 띄워줄 성별 기호 속성
        [JsonIgnore]
        public string GenderDisplay => IsEgg ? "" : (IsFemale ? "♀" : "♂");

        [JsonIgnore]
        public System.Windows.Media.Brush GenderColor => IsFemale ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#E91E63")! : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#1976D2")!;

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

        // ==========================================
        // 🌟 [최적화됨] 강력한 리전 폼 타입 오버라이드 리스트
        // 특정 폼(블레이즈, 워터)을 최상단에 배치하여 우선적으로 검사하도록 순서를 바꿨습니다!
        // ==========================================
        private static readonly List<(int Id, string Keyword, PokemonType Type1, PokemonType Type2)> FormTypeOverrides = new()
        {
            // 🌴 알로라의 모습 (7세대)
            (19, "alola", PokemonType.Dark, PokemonType.Normal),
            (20, "alola", PokemonType.Dark, PokemonType.Normal),
            (26, "alola", PokemonType.Electric, PokemonType.Psychic),
            (27, "alola", PokemonType.Ice, PokemonType.Steel),
            (28, "alola", PokemonType.Ice, PokemonType.Steel),
            (37, "alola", PokemonType.Ice, PokemonType.None),
            (38, "alola", PokemonType.Ice, PokemonType.Fairy),
            (50, "alola", PokemonType.Ground, PokemonType.Steel),
            (51, "alola", PokemonType.Ground, PokemonType.Steel),
            (52, "alola", PokemonType.Dark, PokemonType.None),
            (53, "alola", PokemonType.Dark, PokemonType.None),
            (74, "alola", PokemonType.Rock, PokemonType.Electric),
            (75, "alola", PokemonType.Rock, PokemonType.Electric),
            (76, "alola", PokemonType.Rock, PokemonType.Electric),
            (88, "alola", PokemonType.Poison, PokemonType.Dark),
            (89, "alola", PokemonType.Poison, PokemonType.Dark),
            (103, "alola", PokemonType.Grass, PokemonType.Dragon),
            (105, "alola", PokemonType.Fire, PokemonType.Ghost),

            // 🚂 가라르의 모습 (8세대)
            (52, "galar", PokemonType.Steel, PokemonType.None),
            (77, "galar", PokemonType.Psychic, PokemonType.None),
            (78, "galar", PokemonType.Psychic, PokemonType.Fairy),
            (79, "galar", PokemonType.Poison, PokemonType.Psychic),
            (80, "galar", PokemonType.Poison, PokemonType.Psychic),
            (199, "galar", PokemonType.Poison, PokemonType.Psychic),
            (83, "galar", PokemonType.Fighting, PokemonType.None),
            (110, "galar", PokemonType.Poison, PokemonType.Fairy),
            (122, "galar", PokemonType.Ice, PokemonType.Psychic),
            (144, "galar", PokemonType.Psychic, PokemonType.Flying),
            (145, "galar", PokemonType.Fighting, PokemonType.Flying),
            (146, "galar", PokemonType.Dark, PokemonType.Flying),
            (222, "galar", PokemonType.Ghost, PokemonType.None),
            (263, "galar", PokemonType.Dark, PokemonType.Normal),
            (264, "galar", PokemonType.Dark, PokemonType.Normal),
            (554, "galar", PokemonType.Ice, PokemonType.None),
            (555, "zen", PokemonType.Ice, PokemonType.Fire),
            (555, "galar", PokemonType.Ice, PokemonType.None),
            (562, "galar", PokemonType.Ground, PokemonType.Ghost),
            (618, "galar", PokemonType.Ground, PokemonType.Steel),

            // 🏔️ 히스이의 모습 (포켓몬 레전즈 아르세우스)
            (58, "hisui", PokemonType.Fire, PokemonType.Rock),
            (59, "hisui", PokemonType.Fire, PokemonType.Rock),
            (100, "hisui", PokemonType.Electric, PokemonType.Grass),
            (101, "hisui", PokemonType.Electric, PokemonType.Grass),
            (157, "hisui", PokemonType.Fire, PokemonType.Ghost),
            (211, "hisui", PokemonType.Dark, PokemonType.Poison),
            (215, "hisui", PokemonType.Fighting, PokemonType.Poison),
            (503, "hisui", PokemonType.Water, PokemonType.Dark),
            (549, "hisui", PokemonType.Grass, PokemonType.Fighting),
            (550, "hisui", PokemonType.Water, PokemonType.None),
            (570, "hisui", PokemonType.Normal, PokemonType.Ghost),
            (571, "hisui", PokemonType.Normal, PokemonType.Ghost),
            (628, "hisui", PokemonType.Psychic, PokemonType.Flying),
            (705, "hisui", PokemonType.Steel, PokemonType.Dragon),
            (706, "hisui", PokemonType.Steel, PokemonType.Dragon),
            (713, "hisui", PokemonType.Ice, PokemonType.Rock),
            (724, "hisui", PokemonType.Grass, PokemonType.Fighting),

            // 🇪🇸 팔데아의 모습 (9세대)
            (194, "paldea", PokemonType.Poison, PokemonType.Ground), // 우파
            
            // 🌟 [핵심] 켄타로스: 특수 폼(블레이즈, 워터)을 먼저 검사하도록 순서 배치
            (128, "blaze", PokemonType.Fighting, PokemonType.Fire),
            (128, "_0002", PokemonType.Fighting, PokemonType.Fire),
            (128, "aqua", PokemonType.Fighting, PokemonType.Water),
            (128, "_0003", PokemonType.Fighting, PokemonType.Water),
            (128, "combat", PokemonType.Fighting, PokemonType.None),
            (128, "_0001", PokemonType.Fighting, PokemonType.None),
            // 위의 디테일한 키워드가 없을 때, 단순 'paldea'만 있어도 무조건 격투로 빠지게 방어!
            (128, "paldea", PokemonType.Fighting, PokemonType.None)
        };

        [JsonIgnore]
        public PokemonType Type1
        {
            get
            {
                if (IsEgg) return PokemonType.Normal;

                if (!string.IsNullOrEmpty(SpriteFileName))
                {
                    string lowerFile = SpriteFileName.ToLower();

                    var overrideData = FormTypeOverrides.FirstOrDefault(x => x.Id == SpeciesId && lowerFile.Contains(x.Keyword));

                    if (overrideData.Id != 0)
                    {
                        return overrideData.Type1;
                    }
                }

                return PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId)?.Type1 ?? PokemonType.Normal;
            }
        }

        [JsonIgnore]
        public PokemonType Type2
        {
            get
            {
                if (IsEgg) return PokemonType.None;

                if (!string.IsNullOrEmpty(SpriteFileName))
                {
                    string lowerFile = SpriteFileName.ToLower();

                    var overrideData = FormTypeOverrides.FirstOrDefault(x => x.Id == SpeciesId && lowerFile.Contains(x.Keyword));

                    if (overrideData.Id != 0)
                    {
                        return overrideData.Type2;
                    }
                }

                return PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId)?.Type2 ?? PokemonType.None;
            }
        }

        [JsonIgnore]
        public bool HasType2 => Type2 != PokemonType.None;
        // ==========================================

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
                if (IsEgg) return "알";

                // 🌟 도감 원본 이름에서 암수 기호를 깔끔하게 지웁니다.
                string baseName = PokemonDex.GetName(SpeciesId).Replace("♀", "").Replace("♂", "");

                return baseName + (IsShiny ? " ✨" : "");
            }
            set { _overrideName = value; OnPropertyChanged(nameof(Name)); }
        }

        [JsonIgnore]
        public string FormDescription
        {
            get
            {
                if (IsEgg) return "";

                string formText = "";
                string shinyText = IsShiny ? "✨ 색이 다른 포켓몬" : "";

                if (!string.IsNullOrEmpty(SpriteFileName))
                {
                    string lowerFile = SpriteFileName.ToLower();

                    if (lowerFile.Contains("shiny"))
                    {
                        shinyText = "✨ 색이 다른 포켓몬";
                    }

                    // 🌟 리전 폼 및 특수 폼 키워드 매핑 사전
                    var formKeywords = new Dictionary<string, string>
            {
                { "combat", "팔데아의 모습 (투쟁종)" },
                { "_0001", "팔데아의 모습 (투쟁종)" },
                { "blaze", "팔데아의 모습 (블레이즈종)" },
                { "_0002", "팔데아의 모습 (블레이즈종)" },
                { "aqua", "팔데아의 모습 (워터종)" },
                { "_0003", "팔데아의 모습 (워터종)" },
                { "alola", "알로라의 모습" },
                { "galar", "가라르의 모습" },
                { "hisui", "히스이의 모습" },
                { "paldea", "팔데아의 모습" },
                { "mega", "메가진화" }
            };

                    // 파일명에 등록된 키워드가 포함되어 있는지 확인합니다.
                    foreach (var pair in formKeywords)
                    {
                        if (lowerFile.Contains(pair.Key))
                        {
                            // 켄타로스(128번)가 아닌데 _0001 등이 걸리는 것을 방지하기 위한 안전장치
                            if ((pair.Key == "_0001" || pair.Key == "_0002" || pair.Key == "_0003") && SpeciesId != 128)
                                continue;

                            formText = pair.Value;
                            break;
                        }
                    }

                    // 사전에 등록되지 않은 기타 특수 형태 처리
                    if (string.IsNullOrEmpty(formText) &&
                        !lowerFile.Contains("_0000") &&
                        !lowerFile.Contains("egg") &&
                        !lowerFile.Contains("shiny") &&
                        lowerFile.Contains("_"))
                    {
                        formText = "특수한 모습";
                    }
                }

                if (!string.IsNullOrEmpty(shinyText) && !string.IsNullOrEmpty(formText))
                    return $"{shinyText} ({formText})";
                else if (!string.IsNullOrEmpty(shinyText))
                    return shinyText;
                else if (!string.IsNullOrEmpty(formText))
                    return formText;

                return "";
            }
        }

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
        [JsonIgnore] public System.Windows.Media.Brush SkyColor => (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(TimeOfDay == 0 ? "#E29181" : TimeOfDay == 1 ? "#B5DBE8" : TimeOfDay == 2 ? "#DC8457" : "#151C35")!;
        [JsonIgnore] public System.Windows.Media.Brush GrassColor => (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(IsNight ? "#161C30" : Biome == 0 ? "#7EC07F" : Biome == 1 ? "#DCCA94" : Biome == 2 ? "#4F8A55" : Biome == 3 ? "#8A5544" : Biome == 4 ? "#A8906A" : "#E6EEF5")!;
        [JsonIgnore] public System.Windows.Media.Brush GroundColor => (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom(IsNight ? "#222638" : "#F2EFE1")!;

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
        public double TargetPosX { get; set; } = 0;
        public double TargetPosY { get; set; } = 0;

        private double _flipX = 1;
        public double FlipX { get => _flipX; set => SetProperty(ref _flipX, value); }

        public void ResetPosition()
        {
            PosX = 0; PosY = 0; TargetPosX = 0; TargetPosY = 0; FlipX = 1;
            _tempActionId = ANIM_IDLE;
            _tempActionTimer = 0;
            CheckStateAndAnimate();
        }

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

        [JsonIgnore] public string KantoCountText => $"({FullPokedex.Count(p => p.Id >= 1 && p.Id <= 151 && p.IsUnlocked)}/151)";
        [JsonIgnore] public string JohtoCountText => $"({FullPokedex.Count(p => p.Id >= 152 && p.Id <= 251 && p.IsUnlocked)}/100)";
        [JsonIgnore] public string HoennCountText => $"({FullPokedex.Count(p => p.Id >= 252 && p.Id <= 386 && p.IsUnlocked)}/135)";
        [JsonIgnore] public string SinnohCountText => $"({FullPokedex.Count(p => p.Id >= 387 && p.Id <= 493 && p.IsUnlocked)}/107)";
        [JsonIgnore] public string UnovaCountText => $"({FullPokedex.Count(p => p.Id >= 494 && p.Id <= 649 && p.IsUnlocked)}/156)";
        [JsonIgnore] public string KalosCountText => $"({FullPokedex.Count(p => p.Id >= 650 && p.Id <= 721 && p.IsUnlocked)}/72)";
        [JsonIgnore] public string AlolaCountText => $"({FullPokedex.Count(p => p.Id >= 722 && p.Id <= 809 && p.IsUnlocked)}/88)";
        [JsonIgnore] public string GalarCountText => $"({FullPokedex.Count(p => p.Id >= 810 && p.Id <= 905 && p.IsUnlocked)}/96)";
        [JsonIgnore] public string PaldeaCountText => $"({FullPokedex.Count(p => p.Id >= 906 && p.Id <= 1025 && p.IsUnlocked)}/120)";

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

        private bool _isShowOnlyUnlocked = false;
        public bool IsShowOnlyUnlocked
        {
            get => _isShowOnlyUnlocked;
            set
            {
                if (SetProperty(ref _isShowOnlyUnlocked, value))
                {
                    UpdateFilteredPokedex();
                }
            }
        }

        public void UpdateFilteredPokedex()
        {
            FilteredPokedex.Clear();
            int startId = 1, endId = 1025;

            switch (CurrentRegionIndex)
            {
                case 0: startId = 1; endId = 151; break;
                case 1: startId = 152; endId = 251; break;
                case 2: startId = 252; endId = 386; break;
                case 3: startId = 387; endId = 493; break;
                case 4: startId = 494; endId = 649; break;
                case 5: startId = 650; endId = 721; break;
                case 6: startId = 722; endId = 809; break;
                case 7: startId = 810; endId = 905; break;
                case 8: startId = 906; endId = 1025; break;
                default: startId = 1; endId = 1025; break;
            }

            foreach (var entry in FullPokedex.Where(p => p.Id >= startId && p.Id <= endId))
            {
                if (IsShowOnlyUnlocked && !entry.IsUnlocked)
                    continue;

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
        [JsonIgnore] public string ProfilePageDisplay => $"페이지 {ProfilePage + 1} / 6";

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

        public void ReloadSprite()
        {
            // 🌟 유저님이 이미 만들어두신 훌륭한 렌더링 시스템을 100% 활용합니다!
            // 프레임 캐시를 초기화하여, 다음 CheckStateAndAnimate() 호출 시 
            // 새로운 .bin 파일을 하드디스크에서 읽고 자르도록 유도합니다.
            _animationFrames = null;
            _currentActionId = -1;

            CheckStateAndAnimate();
        }

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

                // 🌟 [추가] 수컷 세꿀버리(415)는 비퀸(416)으로 진화 불가!
                if (SpeciesId == 415 && !IsFemale) return false;

                return Level >= (DexTable[SpeciesId].EvolveLevel + CareMistakes) && LowestStat >= 40;
            }
        }

        // 🌟 날짜(일) * 24시간 * 60분으로 계산하여 AgeMinutes와 비교합니다.
        [JsonIgnore] public bool CanFarewellNow => !IsEgg && !IsSleeping && Ceremony == 0 && AgeMinutes >= (Settings.FarewellAgeDays * 24 * 60) && !IsAnyMiniGameOpen && !IsProfileOpen && !IsBattleOpen;
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

            IsFarewellPostponed = false;

            if (DexTable.Count == 0)
            {
                InitializeEvolutionTable();
            }

            InitializeBadgesList();
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
                PosX += (-FlipX * 1.5);
                if (PosX >= 80)
                {
                    PosX = 80;
                    FlipX = 1;
                }
                else if (PosX <= -80)
                {
                    PosX = -80;
                    FlipX = -1;
                }
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
                    else if (r < 60) { int[] flair = { ANIM_POSE, ANIM_NOD, ANIM_DEEPBREATH }; _tempActionId = flair[moveRand.Next(flair.Length)]; _tempActionTimer = 90; }
                    else { _tempActionId = ANIM_IDLE; _tempActionTimer = moveRand.Next(60, 150); }
                    CheckStateAndAnimate();
                }
            }

            if (_ageSeconds % 60 == 0)
            {
                if (AgeMinutes % MINUTES_PER_LEVEL == 0)
                {
                    IsEvolutionPostponed = false;
                    IsFarewellPostponed = false;
                }

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

                if (Fullness > 40 && Poops < MAX_POOPS && rand.Next(100) < POOP_CHANCE)
                {
                    Poops++;
                    Hygiene = Clamp100(Hygiene - (10 * Poops));

                    if (Settings.UseTrayNotifications)
                        TrayNotificationRequested?.Invoke("화장실 알림", "💩");
                }

                if (AgeMinutes % 10 == 0) { int dJoy = 0; if (Fullness < 30) dJoy -= 2; if (Hygiene < 30) dJoy -= 3; Joy = Clamp100(Joy + dJoy); }

                if (Fullness == 0 && Joy == 0 && Energy == 0 && Hygiene == 0)
                {
                    if (NeglectTicks < RUNAWAY_TICKS)
                        NeglectTicks++;

                    if (CanRunawayNow)
                    {
                        StartRunaway();
                        return;
                    }
                }
                else
                {
                    NeglectTicks = 0;
                }

                if (Fullness < 30 && !_notifiedHunger)
                {
                    if (Settings.UseTrayNotifications)
                        TrayNotificationRequested?.Invoke("배고픔 알림", "🍚");

                    _notifiedHunger = true;
                }
                else if (Fullness >= 30)
                {
                    _notifiedHunger = false;
                }

                if (Joy < 30 && !_notifiedSadness)
                {
                    if (Settings.UseTrayNotifications)
                        TrayNotificationRequested?.Invoke("우울함 알림", "🥹");

                    _notifiedSadness = true;
                }
                else if (Joy >= 30)
                {
                    _notifiedSadness = false;
                }

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

        public void NextProfilePage() { ProfilePage = (ProfilePage + 1) % 6; }
        public void PrevProfilePage() { ProfilePage = (ProfilePage + 5) % 6; }
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
            if (Ceremony == 4) return ANIM_FLOAT;

            if (IsAnyMiniGameOpen) return ANIM_IDLE;
            if (_tempActionTimer > 0) return _tempActionId;

            if (IsSleeping) return ANIM_SLEEP;
            if (IsBathing) return ANIM_SHAKE;

            if (Poops > 0) return ANIM_PAIN;
            if (Hygiene < 30) return ANIM_HURT;
            if (Fullness < 30) return ANIM_CRINGE;
            if (Joy < 30) return ANIM_SINK;

            return ANIM_IDLE;
        }

        public void CheckStateAndAnimate()
        {
            OnPropertyChanged(nameof(MoodText));
            OnPropertyChanged(nameof(CanEvolveNow));
            OnPropertyChanged(nameof(CanShowEvolvePrompt));
            OnPropertyChanged(nameof(CanFarewellNow));
            OnPropertyChanged(nameof(CanShowFarewellPrompt));
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
            string spriteFolder = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites");

            List<SpriteFrameData>? rawStrips = null;

            if (IsEgg)
            {
                string eggPath = Path.Combine(spriteFolder, "p0000_Egg.bin");
                if (File.Exists(eggPath)) rawStrips = Utils.BinSpriteReader.LoadFramesFromBin(eggPath);
            }
            else
            {
                string targetPath = Path.Combine(spriteFolder, SpriteFileName);
                if (!File.Exists(targetPath)) targetPath = Path.Combine(spriteFolder, $"p{SpeciesId:D4}.bin");
                if (File.Exists(targetPath)) rawStrips = Utils.BinSpriteReader.LoadFramesFromBin(targetPath);
            }

            if (rawStrips != null && rawStrips.Count > 0)
            {
                int stripIndex = actionToLoad;
                if (stripIndex >= rawStrips.Count) stripIndex = rawStrips.Count - 1;
                if (stripIndex < 0) stripIndex = 0;

                var targetData = rawStrips[stripIndex];
                if (targetData != null && targetData.Image != null)
                {
                    int directionRow = 0;

                    switch (actionToLoad)
                    {
                        case ANIM_WALK:
                        case ANIM_SLEEP:
                        case ANIM_EVENTSLEEP:
                        case ANIM_LAYING:
                        case ANIM_ATTACK:
                        case ANIM_STRIKE:
                        case ANIM_SHOOT:
                        case ANIM_HOP:
                        case ANIM_CHARGE:
                        case ANIM_LEAPFORTH:
                        case ANIM_TUMBLE:
                        case ANIM_HURT:
                        case ANIM_PAIN:
                        case ANIM_CRINGE:
                        case ANIM_FAINT:
                            directionRow = 6;
                            break;

                        default:
                            directionRow = 0;
                            break;
                    }

                    List<BitmapSource> slicedFrames = SliceStripIntoFrames(targetData.Image, targetData.FrameWidth, targetData.FrameHeight, directionRow);
                    if (slicedFrames.Count > 0)
                    {
                        _animationFrames = slicedFrames.ToArray();
                        _currentFrameIndex = 0;
                        CurrentFrame = _animationFrames[0];
                    }
                }
            }
        }

        private void UpdateEnemyAnimation(int actionToLoad)
        {
            _enemyTempActionId = actionToLoad;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string spriteFolder = Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites");
            string enemyPath = Path.Combine(spriteFolder, $"p{EnemySpeciesId:D4}.bin");

            if (Directory.Exists(spriteFolder))
            {
                var matchedFiles = Directory.GetFiles(spriteFolder, $"p{EnemySpeciesId:D4}*.bin");
                if (matchedFiles.Length > 0) enemyPath = matchedFiles[0];
            }

            if (File.Exists(enemyPath))
            {
                var rawStrips = Utils.BinSpriteReader.LoadFramesFromBin(enemyPath);
                if (rawStrips != null && rawStrips.Count > 0)
                {
                    int stripIndex = actionToLoad;
                    if (stripIndex >= rawStrips.Count) stripIndex = rawStrips.Count - 1;
                    if (stripIndex < 0) stripIndex = 0;

                    var targetData = rawStrips[stripIndex];
                    if (targetData != null && targetData.Image != null)
                    {
                        int directionRow = 6;

                        List<BitmapSource> slicedFrames = SliceStripIntoFrames(targetData.Image, targetData.FrameWidth, targetData.FrameHeight, directionRow);
                        if (slicedFrames.Count > 0) EnemyCurrentFrame = slicedFrames[0];
                    }
                }
            }
        }

        private List<BitmapSource> SliceStripIntoFrames(BitmapSource strip, int fw, int fh, int targetRow = 0)
        {
            List<BitmapSource> frames = new List<BitmapSource>();

            if (fw <= 0 || fh <= 0) return frames;

            int columns = strip.PixelWidth / fw;
            int maxRow = (strip.PixelHeight / fh) - 1;
            int safeRow = Math.Max(0, Math.Min(targetRow, maxRow));

            for (int i = 0; i < columns; i++)
            {
                System.Windows.Int32Rect cropRect = new System.Windows.Int32Rect(i * fw, safeRow * fh, fw, fh);
                CroppedBitmap croppedFrame = new CroppedBitmap(strip, cropRect);
                croppedFrame.Freeze();
                frames.Add(croppedFrame);
            }
            return frames;
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

                        if (pet.Inventory == null || pet.Inventory.Count == 0)
                        {
                            pet.InitializeInventory();
                        }

                        return pet;
                    }
                }
                catch { }
            }

            PokemonState newPet = new PokemonState();
            newPet.InitializeInventory();
            newPet.InitializeAfterLoad();
            return newPet;
        }
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