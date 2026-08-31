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

            // 파티 슬롯이 6칸 미만일 때만 들어갈 수 있습니다.
            if (Party.Count < 6)
            {
                Party.Add(retiredPokemon);
            }
            else
            {
                // 파티가 6마리로 꽉 찼을 때의 임시 처리
                // 추후 UI에서 교체 대상을 직접 선택하는 화면을 띄워야 합니다.
                System.Diagnostics.Debug.WriteLine("파티가 가득 차서 직접 교체해야 합니다!");
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

        // 🌟 화면을 열고 닫는 간단한 헬퍼 함수
        public void OpenParty() { IsPartyOpen = true; }
        public void CloseParty() { IsPartyOpen = false; }
    }
}