using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    // 파티 규칙: 메인(대표) 포켓몬은 항상 Party[0]에 위치합니다.
    // 메인이 알이거나 이별/가출/방생 연출 중일 때는 Party[0]을 건드리지 않습니다.
    public partial class PokemonState
    {
        public ObservableCollection<PartyMember> Party { get; set; } = new();

        private PartyMember? _pendingRetiree = null;

        private bool _isSwapMode = false;
        [JsonIgnore] public bool IsSwapMode
        {
            get => _isSwapMode;
            set => SetProperty(ref _isSwapMode, value);
        }

        // 메인 포켓몬이 파티에 반영될 수 있는 상태인지 (알/연출 중에는 반영하지 않음)
        private bool CanSyncLeader => !IsEgg && Ceremony == 0;

        // 현재 메인 포켓몬의 상태를 파티 멤버 데이터로 복사합니다.
        private PartyMember CreatePartyMemberSnapshot()
        {
            return new PartyMember
            {
                SpeciesId = this.SpeciesId,
                Name = this.Name.Replace(" ✨", ""), // 이름의 이로치 별표는 제거하고 순수 이름만 저장
                SpriteFileName = this.SpriteFileName,
                Level = this.Level,
                AgeMinutes = this.AgeMinutes,
                IsShiny = this.IsShiny,
                IsFemale = this.IsFemale,
                IsGenderless = this.IsGenderless,
                TrAtk = this.TrAtk,
                TrDef = this.TrDef,
                TrSpeed = this.TrSpeed,
                Fullness = this.Fullness,
                Joy = this.Joy,
                Energy = this.Energy,
                Hygiene = this.Hygiene,
                Bond = this.Bond,
                Weight = this.Weight,
                CareMistakes = this.CareMistakes,
                BerryKnown = this.BerryKnown,
                IsEvolutionPostponed = this.IsEvolutionPostponed,
                Medals = this.Medals,
                Skills = this.Skills != null ? (int[])this.Skills.Clone() : new int[4],
                Genes = CloneGenes(this.Genes)
            };
        }

        private static PokemonGene CloneGenes(PokemonGene? g) => g == null
            ? new PokemonGene()
            : new PokemonGene { HpGene = g.HpGene, AtkGene = g.AtkGene, DefGene = g.DefGene, SpeGene = g.SpeGene };

        // 파티 멤버 데이터를 메인 포켓몬 상태로 불러옵니다.
        private void LoadFromPartyMember(PartyMember m)
        {
            _suspendMedalCheck = true; // 값을 다 채운 뒤 한 번만 판정
            _overrideName = null;
            _overrideLevel = null;

            this.SpeciesId = m.SpeciesId;
            this.SpriteFileName = m.SpriteFileName;
            this.AgeMinutes = m.AgeMinutes;
            _ageSeconds = this.AgeMinutes * 60;
            this.IsShiny = m.IsShiny;
            this.IsFemale = m.IsFemale;
            this.IsGenderless = m.IsGenderless;
            this.TrAtk = m.TrAtk;
            this.TrDef = m.TrDef;
            this.TrSpeed = m.TrSpeed;
            this.Fullness = m.Fullness;
            this.Joy = m.Joy;
            this.Energy = m.Energy;
            this.Hygiene = m.Hygiene;
            this.Bond = m.Bond;
            this.Weight = m.Weight;
            this.CareMistakes = m.CareMistakes;
            this.BerryKnown = m.BerryKnown;
            this.IsEvolutionPostponed = m.IsEvolutionPostponed;
            this.Medals = m.Medals;
            this.Skills = m.Skills != null ? (int[])m.Skills.Clone() : new int[4];
            this.Genes = CloneGenes(m.Genes);

            // 교체되어 나온 포켓몬은 깨어 있는 상태로 시작합니다.
            IsSleeping = false;
            IsFarewellPostponed = false;
            NeglectTicks = 0;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(GenderDisplay));
            OnPropertyChanged(nameof(GenderColor));
            OnPropertyChanged(nameof(FavoriteBerryName));
            OnPropertyChanged(nameof(CurrentSkills));

            _suspendMedalCheck = false;
            CheckMedals();
        }

        public void UpdatePartyFirstFlags()
        {
            if (Party == null) return;
            for (int i = 0; i < Party.Count; i++) Party[i].IsFirst = (i == 0);
        }

        private bool _isPartyOpen = false;
        [JsonIgnore] public bool IsPartyOpen
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
            if (_pendingRetiree == null || !Party.Contains(targetToReplace)) return;

            int index = Party.IndexOf(targetToReplace);
            if (index == 0 && CanSyncLeader) return; // 메인 포켓몬 자리는 교체 대상이 아닙니다.

            Party[index] = _pendingRetiree;
            _pendingRetiree = null;
            IsSwapMode = false;
            UpdatePartyFirstFlags();
            Save();
        }

        public void SwapMainWithParty(PartyMember targetMember)
        {
            if (targetMember == null || !Party.Contains(targetMember)) return;
            if (!CanSyncLeader) return; // 알 상태/연출 중에는 교체 불가

            int targetIndex = Party.IndexOf(targetMember);
            if (targetIndex == 0) return;

            var oldLeader = CreatePartyMemberSnapshot();

            // 선택한 멤버를 맨 앞(메인)으로, 기존 메인을 그 자리로 보냅니다.
            Party[targetIndex] = oldLeader;
            if (Party.Count > 0) Party[0] = targetMember;
            else Party.Add(targetMember);

            LoadFromPartyMember(targetMember);

            foreach (var member in Party) member.IsSelected = false;
            UpdatePartyFirstFlags();

            IsPartyOpen = false;
            ResetPosition();
            UpdateBackgroundImage();
            ReloadSprite();
            Save();
        }

        public void CancelSwap()
        {
            _pendingRetiree = null;
            IsSwapMode = false;
        }

        // 메인 포켓몬의 최신 상태를 Party[0]에 반영합니다.
        public void SyncMainToLeader()
        {
            if (Party == null || !CanSyncLeader) return;

            var currentMain = CreatePartyMemberSnapshot();
            currentMain.IsFirst = true;

            if (Party.Count == 0) Party.Add(currentMain);
            else Party[0] = currentMain;
        }

        // 메인 포켓몬이 파티를 떠날 때(이별/가출/방생) Party[0]을 제거합니다.
        private void RemoveLeaderFromParty()
        {
            if (Party != null && Party.Count > 0) Party.RemoveAt(0);
            UpdatePartyFirstFlags();
        }

        // 새로 태어난 포켓몬을 메인 자리(Party[0])에 넣습니다.
        private void InsertNewLeader(PartyMember newborn)
        {
            newborn.IsFirst = true;
            if (Party.Count < 6) Party.Insert(0, newborn);
            else Party[0] = newborn;
            UpdatePartyFirstFlags();
        }

        // 이전 버전에서 메인 포켓몬이 파티 뒤쪽에 들어가 있던 세이브를 바로잡습니다.
        // (부화한 포켓몬이 맨 뒤에 추가되던 버그) 유전자까지 같은 멤버를 메인으로 판단합니다.
        private void RepairLeaderSlot()
        {
            if (Party == null || Party.Count == 0 || !CanSyncLeader) return;

            bool IsMain(PartyMember p) =>
                p.SpeciesId == SpeciesId && p.SpriteFileName == SpriteFileName && p.Genes != null &&
                p.Genes.HpGene == Genes.HpGene && p.Genes.AtkGene == Genes.AtkGene &&
                p.Genes.DefGene == Genes.DefGene && p.Genes.SpeGene == Genes.SpeGene;

            bool leaderOk = IsMain(Party[0]);

            // Party[0] 이외의 자리에 있는 메인 포켓몬(뒤쪽에 추가됐거나 복제된 것)을 제거합니다.
            bool removed = false;
            for (int i = Party.Count - 1; i >= 1; i--)
            {
                if (IsMain(Party[i])) { Party.RemoveAt(i); removed = true; }
            }

            if (!leaderOk && removed) InsertNewLeader(CreatePartyMemberSnapshot());
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
