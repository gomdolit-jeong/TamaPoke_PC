using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 생애 주기 (Life Cycle)

        private static readonly HashSet<int> AlwaysFemaleIds = new() { 29, 30, 31, 113, 115, 124, 238, 241, 242, 314, 380, 412, 416, 440, 478, 488, 548, 549, 629, 630, 669, 670, 671, 761, 762, 763, 856, 857, 858, 868, 869, 905, 957, 958, 959, 1017 };
        private static readonly HashSet<int> AlwaysMaleIds = new() { 32, 33, 34, 106, 107, 128, 236, 237, 313, 381, 414, 475, 538, 539, 627, 628, 641, 642, 645, 859, 860, 861, 1014, 1015, 1016 };
        private static readonly HashSet<int> Female12_5Ids = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 152, 153, 154, 155, 156, 157, 158, 159, 160, 252, 253, 254, 255, 256, 257, 258, 259, 260, 387, 388, 389, 390, 391, 392, 393, 394, 395, 495, 496, 497, 498, 499, 500, 501, 502, 503, 650, 651, 652, 653, 654, 655, 656, 657, 658, 722, 723, 724, 725, 726, 727, 728, 729, 730, 810, 811, 812, 813, 814, 815, 816, 817, 818, 906, 907, 908, 909, 910, 911, 912, 913, 914, 133, 134, 135, 136, 196, 197, 470, 471, 700, 138, 139, 140, 141, 345, 346, 347, 348, 408, 409, 410, 411, 564, 565, 566, 567, 696, 697, 698, 699, 142, 143, 446, 175, 176, 468, 447, 448, 415, 757, 891, 892 };
        private static readonly HashSet<int> Female75Ids = new() { 35, 36, 37, 38, 39, 40, 174, 222, 298, 572, 573, 574, 575, 576, 667, 668, 682, 683, 955, 956 };
        private static readonly HashSet<int> Female25Ids = new() { 58, 59, 63, 64, 65, 66, 67, 68, 125, 126, 239, 240, 296, 297, 466, 467, 532, 533, 534 };
        private static readonly HashSet<int> GenderlessIds = new() { 81, 82, 100, 101, 120, 121, 132, 137, 201, 233, 292, 337, 338, 343, 344, 374, 375, 376, 436, 437, 462, 474, 479, 599, 600, 601, 615, 622, 623, 703, 774, 781, 854, 855, 870, 924, 925, 999, 1000 };

        private static HashSet<int> MissingSpriteIds = new();

        private string _spriteFileName = "p0000.bin";
        public string SpriteFileName
        {
            get
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites", _spriteFileName);

                if (!File.Exists(fullPath))
                {
                    return "p0000.bin";
                }

                return _spriteFileName;
            }
            set
            {
                if (SetProperty(ref _spriteFileName, value))
                {
                    _animationFrames = null;
                    _currentActionId = -1;

                    OnPropertyChanged(nameof(FormDescription));
                    OnPropertyChanged(nameof(Type1));
                    OnPropertyChanged(nameof(Type2));
                    OnPropertyChanged(nameof(HasType2));
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        // ==========================================
        // 🌟 진화 테이블 시스템
        // ==========================================
        public static Dictionary<int, (int EvolveLevel, int[] NextSpeciesIds)> DexTable { get; private set; } = new();

        private int GetGenStartId(int gen) => gen switch
        {
            1 => 1,
            2 => 152,
            3 => 252,
            4 => 387,
            5 => 494,
            6 => 650,
            7 => 722,
            8 => 810,
            9 => 906,
            _ => 1
        };

        private int GetGenEndId(int gen) => gen switch
        {
            1 => 151,
            2 => 251,
            3 => 386,
            4 => 493,
            5 => 649,
            6 => 721,
            7 => 809,
            8 => 905,
            9 => GameConstants.MAX_POKEMON_ID,
            _ => GameConstants.MAX_POKEMON_ID
        };

        public static void InitializeEvolutionTable()
        {
            DexTable.Clear();

            foreach (var p in PokemonDex.AllPokemons)
            {
                if (p.EvolveTo > 0)
                {
                    int safeEvolveLevel = p.EvolveLevel > 0 ? p.EvolveLevel : 36;
                    DexTable[p.Id] = (safeEvolveLevel, new int[] { p.EvolveTo });
                }
            }

            DexTable[44] = (36, new int[] { 45, 182 });
            DexTable[61] = (36, new int[] { 62, 186 });
            DexTable[79] = (37, new int[] { 80, 199 });
            DexTable[133] = (30, new int[] { 134, 135, 136, 196, 197, 470, 471, 700 });
            DexTable[236] = (20, new int[] { 106, 107, 237 });
            DexTable[265] = (7, new int[] { 266, 268 });
            DexTable[281] = (30, new int[] { 282, 475 });
            DexTable[290] = (20, new int[] { 291, 292 });
            DexTable[361] = (42, new int[] { 362, 478 });
            DexTable[366] = (30, new int[] { 367, 368 });
            DexTable[412] = (20, new int[] { 413, 414 });
            DexTable[415] = (21, new int[] { 416 });

            DexTable[52] = (28, new int[] { 53, 863 });
            DexTable[123] = (40, new int[] { 212, 900 });
            DexTable[439] = (18, new int[] { 122, 866 });
            DexTable[562] = (34, new int[] { 563, 867 });
            DexTable[790] = (53, new int[] { 791, 792 });
            DexTable[840] = (30, new int[] { 841, 842, 1011 });
            DexTable[935] = (30, new int[] { 936, 937 });
            DexTable[194] = (20, new int[] { 195, 980 });
            DexTable[215] = (28, new int[] { 461, 903 });
        }

        public void RefreshPokedex()
        {
            FullPokedex.Clear();
            var sortedPokemons = PokemonDex.AllPokemons.OrderBy(p => p.Id);

            foreach (var poke in sortedPokemons)
            {
                bool unlocked = UnlockedPokemon.Contains(poke.Id);
                FullPokedex.Add(new PokedexEntry
                {
                    Id = poke.Id,
                    SpeciesName = poke.DisplayName,
                    IsUnlocked = unlocked
                });
            }

            UpdateFilteredPokedex();
        }

        public void UnlockPokemonInPokedex(int speciesId)
        {
            if (UnlockedPokemon == null) UnlockedPokemon = new List<int>();

            if (!UnlockedPokemon.Contains(speciesId))
            {
                UnlockedPokemon.Add(speciesId);
                RegisteredCount = UnlockedPokemon.Count;
                RefreshPokedex();

                OnPropertyChanged(nameof(KantoCountText));
                OnPropertyChanged(nameof(JohtoCountText));
                OnPropertyChanged(nameof(HoennCountText));
                OnPropertyChanged(nameof(SinnohCountText));

                Save();
            }
        }

        // ==========================================
        // 🌟 알 부화 (탄생) 로직
        // ==========================================

        private void ApplyGenderRatio(int speciesId, Random rand)
        {
            IsGenderless = false;

            bool isLegendaryWithGender = AlwaysFemaleIds.Contains(speciesId) || AlwaysMaleIds.Contains(speciesId) || Female12_5Ids.Contains(speciesId) || speciesId == 485;

            if (GenderlessIds.Contains(speciesId) || (LegendaryIds.Contains(speciesId) && !isLegendaryWithGender))
            {
                IsGenderless = true;
                IsFemale = false;
                return;
            }

            if (AlwaysFemaleIds.Contains(speciesId)) { IsFemale = true; return; }
            if (AlwaysMaleIds.Contains(speciesId)) { IsFemale = false; return; }
            if (Female12_5Ids.Contains(speciesId)) { IsFemale = rand.Next(1000) >= 875; return; }
            if (Female75Ids.Contains(speciesId)) { IsFemale = rand.Next(100) < 75; return; }
            if (Female25Ids.Contains(speciesId)) { IsFemale = rand.Next(100) < 25; return; }

            IsFemale = rand.Next(2) == 0;
        }

        public void Hatch()
        {
            _overrideName = null;
            _overrideLevel = null;

            Settings = TamaPoke.Utils.SettingsManager.Load();
            Random rand = new Random();

            SpeciesId = DetermineHatchSpecies(rand);
            ApplyGenderRatio(SpeciesId, rand);
            ApplyShinyAndSprite(rand);
            ResetNewbornStats(rand);
            RegisterToPartyAndDex();

            RefreshPokedex();
            Save();

            OnPropertyChanged(nameof(Name));
            CheckStateAndAnimate();
        }

        #region Hatch() 헬퍼 메서드 모음

        private void ReloadMissingSprites()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "missing_sprites.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var ids = JsonSerializer.Deserialize<List<int>>(json);
                    if (ids != null)
                    {
                        MissingSpriteIds = new HashSet<int>(ids);
                    }
                }
            }
            catch
            {
            }
        }

        private int DetermineHatchSpecies(Random rand)
        {
            if (DexTable.Count == 0) InitializeEvolutionTable();

            ReloadMissingSprites();

            var allEvolvedIds = new HashSet<int>();

            void CollectEvolutions(int currentId)
            {
                if (DexTable.TryGetValue(currentId, out var data))
                {
                    foreach (int nextId in data.NextSpeciesIds)
                    {
                        if (allEvolvedIds.Add(nextId)) CollectEvolutions(nextId);
                    }
                }
            }

            foreach (var rootId in DexTable.Keys) CollectEvolutions(rootId);

            var baseSpeciesIds = PokemonDex.AllPokemons
                .Where(p => Settings.SelectedGenerations.Any(gen => p.Id >= GetGenStartId(gen) && p.Id <= GetGenEndId(gen))
                            && !allEvolvedIds.Contains(p.Id)
                            && !LegendaryIds.Contains(p.Id)
                            && !MissingSpriteIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToList();

            if (RegisteredCount >= 30 && rand.Next(100) < 3)
            {
                var validLegendaries = LegendaryIds.Where(id => !MissingSpriteIds.Contains(id)).ToArray();
                if (validLegendaries.Length > 0)
                {
                    return validLegendaries[rand.Next(validLegendaries.Length)];
                }
            }

            return baseSpeciesIds.Count > 0 ? baseSpeciesIds[rand.Next(baseSpeciesIds.Count)] : 1;
        }

        private void ApplyShinyAndSprite(Random rand)
        {
            int shinyBase = (LastEnd == 1 ? 24 : 48) - CareBonus();
            if (shinyBase < 8) shinyBase = 8;
            IsShiny = (rand.Next(shinyBase) == 0);

            string assetFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites");

            if (!Directory.Exists(assetFolderPath))
            {
                Directory.CreateDirectory(assetFolderPath);
            }

            string searchPattern = $"p{SpeciesId:D4}*.bin";
            string[] allForms = Directory.GetFiles(assetFolderPath, searchPattern);

            string baseFileName = $"p{SpeciesId:D4}.bin";
            SpriteFileName = baseFileName;

            if (allForms.Length > 0)
            {
                if (allForms.Length > 1 && rand.Next(100) < 10)
                {
                    var specialForms = allForms.Where(f => Path.GetFileName(f) != baseFileName && !Path.GetFileName(f).ToLower().Contains("shiny") && !Path.GetFileName(f).ToLower().Contains("female")).ToArray();
                    if (specialForms.Length > 0)
                    {
                        string selectedFormPath = specialForms[rand.Next(specialForms.Length)];
                        SpriteFileName = Path.GetFileName(selectedFormPath);
                    }
                }

                SpriteFileName = ResolveComplexSpriteName(allForms, SpriteFileName);

                if (SpriteFileName.ToLower().Contains("shiny") && Settings.UseTrayNotifications)
                {
                    TrayNotificationRequested?.Invoke("✨ 반짝반짝!", "색이 다른(이로치) 포켓몬이 태어났어요!");
                }
                else if (SpriteFileName != baseFileName && !SpriteFileName.ToLower().Contains("female") && Settings.UseTrayNotifications)
                {
                    TrayNotificationRequested?.Invoke("✨ 특별한 탄생!", "평소와 다른 특별한 모습의 포켓몬이 태어났어요!");
                }
            }
            else
            {
                SpriteFileName = "p0000.bin";
            }
        }

        private void ResetNewbornStats(Random rand)
        {
            Genes = new PokemonGene
            {
                HpGene = rand.Next(0, 32),
                AtkGene = rand.Next(0, 32),
                DefGene = rand.Next(0, 32),
                SpeGene = rand.Next(0, 32)
            };

            for (int i = 0; i < 4; i++) Skills[i] = 0;
            RelearnFromLevel();

            BerryKnown = false;
            NeglectTicks = 0;
            _ageSeconds = 0;
            AgeMinutes = 0;

            ResetPosition();

            IsEvolutionPostponed = false;
            IsFarewellPostponed = false;
            Ceremony = 0;

            Fullness = 80;
            Joy = 80;
            Energy = 100;
            Hygiene = 100;
        }

        private void RegisterToPartyAndDex()
        {
            if (SpeciesId > 0 && !UnlockedPokemon.Contains(SpeciesId))
            {
                UnlockedPokemon.Add(SpeciesId);
                UnlockedPokemon.Sort();
                RegisteredCount = UnlockedPokemon.Count;
            }

            var newbornMember = new PartyMember
            {
                SpeciesId = this.SpeciesId,
                Name = this.Name,
                Level = this.Level,
                IsShiny = this.IsShiny,
                TrAtk = this.TrAtk,
                TrDef = this.TrDef,
                TrSpeed = this.TrSpeed,
                Skills = (int[])this.Skills.Clone(),
                Genes = new PokemonGene
                {
                    HpGene = this.Genes.HpGene,
                    AtkGene = this.Genes.AtkGene,
                    DefGene = this.Genes.DefGene,
                    SpeGene = this.Genes.SpeGene
                }
            };

            if (Party.Count == 0)
            {
                Party.Add(newbornMember);
            }
            else
            {
                if (Party.Count < 6) Party.Add(newbornMember);
                else Party[0] = newbornMember;
            }
        }

        private string ResolveComplexSpriteName(string[] allForms, string baseFileName)
        {
            if (allForms.Length == 0) return "p0000.bin";

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
            bool tryShiny = IsShiny && !fileNameWithoutExt.ToLower().Contains("shiny");
            bool tryFemale = IsFemale && !fileNameWithoutExt.ToLower().Contains("female");

            string? finalMatch = null;

            if (tryShiny && tryFemale)
            {
                finalMatch = allForms.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny_Female", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female_Shiny", StringComparison.OrdinalIgnoreCase))
                    ?? allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
            }
            else if (tryFemale)
            {
                finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female", StringComparison.OrdinalIgnoreCase));
            }
            else if (tryShiny)
            {
                finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
            }

            return finalMatch != null ? Path.GetFileName(finalMatch) : baseFileName;
        }

        #endregion

        // ==========================================
        // 🌟 진화 로직
        // ==========================================
        public async void StartEvolutionCeremony()
        {
            if (IsCeremony || IsEgg || IsSleeping || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;
            if (IsFinalEvolution || !CanEvolveNow) return;

            Ceremony = 4;
            IsEvolutionPostponed = false;

            string oldName = Name;
            ResetPosition();

            EvolutionMessage = "오잉!?";
            _tempActionId = 17;
            _tempActionTimer = 90;
            CheckStateAndAnimate();
            await Task.Delay(1500);

            EvolutionMessage = $"{oldName}의 상태가...?!";
            IsEvolvingFlash = true;
            PosY = -20;
            CheckStateAndAnimate();
            await Task.Delay(3000);

            IsEvolvingFlash = false;
            PosY = 0;

            ExecuteActualEvolve();

            _tempActionId = 10;
            _tempActionTimer = 60;
            CheckStateAndAnimate();

            EvolutionMessage = $"축하합니다! {oldName}은(는)\n{Name}(으)로 진화했습니다!";
            await Task.Delay(3500);

            Ceremony = 0;
            EvolutionMessage = "";
        }

        private void ExecuteActualEvolve()
        {
            if (!DexTable.ContainsKey(SpeciesId)) return;

            int[] possibleNextForms = DexTable[SpeciesId].NextSpeciesIds;
            int nextSpeciesId = -1;

            if (possibleNextForms != null && possibleNextForms.Length > 0)
            {
                int randomIndex = new Random().Next(possibleNextForms.Length);
                nextSpeciesId = possibleNextForms[randomIndex];
            }

            if (nextSpeciesId == -1) return;

            SpeciesId = nextSpeciesId;

            RefreshSpriteFileName();
            ReloadSprite();

            _overrideName = null;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(FormDescription));

            IsEvolutionPostponed = false;
            ResetPosition();

            if (SpeciesId > 0 && !UnlockedPokemon.Contains(SpeciesId))
            {
                UnlockedPokemon.Add(SpeciesId);
                UnlockedPokemon.Sort();
                RegisteredCount = UnlockedPokemon.Count;
            }
            RefreshPokedex();

            PlayerHp = CombatMaxHp;
            Save();
        }

        public void RefreshSpriteFileName()
        {
            string assetFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites");

            if (!Directory.Exists(assetFolderPath))
            {
                Directory.CreateDirectory(assetFolderPath);
            }

            string[] allForms = Directory.GetFiles(assetFolderPath, $"p{SpeciesId:D4}*.bin");

            SpriteFileName = ResolveComplexSpriteName(allForms, $"p{SpeciesId:D4}.bin");
        }

        // ==========================================
        // 🌟 새 게임 (알) 강제 준비
        // ==========================================
        public void PrepareNewEgg()
        {
            SpeciesId = -1; EggTaps = 0; _ageSeconds = 0; AgeMinutes = 0;
            _overrideName = null;
            _overrideLevel = null;

            Fullness = 80; Joy = 80; Energy = 80; Hygiene = 100;
            Poops = 0; Weight = 10; CareMistakes = 0; Bond = 0; NeglectTicks = 0;
            IsSleeping = false; IsShiny = false; Genes = new PokemonGene(); BerryKnown = false;
            Ceremony = 0; IsEvolutionPostponed = false; IsFarewellPostponed = false;

            IsBallGameOpen = false; IsCatchGameOpen = false; IsMemoGameOpen = false; IsCleanGameOpen = false;
            IsBattleOpen = false; IsAttackMenuOpen = false; IsBattleResolved = false; IsCatchOffered = false;

            IsPartyOpen = false;
            IsSwapMode = false;
            _pendingRetiree = null;

            TrAtk = 0; TrDef = 0; TrSpeed = 0; Medals = 0;
            for (int i = 0; i < 4; i++) Skills[i] = 0;

            OnPropertyChanged("CurrentSkills");
            OnPropertyChanged(nameof(PoopDisplay)); OnPropertyChanged(nameof(IsEating)); OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(Name));

            ResetPosition();
        }

        // ==========================================
        // 🌟 이별, 가출, 방생 로직
        // ==========================================

        public void StartFarewell()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition();

            if (Party != null && Party.Count > 0)
            {
                var target = Party.FirstOrDefault(p => p.SpeciesId == this.SpeciesId);
                if (target != null) Party.Remove(target);
            }

            LastEnd = 1;
            Ceremony = 1;
            ExecuteCeremonyTimer();
        }

        public void StartRunaway()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition();

            if (Party != null && Party.Count > 0)
            {
                var target = Party.FirstOrDefault(p => p.SpeciesId == this.SpeciesId);
                if (target != null) Party.Remove(target);
            }

            LastEnd = 2;
            Ceremony = 2;
            ExecuteCeremonyTimer();
        }

        public void StartRelease()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition();

            if (Party != null && Party.Count > 0)
            {
                var target = Party.FirstOrDefault(p => p.SpeciesId == this.SpeciesId);
                if (target != null) Party.Remove(target);
            }

            LastEnd = 3;
            Ceremony = 3;
            ExecuteCeremonyTimer();
        }

        private void ExecuteCeremonyTimer()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Ceremony = 0;
                PrepareNewEgg();
            };
            timer.Start();
        }

        // ==========================================
        // 🌟 모든 데이터 공장 초기화
        // ==========================================
        public void FactoryReset()
        {
            if (System.IO.File.Exists(SaveFilePath)) System.IO.File.Delete(SaveFilePath);

            UnlockedPokemon.Clear(); FullPokedex.Clear(); RegisteredCount = 0; Streak = 0; LastEnd = 1; LastPlayedDate = DateTime.Now.Date;
            GameHighScore = 0; CatchHighScore = 0; MemoHighScore = 0; CleanHighScore = 0;
            GymBadges = 0;
            IsGymBattle = false;

            TrAtk = 0;
            TrDef = 0;
            TrSpeed = 0;

            if (Party != null) Party.Clear();

            InitializeInventory();

            PrepareNewEgg();
            RefreshPokedex();
            Save();
        }

        #endregion
    }
}