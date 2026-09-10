using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        public ObservableCollection<PartyMember> Party { get; set; } = new();

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
                SpriteFileName = this.SpriteFileName,
                Level = this.Level,
                AgeMinutes = this.AgeMinutes,
                IsShiny = this.IsShiny,
                TrAtk = this.TrAtk,
                TrDef = this.TrDef,
                TrSpeed = this.TrSpeed,
                Fullness = this.Fullness,
                Joy = this.Joy,
                Energy = this.Energy,
                Hygiene = this.Hygiene,
                Bond = this.Bond,
                Weight = this.Weight,
                IsEvolutionPostponed = this.IsEvolutionPostponed,
                Medals = this.Medals,
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
                Party.Add(retiredPokemon);
            else
            {
                _pendingRetiree = retiredPokemon;
                IsSwapMode = true;
                IsPartyOpen = true;
            }
        }

        public void UpdatePartyFirstFlags()
        {
            if (Party == null) return;
            for (int i = 0; i < Party.Count; i++) Party[i].IsFirst = (i == 0);
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
                Party[index] = _pendingRetiree;
                _pendingRetiree = null;
                IsSwapMode = false;
            }
        }

        public void SwapMainWithParty(PartyMember targetMember)
        {
            if (targetMember == null || !Party.Contains(targetMember)) return;

            int targetIndex = Party.IndexOf(targetMember);
            if (targetIndex == 0) return;

            var newLeader = new PartyMember
            {
                SpeciesId = targetMember.SpeciesId,
                Name = targetMember.Name,
                SpriteFileName = targetMember.SpriteFileName,
                Level = targetMember.Level,
                AgeMinutes = targetMember.AgeMinutes,
                IsShiny = targetMember.IsShiny,
                TrAtk = targetMember.TrAtk,
                TrDef = targetMember.TrDef,
                TrSpeed = targetMember.TrSpeed,
                Fullness = targetMember.Fullness,
                Joy = targetMember.Joy,
                Energy = targetMember.Energy,
                Hygiene = targetMember.Hygiene,
                Bond = targetMember.Bond,
                Weight = targetMember.Weight,
                IsEvolutionPostponed = targetMember.IsEvolutionPostponed,
                Medals = targetMember.Medals,
                Skills = targetMember.Skills != null ? (int[])targetMember.Skills.Clone() : new int[4],
                Genes = targetMember.Genes != null ? new PokemonGene { HpGene = targetMember.Genes.HpGene, AtkGene = targetMember.Genes.AtkGene, DefGene = targetMember.Genes.DefGene, SpeGene = targetMember.Genes.SpeGene } : new PokemonGene()
            };

            var oldLeader = new PartyMember
            {
                SpeciesId = this.SpeciesId,
                Name = this.Name.Replace(" ✨", ""),
                SpriteFileName = this.SpriteFileName,
                Level = this.Level,
                AgeMinutes = this.AgeMinutes,
                IsShiny = this.IsShiny,
                TrAtk = this.TrAtk,
                TrDef = this.TrDef,
                TrSpeed = this.TrSpeed,
                Fullness = this.Fullness,
                Joy = this.Joy,
                Energy = this.Energy,
                Hygiene = this.Hygiene,
                Bond = this.Bond,
                Weight = this.Weight,
                IsEvolutionPostponed = this.IsEvolutionPostponed,
                Medals = this.Medals,
                Skills = this.Skills != null ? (int[])this.Skills.Clone() : new int[4],
                Genes = this.Genes != null ? new PokemonGene { HpGene = this.Genes.HpGene, AtkGene = this.Genes.AtkGene, DefGene = this.Genes.DefGene, SpeGene = this.Genes.SpeGene } : new PokemonGene()
            };

            Party.RemoveAt(targetIndex);
            Party.Insert(targetIndex, oldLeader);
            Party.RemoveAt(0);
            Party.Insert(0, newLeader);

            this.SpeciesId = newLeader.SpeciesId;
            this.SpriteFileName = newLeader.SpriteFileName;
            this.Name = newLeader.Name;
            this._overrideLevel = null;
            this.AgeMinutes = newLeader.AgeMinutes;
            _ageSeconds = this.AgeMinutes * 60;
            this.IsShiny = newLeader.IsShiny;
            this.TrAtk = newLeader.TrAtk;
            this.TrDef = newLeader.TrDef;
            this.TrSpeed = newLeader.TrSpeed;
            this.Fullness = newLeader.Fullness;
            this.Joy = newLeader.Joy;
            this.Energy = newLeader.Energy;
            this.Hygiene = newLeader.Hygiene;
            this.Bond = newLeader.Bond;
            this.Weight = newLeader.Weight;
            this.IsEvolutionPostponed = newLeader.IsEvolutionPostponed;
            this.Medals = newLeader.Medals;

            if (newLeader.Skills != null) this.Skills = (int[])newLeader.Skills.Clone();
            if (newLeader.Genes != null) this.Genes = new PokemonGene { HpGene = newLeader.Genes.HpGene, AtkGene = newLeader.Genes.AtkGene, DefGene = newLeader.Genes.DefGene, SpeGene = newLeader.Genes.SpeGene };

            for (int i = 0; i < Party.Count; i++)
            {
                Party[i].IsFirst = (i == 0);
                Party[i].IsSelected = false;
            }

            IsPartyOpen = false;
            UpdateBackgroundImage();
            CheckStateAndAnimate();
            Save();
        }

        public void CancelSwap()
        {
            _pendingRetiree = null;
            IsSwapMode = false;
        }

        public void SyncMainToLeader()
        {
            if (Party == null) return;

            var currentMain = new PartyMember
            {
                SpeciesId = this.SpeciesId,
                Name = this.Name.Replace(" ✨", ""),
                SpriteFileName = this.SpriteFileName,
                Level = this.Level,
                AgeMinutes = this.AgeMinutes,
                IsShiny = this.IsShiny,
                TrAtk = this.TrAtk,
                TrDef = this.TrDef,
                TrSpeed = this.TrSpeed,
                Fullness = this.Fullness,
                Joy = this.Joy,
                Energy = this.Energy,
                Hygiene = this.Hygiene,
                Bond = this.Bond,
                Weight = this.Weight,
                IsEvolutionPostponed = this.IsEvolutionPostponed,
                Medals = this.Medals,
                Skills = this.Skills != null ? (int[])this.Skills.Clone() : new int[4],
                Genes = this.Genes != null ? new PokemonGene { HpGene = this.Genes.HpGene, AtkGene = this.Genes.AtkGene, DefGene = this.Genes.DefGene, SpeGene = this.Genes.SpeGene } : new PokemonGene()
            };

            if (Party.Count == 0) Party.Add(currentMain);
            else { Party.RemoveAt(0); Party.Insert(0, currentMain); }
        }

        public void OpenParty()
        {
            SyncMainToLeader();
            UpdatePartyFirstFlags();
            IsPartyOpen = true;
        }

        public void CloseParty()
        {
            IsPartyOpen = false;
        }
    }
}