using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    // 🌟 partial 키워드를 사용하여 기존 PokemonState 클래스에 합쳐집니다.
    public partial class PokemonState
    {
        // UI와 자동으로 동기화되며, 최대 6마리까지 저장될 파티 리스트입니다.
        public ObservableCollection<PartyMember> Party { get; set; } = new();

        // 🌟 작별(Farewell) 또는 방생 시 호출하여 현재 포켓몬을 파티에 추가하는 함수입니다.
        private PartyMember? _pendingRetiree = null;

        private bool _isSwapMode = false;
        public bool IsSwapMode
        {
            get => _isSwapMode;
            set => SetProperty(ref _isSwapMode, value);
        }

        public void RetireToParty()
        {
            var retiredPokemon = new PartyMember
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

            if (Party.Count < 6)
            {
                Party.Add(retiredPokemon);
            }
            else
            {
                // 🌟 파티가 꽉 찼다면 교체 모드로 진입하고 파티 화면을 강제로 엽니다.
                _pendingRetiree = retiredPokemon;
                IsSwapMode = true;
                IsPartyOpen = true;
            }
        }

        private bool _isPartyOpen = false;
        public bool IsPartyOpen
        {
            get => _isPartyOpen;
            set
            {
                if (SetProperty(ref _isPartyOpen, value))
                {
                    OnPropertyChanged(nameof(IsAliveAndNotEgg));
                    OnPropertyChanged(nameof(MoodText));
                }
            }
        }

        public void ExecuteSwap(PartyMember targetToReplace)
        {
            if (_pendingRetiree != null && Party.Contains(targetToReplace))
            {
                int index = Party.IndexOf(targetToReplace);
                Party[index] = _pendingRetiree; // 기존 멤버를 밀어내고 새 멤버를 넣습니다.

                _pendingRetiree = null;
                IsSwapMode = false;
            }
        }

        public void SwapMainWithParty(PartyMember targetMember)
        {
            if (targetMember == null || !Party.Contains(targetMember)) return;

            int index = Party.IndexOf(targetMember);
            if (index < 0) return;

            // 1. 현재 메인 포켓몬의 스탯 및 모든 세부 정보를 백업용 객체로 만듭니다.
            var oldMain = new PartyMember
            {
                SpeciesId = this.SpeciesId,
                Name = this.Name.Replace(" ✨", ""),
                Level = this.Level,
                IsShiny = this.IsShiny,
                TrAtk = this.TrAtk,
                TrDef = this.TrDef,
                TrSpeed = this.TrSpeed,
                Skills = this.Skills != null ? (int[])this.Skills.Clone() : new int[4],
                Genes = this.Genes != null ? new PokemonGene
                {
                    HpGene = this.Genes.HpGene,
                    AtkGene = this.Genes.AtkGene,
                    DefGene = this.Genes.DefGene,
                    SpeGene = this.Genes.SpeGene
                } : new PokemonGene()
            };

            // 2. 선택된 파티 멤버의 데이터를 메인 화면(PokemonState)으로 가져옵니다.
            this.SpeciesId = targetMember.SpeciesId;
            this.Name = targetMember.Name;
            this.Level = targetMember.Level;
            this.IsShiny = targetMember.IsShiny;
            this.TrAtk = targetMember.TrAtk;
            this.TrDef = targetMember.TrDef;
            this.TrSpeed = targetMember.TrSpeed;

            if (targetMember.Skills != null)
            {
                this.Skills = (int[])targetMember.Skills.Clone();
            }

            if (targetMember.Genes != null)
            {
                this.Genes = new PokemonGene
                {
                    HpGene = targetMember.Genes.HpGene,
                    AtkGene = targetMember.Genes.AtkGene,
                    DefGene = targetMember.Genes.DefGene,
                    SpeGene = targetMember.Genes.SpeGene
                };
            }

            // 3. 파티 리스트의 해당 위치(index)에 방금 백업한 옛날 메인 포켓몬을 대입합니다.
            Party[index] = oldMain;

            // 4. 선택 상태 초기화 및 파티 창 닫기
            targetMember.IsSelected = false;
            oldMain.IsSelected = false;
            IsPartyOpen = false;

            // 5. 화면 갱신 및 저장
            UpdateBackgroundImage();
            CheckStateAndAnimate();
            Save();
        }

        // 🌟 교체를 취소하고 7번째 포켓몬을 그냥 놔주는 함수입니다.
        public void CancelSwap()
        {
            _pendingRetiree = null;
            IsSwapMode = false;
        }

        // 🌟 화면을 열고 닫는 간단한 헬퍼 함수
        public void OpenParty() { IsPartyOpen = true; }
        public void CloseParty() { IsPartyOpen = false; }
    }
}