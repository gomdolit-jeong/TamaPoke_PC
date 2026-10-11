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
    public partial class PokemonState
    {
        #region 도감 (Pokedex)
        [JsonIgnore]
        public ObservableCollection<PokedexEntry> FullPokedex { get; set; } = new ObservableCollection<PokedexEntry>();

        [JsonIgnore]
        public ObservableCollection<PokedexEntry> FilteredPokedex { get; set; } = new ObservableCollection<PokedexEntry>();

        [JsonIgnore] public string KantoCountText => RegionCountText(1);
        [JsonIgnore] public string JohtoCountText => RegionCountText(2);
        [JsonIgnore] public string HoennCountText => RegionCountText(3);
        [JsonIgnore] public string SinnohCountText => RegionCountText(4);
        [JsonIgnore] public string UnovaCountText => RegionCountText(5);
        [JsonIgnore] public string KalosCountText => RegionCountText(6);
        [JsonIgnore] public string AlolaCountText => RegionCountText(7);
        [JsonIgnore] public string GalarCountText => RegionCountText(8);
        [JsonIgnore] public string PaldeaCountText => RegionCountText(9);

        private string RegionCountText(int generation)
        {
            var r = GameRegions.ByGeneration(generation)!;
            return $"({FullPokedex.Count(p => r.Contains(p.Id) && p.IsUnlocked)}/{r.Count})";
        }

        private int _currentRegionIndex = 0;
        [JsonIgnore] public int CurrentRegionIndex
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
        [JsonIgnore] public bool IsShowOnlyUnlocked
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
            var region = GameRegions.ByGeneration(CurrentRegionIndex + 1);
            int startId = region?.StartId ?? 1, endId = region?.EndId ?? GameConstants.MAX_POKEMON_ID;

            foreach (var entry in FullPokedex.Where(p => p.Id >= startId && p.Id <= endId))
            {
                if (IsShowOnlyUnlocked && !entry.IsUnlocked)
                    continue;

                FilteredPokedex.Add(entry);
            }
        }

        private bool _isDexOpen = false;
        [JsonIgnore] public bool IsDexOpen
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

            // 지역별 수집 수 표시 갱신 (1~9세대 전체)
            OnPropertyChanged(nameof(KantoCountText)); OnPropertyChanged(nameof(JohtoCountText)); OnPropertyChanged(nameof(HoennCountText));
            OnPropertyChanged(nameof(SinnohCountText)); OnPropertyChanged(nameof(UnovaCountText)); OnPropertyChanged(nameof(KalosCountText));
            OnPropertyChanged(nameof(AlolaCountText)); OnPropertyChanged(nameof(GalarCountText)); OnPropertyChanged(nameof(PaldeaCountText));
        }

        public void UnlockPokemonInPokedex(int speciesId)
        {
            if (UnlockedPokemon == null) UnlockedPokemon = new List<int>();

            if (!UnlockedPokemon.Contains(speciesId))
            {
                UnlockedPokemon.Add(speciesId);
                OnPropertyChanged(nameof(RegisteredCount));
                RefreshPokedex();
                Save();
            }
        }
        #endregion
    }
}
