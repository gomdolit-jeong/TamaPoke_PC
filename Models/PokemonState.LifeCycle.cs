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
            DexTable[44] = (36, new int[] { 45, 182 }); // 냄새꼬
            DexTable[61] = (36, new int[] { 62, 186 }); // 슈륙챙이
            DexTable[79] = (37, new int[] { 80, 199 }); // 야돈
            DexTable[133] = (30, new int[] { 134, 135, 136, 196, 197, 470, 471 }); // 이브이
            DexTable[236] = (20, new int[] { 106, 107, 237 }); // 배루키
            DexTable[265] = (7, new int[] { 266, 268 }); // 개무소
            DexTable[361] = (42, new int[] { 362, 478 }); // 눈꼬마
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

        // 🌟 알에서 깨어날 때 포켓몬을 랜덤으로 정해주는 함수
        // 🌟 알에서 깨어날 때 포켓몬을 정하고 파티에 등록하는 함수
        public void Hatch()
        {
            Random rand = new Random();
            int shinyBase = (LastEnd == 1 ? 24 : 48) - CareBonus();
            if (shinyBase < 8) shinyBase = 8;
            IsShiny = (rand.Next(shinyBase) == 0);

            var childIds = DexTable.Values.SelectMany(v => v.NextSpeciesIds).ToHashSet();

            var baseSpeciesIds = PokemonDex.AllPokemons
                .Where(p => p.Id <= 493
                            && !childIds.Contains(p.Id)
                            && !LegendaryIds.Contains(p.Id))
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

            // ==========================================
            // 🌟 [추가됨] 부화한 포켓몬을 파티 시스템에 자동 등록
            // ==========================================
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

            // 파티가 비어있다면 곧바로 추가하고, 자리가 있다면 첫 자리에 넣어줍니다.
            if (Party.Count == 0)
            {
                Party.Add(newbornMember);
            }
            else
            {
                // 원하시는 경우 기존 파티의 첫 번째 자리를 갱신하거나 리스트에 추가할 수 있습니다.
                // 여기서는 새 생명이 탄생했을 때 파티에 자리가 남았다면 자동 추가되도록 처리합니다.
                if (Party.Count < 6)
                {
                    Party.Add(newbornMember);
                }
                else
                {
                    // 파티가 이미 6마리로 꽉 찬 상태라면 첫 번째 멤버를 교체하거나 
                    // 혹은 기존처럼 벤치에 두는 등의 정책을 쓸 수 있습니다. 
                    // 여기서는 안전하게 첫 번째 슬롯을 새로 태어난 아이로 갱신해 줍니다.
                    Party[0] = newbornMember;
                }
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
        private void ExecuteActualEvolve()
        {
            if (!DexTable.ContainsKey(SpeciesId)) return;

            int[] possibleNextForms = DexTable[SpeciesId].NextSpeciesIds;

            int nextSpeciesId = -1;
            if (SpeciesId == 133)
            {
                int[] eveeForms = new[] { 134, 135, 136 };
                List<int> uncollectedForms = new List<int>();
                foreach (int form in eveeForms)
                {
                    if (!UnlockedPokemon.Contains(form)) uncollectedForms.Add(form);
                }
                nextSpeciesId = uncollectedForms.Count > 0 ? uncollectedForms[new Random().Next(uncollectedForms.Count)] : eveeForms[new Random().Next(eveeForms.Length)];
            }
            else
            {
                nextSpeciesId = possibleNextForms[new Random().Next(possibleNextForms.Length)];
            }

            SpeciesId = nextSpeciesId;
            IsEvolutionPostponed = false;
            _ageSeconds = 0; AgeMinutes = 0; ResetPosition();

            if (SpeciesId > 0 && !UnlockedPokemon.Contains(SpeciesId))
            {
                UnlockedPokemon.Add(SpeciesId);
                UnlockedPokemon.Sort();
                RegisteredCount = UnlockedPokemon.Count;
            }
            RefreshPokedex(); Save();
        }

        // 🌟 새 게임(알 상태)을 준비하는 함수
        public void PrepareNewEgg()
        {
            SpeciesId = -1; EggTaps = 0; _ageSeconds = 0; AgeMinutes = 0;
            Fullness = 80; Joy = 80; Energy = 80; Hygiene = 100;
            Poops = 0; Weight = 10; CareMistakes = 0; Bond = 0; NeglectTicks = 0;
            IsSleeping = false; IsShiny = false; Genes = new PokemonGene(); BerryKnown = false;
            Ceremony = 0; IsEvolutionPostponed = false; IsFarewellPostponed = false;

            // 기존 창 닫기 로직
            IsBallGameOpen = false; IsCatchGameOpen = false; IsMemoGameOpen = false; IsCleanGameOpen = false;
            IsBattleOpen = false; IsAttackMenuOpen = false; IsBattleResolved = false; IsCatchOffered = false;

            // 🌟 추가됨: 파티 창과 교체 모드 상태를 강제로 닫아 IdleView로 돌아가게 합니다.
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
        public void StartFarewell() { if (IsEgg || Ceremony != 0) return; RetireToParty(); LastEnd = 1; Ceremony = 1; ExecuteCeremonyTimer(); }
        public void StartRunaway() { if (IsEgg || Ceremony != 0) return; /* 도망은 파티에 포함되지 않으므로 RetireToParty()를 호출하지 않습니다 */ LastEnd = 2; Ceremony = 2; ExecuteCeremonyTimer(); }
        public void StartRelease() { if (IsEgg || Ceremony != 0) return; RetireToParty(); LastEnd = 3; Ceremony = 3; ExecuteCeremonyTimer(); }

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

            // 🌟 추가됨: 공장 초기화 시 파티에 보관된 포켓몬 데이터도 완벽하게 삭제합니다.
            if (Party != null)
            {
                Party.Clear();
            }

            PrepareNewEgg(); // 이 함수가 호출되면서 파티 창도 자동으로 닫힙니다.
            RefreshPokedex();
            Save();
        }

        #endregion
    }
}