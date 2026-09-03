using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TamaPoke.Models
{
    // 🌟 본체와 하나로 합쳐질 수 있도록 partial 키워드를 사용합니다.
    public partial class PokemonState
    {
        #region 생애 주기 (Life Cycle)

        // ==========================================
        // 🌟 [추가됨] 동적 진화 테이블 시스템
        // ==========================================
        public static Dictionary<int, (int EvolveLevel, int[] NextSpeciesIds)> DexTable { get; private set; } = new();

        public static void InitializeEvolutionTable()
        {
            DexTable.Clear();

            // 1. JSON 파일의 649마리 데이터를 모두 읽어옵니다.
            foreach (var p in PokemonDex.AllPokemons)
            {
                if (p.EvolveTo > 0 && p.EvolveLevel > 0)
                {
                    DexTable[p.Id] = (p.EvolveLevel, new int[] { p.EvolveTo });
                }
            }

            // 2. 다중 진화(여러 갈래로 진화하는 포켓몬) 예외 처리 수동 등록
            DexTable[44] = (36, new int[] { 45, 182 }); // 냄새꼬 -> 라플레시아 / 아르코
            DexTable[61] = (36, new int[] { 62, 186 }); // 슈륙챙이 -> 강챙이 / 왕구리
            DexTable[79] = (37, new int[] { 80, 199 }); // 야돈 -> 야도란 / 야도킹
            DexTable[133] = (30, new int[] { 134, 135, 136, 196, 197, 470, 471 }); // 이브이 -> 7종
            DexTable[236] = (20, new int[] { 106, 107, 237 }); // 배루키 -> 시라소몬 / 홍수몬 / 카포에라
            DexTable[265] = (7, new int[] { 266, 268 }); // 개무소 -> 실쿤 / 카스쿤
            DexTable[281] = (30, new int[] { 282, 475 }); // 킬리아 -> 가디안 / 엘레이드
            DexTable[290] = (20, new int[] { 291, 292 }); // 토중몬 -> 아이스크 / 껍질몬
            DexTable[361] = (42, new int[] { 362, 478 }); // 눈꼬마 -> 얼음귀신 / 눈여아
            DexTable[366] = (30, new int[] { 367, 368 }); // 진주몽 -> 헌테일 / 분홍장이
            DexTable[412] = (20, new int[] { 413, 414 }); // 도롱충이 -> 도롱마담 / 나메일
            DexTable[415] = (21, new int[] { 416 }); // 세꿀버리 -> 비퀸 (성별 예외)

        }

        // ==========================================
        // 🌟 기존 생애 주기 로직
        // ==========================================

        // 🌟 도감 데이터를 갱신하는 헬퍼 함수
        public void RefreshPokedex()
        {
            if (FullPokedex.Count == 0)
            {
                foreach (var p in PokemonDex.AllPokemons)
                {
                    if (p.Id <= 493) // 4세대까지만 도감에 표시
                    {
                        FullPokedex.Add(new PokedexEntry { Id = p.Id, SpeciesName = p.DisplayName, IsUnlocked = false });
                    }
                }
            }
            foreach (var entry in FullPokedex)
            {
                entry.IsUnlocked = UnlockedPokemon.Contains(entry.Id);
            }
            UpdateFilteredPokedex();
        }

        // 🌟 포획한 포켓몬을 도감에 해금하는 메서드
        public void UnlockPokemonInPokedex(int speciesId)
        {
            if (UnlockedPokemon == null)
            {
                UnlockedPokemon = new List<int>();
            }

            // 아직 도감에 등록되지 않은 포켓몬이라면 추가합니다.
            if (!UnlockedPokemon.Contains(speciesId))
            {
                UnlockedPokemon.Add(speciesId);
                RegisteredCount = UnlockedPokemon.Count;

                // 도감 데이터를 새로고침하여 화면에 즉시 반영되도록 합니다.
                RefreshPokedex();

                // 변경된 도감 상태를 세이브 파일에 저장합니다.
                Save();
            }
        }

        // 🌟 알에서 깨어날 때 포켓몬을 랜덤으로 정해주는 함수
        // 🌟 알에서 깨어날 때 포켓몬을 정하고 파티에 등록하는 함수
        public void Hatch()
        {
            _overrideName = null;
            _overrideLevel = null;

            Random rand = new Random();
            int shinyBase = (LastEnd == 1 ? 24 : 48) - CareBonus();
            if (shinyBase < 8) shinyBase = 8;
            IsShiny = (rand.Next(shinyBase) == 0);

            // 🌟 [핵심 수정] DexTable에 등록된 모든 진화 후 형태(다중 진화 및 일반 진화 포함)를 완벽하게 모읍니다.
            var childIds = new HashSet<int>();

            // 만약 앱 실행 시 InitializeEvolutionTable()이 먼저 호출되지 않았다면 여기서 안전하게 한 번 더 실행해 줍니다.
            if (DexTable.Count == 0)
            {
                InitializeEvolutionTable();
            }

            foreach (var v in DexTable.Values)
            {
                foreach (var id in v.NextSpeciesIds)
                {
                    childIds.Add(id);
                }
            }

            // 🌟 진화형(childIds)에 포함되지 않고, 전설이 아닌 1세대~4세대 기본 포켓몬만 추출합니다.
            var baseSpeciesIds = PokemonDex.AllPokemons
                .Where(p => p.Id <= 493
                            && !childIds.Contains(p.Id)      // 조건 A: 진화 형태(부스터, 둥실라이드 등) 완벽 차단
                            && !LegendaryIds.Contains(p.Id)) // 조건 B: 전설의 포켓몬 차단
                .Select(p => p.Id).ToList();

            if (RegisteredCount >= 30 && rand.Next(100) < 3)
            {
                SpeciesId = LegendaryIds[rand.Next(LegendaryIds.Length)];
            }
            else
            {
                SpeciesId = baseSpeciesIds.Count > 0 ? baseSpeciesIds[rand.Next(baseSpeciesIds.Count)] : 1;
            }

            Genes = new PokemonGene { HpGene = 90 + rand.Next(21), AtkGene = 90 + rand.Next(21), DefGene = 90 + rand.Next(21), SpeGene = 90 + rand.Next(21) };

            for (int i = 0; i < 4; i++) Skills[i] = 0;
            RelearnFromLevel();

            BerryKnown = false; NeglectTicks = 0; _ageSeconds = 0; AgeMinutes = 0; ResetPosition(); IsEvolutionPostponed = false; IsFarewellPostponed = false; Ceremony = 0;

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

            RefreshPokedex(); Save();
            OnPropertyChanged(nameof(Name));
        }

        // 🌟 진화를 극적인 3단계 비동기 연출로 업그레이드한 함수
        public async void StartEvolutionCeremony()
        {
            if (IsCeremony || IsEgg || IsSleeping || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;
            if (IsFinalEvolution || !CanEvolveNow) return;

            Ceremony = 4; // 진화 상태 돌입
            IsEvolutionPostponed = false;

            string oldName = Name;
            ResetPosition();

            // ==========================================
            // 🎬 [1단계] 진화 징조
            // ==========================================
            EvolutionMessage = "오잉!?";
            _tempActionId = 7; // ANIM_POSE
            _tempActionTimer = 90;
            CheckStateAndAnimate();
            await Task.Delay(1500);

            // ==========================================
            // 🎬 [2단계] 진화 진행 (빛남 효과)
            // ==========================================
            EvolutionMessage = $"{oldName}의 상태가...?!";
            IsEvolvingFlash = true; // XAML의 하얀 빛 애니메이션 켜기
            PosY = -20; // 둥둥 떠오르는 연출
            CheckStateAndAnimate();
            await Task.Delay(3000); // 3초 대기

            // ==========================================
            // 🎬 [3단계] 진화 완료
            // ==========================================
            IsEvolvingFlash = false;
            PosY = 0;

            ExecuteActualEvolve(); // 🌟 실제 데이터 변경

            _tempActionId = 8; // ANIM_HOP
            _tempActionTimer = 60;
            CheckStateAndAnimate();

            EvolutionMessage = $"축하합니다! {oldName}은(는)\n{Name}(으)로 진화했습니다!";
            await Task.Delay(3500); // 메시지 표시 대기

            // 연출 종료
            Ceremony = 0;
            EvolutionMessage = "";
        }

        // 🌟 실제 포켓몬 아이디(SpeciesId)를 다음 진화 단계로 바꾸는 헬퍼 함수
        // 🌟 모든 다중 진화 포켓몬이 공평한 무작위 확률로 진화하도록 개선된 함수
        private void ExecuteActualEvolve()
        {
            if (!DexTable.ContainsKey(SpeciesId)) return;

            // 해당 포켓몬이 진화할 수 있는 모든 다음 형태의 목록을 가져옵니다.
            int[] possibleNextForms = DexTable[SpeciesId].NextSpeciesIds;

            int nextSpeciesId = -1;

            // 🌟 이브이 및 기타 다중 진화 포켓몬 구분 없이 모두 공평한 랜덤으로 결정합니다.
            if (possibleNextForms != null && possibleNextForms.Length > 0)
            {
                int randomIndex = new Random().Next(possibleNextForms.Length);
                nextSpeciesId = possibleNextForms[randomIndex];
            }

            if (nextSpeciesId == -1) return;

            SpeciesId = nextSpeciesId;

            // 🌟 [핵심 해결책] 덮어씌워져 있던 옛날 이름을 지워서, 새 종족값에 맞는 이름(비퀸)을 도감에서 다시 불러오도록 합니다!
            _overrideName = null;
            OnPropertyChanged(nameof(Name));

            IsEvolutionPostponed = false;
            _ageSeconds = 0; AgeMinutes = 0; ResetPosition();

            // 진화한 형태가 최초 발견이라면 도감에 등록합니다.
            if (SpeciesId > 0 && !UnlockedPokemon.Contains(SpeciesId))
            {
                UnlockedPokemon.Add(SpeciesId);
                UnlockedPokemon.Sort();
                RegisteredCount = UnlockedPokemon.Count;
            }
            RefreshPokedex(); Save();
        }

        // 🌟 새 게임(알 상태)을 준비하는 함수
        // 🌟 새 게임(알 상태)을 준비하는 함수
        public void PrepareNewEgg()
        {
            SpeciesId = -1; EggTaps = 0; _ageSeconds = 0; AgeMinutes = 0;

            // 🌟 핵심 수정: 예전 포켓몬의 이름과 레벨 정보가 남아있지 않도록 깨끗하게 지워줍니다!
            _overrideName = null;
            _overrideLevel = null;

            Fullness = 80; Joy = 80; Energy = 80; Hygiene = 100;
            Poops = 0; Weight = 10; CareMistakes = 0; Bond = 0; NeglectTicks = 0;
            IsSleeping = false; IsShiny = false; Genes = new PokemonGene(); BerryKnown = false;
            Ceremony = 0; IsEvolutionPostponed = false; IsFarewellPostponed = false;

            // 기존 창 닫기 로직
            IsBallGameOpen = false; IsCatchGameOpen = false; IsMemoGameOpen = false; IsCleanGameOpen = false;
            IsBattleOpen = false; IsAttackMenuOpen = false; IsBattleResolved = false; IsCatchOffered = false;

            // 파티 창과 교체 모드 상태를 강제로 닫아 IdleView로 돌아가게 합니다.
            IsPartyOpen = false;
            IsSwapMode = false;
            _pendingRetiree = null; // 대기 중인 은퇴 포켓몬도 비워줍니다.

            TrAtk = 0; TrDef = 0; TrSpeed = 0; Medals = 0;
            for (int i = 0; i < 4; i++) Skills[i] = 0;

            OnPropertyChanged(nameof(CurrentSkills));
            OnPropertyChanged(nameof(PoopDisplay)); OnPropertyChanged(nameof(IsEating)); OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(Name));

            ResetPosition();
        }

        // 🌟 이별 및 작별 관련 타이머 이벤트
        public void StartFarewell()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition(); // 🌟 핵심: 인사를 시작하기 전에 중앙으로 이동시킵니다!
            LastEnd = 1;
            Ceremony = 1;
            ExecuteCeremonyTimer();
        }

        public void StartRunaway()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition(); // 🌟 가출할 때도 중앙에서 시작!
            /* 도망은 파티에 포함되지 않으므로 RetireToParty()를 호출하지 않습니다 */
            LastEnd = 2;
            Ceremony = 2;
            ExecuteCeremonyTimer();
        }

        public void StartRelease()
        {
            if (IsEgg || Ceremony != 0) return;
            ResetPosition(); // 🌟 방생할 때도 중앙에서 시작!
            LastEnd = 3;
            Ceremony = 3;
            ExecuteCeremonyTimer();
        }

        private void ExecuteCeremonyTimer()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            timer.Tick += (s, e) => { timer.Stop(); Ceremony = 0; PrepareNewEgg(); };
            timer.Start();
        }

        // 🌟 데이터를 완전히 지우고 처음으로 되돌리는 함수
        public void FactoryReset()
        {
            if (System.IO.File.Exists(SaveFilePath)) System.IO.File.Delete(SaveFilePath);

            UnlockedPokemon.Clear(); FullPokedex.Clear(); RegisteredCount = 0; Streak = 0; LastEnd = 1; LastPlayedDate = DateTime.Now.Date;
            GameHighScore = 0; CatchHighScore = 0; MemoHighScore = 0; CleanHighScore = 0;

            if (Party != null)
            {
                Party.Clear();
            }

            // ==========================================
            // 🌟 [추가됨] 새 게임 시작 시 기본 아이템 지급
            // ==========================================
            MonsterBalls = 5; // 포켓볼 5개 지급
            Potions = 3;   // 치료약 1개 지급

            PrepareNewEgg();
            RefreshPokedex();
            Save();
        }

        #endregion
    }
}