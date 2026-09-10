using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 생애 주기 (Life Cycle)

        private string _spriteFileName = "p0000.bin";
        public string SpriteFileName
        {
            get => _spriteFileName;
            set
            {
                if (SetProperty(ref _spriteFileName, value))
                {
                    // 파일명이 바뀌면 즉시 프레임을 초기화하여 새로운 이미지를 로드하도록 유도합니다.
                    _animationFrames = null;
                    _currentActionId = -1;
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
            9 => 1025,
            _ => 1025
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
        public void Hatch()
        {
            _overrideName = null;
            _overrideLevel = null;

            Settings = TamaPoke.Utils.SettingsManager.Load();
            Random rand = new Random();

            if (DexTable.Count == 0) InitializeEvolutionTable();

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
                            && !LegendaryIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToList();

            if (RegisteredCount >= 30 && rand.Next(100) < 3) SpeciesId = LegendaryIds[rand.Next(LegendaryIds.Length)];
            else SpeciesId = baseSpeciesIds.Count > 0 ? baseSpeciesIds[rand.Next(baseSpeciesIds.Count)] : 1;
            
            // 🌟 1. 암/수 랜덤 결정 (이제 파일 호출에도 쓰입니다!)
            IsFemale = (rand.Next(2) == 0);

            // 🌟 2. [추가] 종족 고유 성별 고정 (니드런 계열 방어 코드)
            if (SpeciesId == 29 || SpeciesId == 30 || SpeciesId == 31)
            {
                IsFemale = true; // 니드런♀(29), 니드리나(30), 니드퀸(31)은 무조건 암컷
            }
            else if (SpeciesId == 32 || SpeciesId == 33 || SpeciesId == 34)
            {
                IsFemale = false; // 니드런♂(32), 니드리노(33), 니드킹(34)은 무조건 수컷
            }

            string assetFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites");
            string searchPattern = $"p{SpeciesId:D4}*.bin";
            string[] allForms = Directory.GetFiles(assetFolderPath, searchPattern);

            string baseFileName = $"p{SpeciesId:D4}.bin";
            SpriteFileName = baseFileName;

            int shinyBase = (LastEnd == 1 ? 24 : 48) - CareBonus();
            if (shinyBase < 8) shinyBase = 8;
            IsShiny = (rand.Next(shinyBase) == 0);

            if (allForms.Length > 0)
            {
                // 특수 폼(알로라, 가라르 등)을 처리하는 로직
                if (allForms.Length > 1 && rand.Next(100) < 10)
                {
                    var specialForms = allForms.Where(f => Path.GetFileName(f) != baseFileName && !Path.GetFileName(f).ToLower().Contains("shiny") && !Path.GetFileName(f).ToLower().Contains("female")).ToArray();
                    if (specialForms.Length > 0)
                    {
                        string selectedFormPath = specialForms[rand.Next(specialForms.Length)];
                        SpriteFileName = Path.GetFileName(selectedFormPath);
                    }
                }

                // 🌟 2. 암수 및 이로치 파일 스마트 매칭 로직
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(SpriteFileName);

                bool tryShiny = IsShiny && !fileNameWithoutExt.ToLower().Contains("shiny");
                bool tryFemale = IsFemale && !fileNameWithoutExt.ToLower().Contains("female");

                string? finalMatch = null;

                if (tryShiny && tryFemale)
                {
                    // 1순위: 이로치 + 암컷 동시 만족 파일 찾기
                    finalMatch = allForms.FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny_Female", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female_Shiny", StringComparison.OrdinalIgnoreCase));

                    // 2순위: 암컷 전용 이로치가 없다면 기본 이로치 파일로 타협
                    if (finalMatch == null)
                        finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
                }
                else if (tryFemale)
                {
                    // 일반 암컷 전용 탐색
                    finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female", StringComparison.OrdinalIgnoreCase));
                }
                else if (tryShiny)
                {
                    // 일반 이로치 전용 탐색
                    finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
                }

                // 🌟 3. 일치하는 파일이 있을 때만 파일명을 덮어씌웁니다! (없으면 원래 파일 유지)
                if (finalMatch != null)
                {
                    SpriteFileName = Path.GetFileName(finalMatch);
                }

                // 알림 띄우기
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

            Genes = new PokemonGene
            {
                HpGene = rand.Next(0, 32),
                AtkGene = rand.Next(0, 32),
                DefGene = rand.Next(0, 32),
                SpeGene = rand.Next(0, 32)
            };

            for (int i = 0; i < 4; i++) Skills[i] = 0;
            RelearnFromLevel();

            BerryKnown = false; NeglectTicks = 0; _ageSeconds = 0; AgeMinutes = 0; ResetPosition(); IsEvolutionPostponed = false; IsFarewellPostponed = false; Ceremony = 0;
            Fullness = 80; Joy = 80; Energy = 100; Hygiene = 100;
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

            if (Party.Count == 0) Party.Add(newbornMember);
            else
            {
                if (Party.Count < 6) Party.Add(newbornMember);
                else Party[0] = newbornMember;
            }

            RefreshPokedex(); Save();
            OnPropertyChanged(nameof(Name));
            CheckStateAndAnimate();
        }

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

            // 1. 도감 번호가 진화형으로 바뀝니다!
            SpeciesId = nextSpeciesId;

            // 🌟 2. [추가] 바뀐 도감 번호에 맞춰서 파일명을 다시 계산합니다!
            RefreshSpriteFileName();

            // 🌟 3. [추가] 파일명이 바뀌었으니, 실제 메모리에 이미지를 다시 불러옵니다!
            // (주의: LoadAnimation() 등 실제 이미지를 불러오는 유저님의 메서드 이름으로 변경해주세요)
            ReloadSprite();

            _overrideName = null;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(FormDescription)); // 🌟 폼 텍스트도 갱신되도록 추가

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
            string searchPattern = $"p{SpeciesId:D4}*.bin";
            string[] allForms = Directory.GetFiles(assetFolderPath, searchPattern);

            string baseFileName = $"p{SpeciesId:D4}.bin";
            SpriteFileName = baseFileName;

            if (allForms.Length > 0)
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(SpriteFileName);

                bool tryShiny = IsShiny && !fileNameWithoutExt.ToLower().Contains("shiny");
                bool tryFemale = IsFemale && !fileNameWithoutExt.ToLower().Contains("female");

                string? finalMatch = null;

                if (tryShiny && tryFemale)
                {
                    finalMatch = allForms.FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny_Female", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female_Shiny", StringComparison.OrdinalIgnoreCase));

                    if (finalMatch == null)
                        finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
                }
                else if (tryFemale)
                {
                    finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Female", StringComparison.OrdinalIgnoreCase));
                }
                else if (tryShiny)
                {
                    finalMatch = allForms.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals($"{fileNameWithoutExt}_Shiny", StringComparison.OrdinalIgnoreCase));
                }

                if (finalMatch != null)
                {
                    SpriteFileName = Path.GetFileName(finalMatch);
                }
            }
            else
            {
                SpriteFileName = "p0000.bin";
            }
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

            OnPropertyChanged(nameof(CurrentSkills));
            OnPropertyChanged(nameof(PoopDisplay)); OnPropertyChanged(nameof(IsEating)); OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(Name));

            ResetPosition();
        }

        // ==========================================
        // 🌟 [핵심 수정] 이별, 가출, 방생 로직 최적화
        // ==========================================

        // 1. 이별 (Farewell - 오랫동안 함께했을 때)
        public void StartFarewell()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition();

            // 파티에 등록되어 있다면 내 정보만 쏙 빼냅니다.
            if (Party != null && Party.Count > 0)
            {
                var target = Party.FirstOrDefault(p => p.SpeciesId == this.SpeciesId);
                if (target != null) Party.Remove(target);
            }

            LastEnd = 1;
            Ceremony = 1;
            ExecuteCeremonyTimer();
        }

        // 2. 가출 (Runaway - 방치되었을 때)
        public void StartRunaway()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition();

            // 파티에 등록되어 있다면 내 정보만 쏙 빼냅니다.
            if (Party != null && Party.Count > 0)
            {
                var target = Party.FirstOrDefault(p => p.SpeciesId == this.SpeciesId);
                if (target != null) Party.Remove(target);
            }

            LastEnd = 2;
            Ceremony = 2;
            ExecuteCeremonyTimer();
        }

        // 3. 방생 (Release - 유저가 직접 보내줄 때)
        public void StartRelease()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition();

            // 파티에 등록되어 있다면 내 정보만 쏙 빼냅니다.
            if (Party != null && Party.Count > 0)
            {
                var target = Party.FirstOrDefault(p => p.SpeciesId == this.SpeciesId);
                if (target != null) Party.Remove(target);
            }

            LastEnd = 3;
            Ceremony = 3;
            ExecuteCeremonyTimer();
        }

        // 🌟 이별/가출 연출을 위한 10초 대기 타이머
        private void ExecuteCeremonyTimer()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                Ceremony = 0;
                PrepareNewEgg(); // 애니메이션 종료 후 새로운 알 등장!
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