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
            DexTable[194] = (20, new int[] { 195, 980 }); // 우파 -> 누오, 토오
            DexTable[215] = (28, new int[] { 461, 903 }); // 포푸니 -> 포푸니라, 포푸니크
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

        // 🌟 1~9세대 원작 고증 성비 데이터베이스 및 적용 헬퍼 메서드
        private void ApplyGenderRatio(int speciesId, Random rand)
        {
            // 1. 100% 암컷 (니드런♀, 삐삐, 캥카, 럭키, 밀탱크, 라티아스, 비퀸, 해피너스 등)
            HashSet<int> alwaysFemale = new HashSet<int> { 29, 30, 31, 113, 115, 124, 238, 241, 242, 314, 380, 412, 416, 440, 478, 488, 548, 549, 629, 630, 669, 670, 671, 761, 762, 763, 856, 857, 858, 868, 869, 905, 957, 958, 959, 1017 };

            // 2. 100% 수컷 (니드런♂, 켄타로스, 배루키 계열, 라티오스, 엘레이드, 토네로스 등)
            HashSet<int> alwaysMale = new HashSet<int> { 32, 33, 34, 106, 107, 128, 236, 237, 313, 381, 414, 475, 538, 539, 627, 628, 641, 642, 645, 859, 860, 861, 1014, 1015, 1016 };

            // 3. 수컷 87.5% / 암컷 12.5% (스타팅 전원, 이브이 계열, 잠만보, 화석 포켓몬, 세꿀버리, 루카리오 등)
            HashSet<int> female12_5 = new HashSet<int> {
                1,2,3,4,5,6,7,8,9, 152,153,154,155,156,157,158,159,160,
                252,253,254,255,256,257,258,259,260, 387,388,389,390,391,392,393,394,395,
                495,496,497,498,499,500,501,502,503, 650,651,652,653,654,655,656,657,658,
                722,723,724,725,726,727,728,729,730, 810,811,812,813,814,815,816,817,818,
                906,907,908,909,910,911,912,913,914,
                133,134,135,136,196,197,470,471,700,
                138,139,140,141,345,346,347,348,408,409,410,411,564,565,566,567,696,697,698,699,
                142,143,446, 175,176,468, 447,448, 415, 757, 891, 892
            };

            // 4. 수컷 25% / 암컷 75% (삐삐, 식스테일, 푸린, 코산호, 치라미 등)
            HashSet<int> female75 = new HashSet<int> { 35, 36, 37, 38, 39, 40, 174, 222, 298, 572, 573, 574, 575, 576, 667, 668, 682, 683, 955, 956 };

            // 5. 수컷 75% / 암컷 25% (캐이시, 알통몬, 가디, 에레키드, 마그비, 노보청 등)
            HashSet<int> female25 = new HashSet<int> { 58, 59, 63, 64, 65, 66, 67, 68, 125, 126, 239, 240, 296, 297, 466, 467, 532, 533, 534 };

            // 6. 무성(성별 없음) 일반 포켓몬 (코일, 찌리리공, 메타몽, 폴리곤 등)
            HashSet<int> genderless = new HashSet<int> { 81, 82, 100, 101, 120, 121, 132, 137, 201, 233, 292, 337, 338, 343, 344, 374, 375, 376, 436, 437, 462, 474, 479, 599, 600, 601, 615, 622, 623, 703, 774, 781, 854, 855, 870, 924, 925, 999, 1000 };

            // 기본 초기화
            IsGenderless = false;

            // 🌟 전설/환상 방어 코드: 히드런(485)은 50:50, 그 외 성별이 있는 전설을 제외한 모든 전설은 무성 처리
            bool isLegendaryWithGender = alwaysFemale.Contains(speciesId) || alwaysMale.Contains(speciesId) || female12_5.Contains(speciesId) || speciesId == 485;
            if (genderless.Contains(speciesId) || (LegendaryIds.Contains(speciesId) && !isLegendaryWithGender))
            {
                IsGenderless = true;
                IsFemale = false;
                return;
            }

            // 확률 적용
            if (alwaysFemale.Contains(speciesId)) { IsFemale = true; return; }
            if (alwaysMale.Contains(speciesId)) { IsFemale = false; return; }
            if (female12_5.Contains(speciesId)) { IsFemale = rand.Next(1000) >= 875; return; }
            if (female75.Contains(speciesId)) { IsFemale = rand.Next(100) < 75; return; }
            if (female25.Contains(speciesId)) { IsFemale = rand.Next(100) < 25; return; }

            // 그 외 모든 포켓몬은 기본 50:50
            IsFemale = rand.Next(2) == 0;
        }

        public void Hatch()
        {
            _overrideName = null;
            _overrideLevel = null;

            Settings = TamaPoke.Utils.SettingsManager.Load();
            Random rand = new Random();

            // 1. 종족(도감 번호) 결정
            SpeciesId = DetermineHatchSpecies(rand);

            // 2. 성별 부여 (이미 만들어두신 멋진 메서드 호출!)
            ApplyGenderRatio(SpeciesId, rand);

            // 3. 이로치(색이 다른) 여부 결정 및 스프라이트 파일 연결
            ApplyShinyAndSprite(rand);

            // 4. 유전자, 스킬, 육성 스탯 초기화
            ResetNewbornStats(rand);

            // 5. 도감 및 파티 등록
            RegisterToPartyAndDex();

            // 6. 저장 및 UI 갱신
            RefreshPokedex();
            Save();

            OnPropertyChanged(nameof(Name));
            CheckStateAndAnimate();
        }

        #region Hatch() 헬퍼 메서드 모음

        private int DetermineHatchSpecies(Random rand)
        {
            if (DexTable.Count == 0) InitializeEvolutionTable();

            // 🌟 1. 이미지가 없는 포켓몬 번호들을 HashSet으로 분리해 둡니다.
            HashSet<int> missingSpriteIds = new HashSet<int>
            {
                514, 516, 520, 522, 523, 538, 558, 564, 565, 591, 592, 616, 626,
                732, 735, 756, 765, 837, 838, 839, 847, 866, 878, 893, 896, 931,
                942, 943, 944, 947, 949, 950, 954, 956, 962, 973, 986, 993, 1001,
                1002, 1014, 1022, 1023
            };

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

            // 🌟 2. 조건에 missingSpriteIds에 포함되지 않은 포켓몬만 가져오도록 추가합니다.
            var baseSpeciesIds = PokemonDex.AllPokemons
                .Where(p => Settings.SelectedGenerations.Any(gen => p.Id >= GetGenStartId(gen) && p.Id <= GetGenEndId(gen))
                            && !allEvolvedIds.Contains(p.Id)
                            && !LegendaryIds.Contains(p.Id)
                            && !missingSpriteIds.Contains(p.Id)) // 누락된 이미지 필터링!
                .Select(p => p.Id)
                .ToList();

            // 🌟 3. 전설의 포켓몬을 뽑을 때도 누락된 번호는 제외합니다.
            if (RegisteredCount >= 30 && rand.Next(100) < 3)
            {
                var validLegendaries = LegendaryIds.Where(id => !missingSpriteIds.Contains(id)).ToArray();
                if (validLegendaries.Length > 0)
                {
                    return validLegendaries[rand.Next(validLegendaries.Length)];
                }
            }

            // 안전장치: 뽑을 수 있는 포켓몬이 하나도 없다면 1번(이상해씨)을 반환
            return baseSpeciesIds.Count > 0 ? baseSpeciesIds[rand.Next(baseSpeciesIds.Count)] : 1;
        }

        private void ApplyShinyAndSprite(Random rand)
        {
            // 이로치 확률 계산
            int shinyBase = (LastEnd == 1 ? 24 : 48) - CareBonus();
            if (shinyBase < 8) shinyBase = 8;
            IsShiny = (rand.Next(shinyBase) == 0);

            string assetFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites");
            string searchPattern = $"p{SpeciesId:D4}*.bin";
            string[] allForms = Directory.GetFiles(assetFolderPath, searchPattern);

            string baseFileName = $"p{SpeciesId:D4}.bin";
            SpriteFileName = baseFileName;

            if (allForms.Length > 0)
            {
                // 특수 폼(알로라, 가라르 등) 처리
                if (allForms.Length > 1 && rand.Next(100) < 10)
                {
                    var specialForms = allForms.Where(f => Path.GetFileName(f) != baseFileName && !Path.GetFileName(f).ToLower().Contains("shiny") && !Path.GetFileName(f).ToLower().Contains("female")).ToArray();
                    if (specialForms.Length > 0)
                    {
                        string selectedFormPath = specialForms[rand.Next(specialForms.Length)];
                        SpriteFileName = Path.GetFileName(selectedFormPath);
                    }
                }

                // 암수 및 이로치 파일 스마트 매칭 로직
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

                // 트레이 알림
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