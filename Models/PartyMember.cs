using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    // 🌟 포켓몬의 모든 고유 상태를 완벽하게 기억하는 파티 멤버 클래스입니다.
    public class PartyMember : INotifyPropertyChanged
    {
        public int SpeciesId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int AgeMinutes { get; set; }
        public bool IsShiny { get; set; }

        public int TrAtk { get; set; }
        public int TrDef { get; set; }
        public int TrSpeed { get; set; }

        // 🌟 포켓몬의 컨디션과 고유 상태 완벽 보존
        public int Fullness { get; set; }
        public int Joy { get; set; }
        public int Energy { get; set; }
        public int Hygiene { get; set; }
        public int Bond { get; set; }
        public int Weight { get; set; }
        public bool IsEvolutionPostponed { get; set; }
        public int Medals { get; set; } // 비트마스크 정수형 유지

        public int[] Skills { get; set; } = new int[4];
        public PokemonGene Genes { get; set; } = new PokemonGene();

        private bool _isFirst;
        [JsonIgnore]
        public bool IsFirst
        {
            get => _isFirst;
            set
            {
                if (_isFirst != value)
                {
                    _isFirst = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public string LevelDisplay => $"Lv.{Level}";

        [JsonIgnore]
        public string Type1Display
        {
            get
            {
                var pokemon = PokemonDex.AllPokemons.FirstOrDefault(p => p.Id == SpeciesId);
                return pokemon != null ? pokemon.Type1.ToString().ToUpper() : "";
            }
        }

        [JsonIgnore]
        public string Type2Display
        {
            get
            {
                var pokemon = PokemonDex.AllPokemons.FirstOrDefault(p => p.Id == SpeciesId);
                if (pokemon == null || pokemon.Type2 == PokemonType.None) return "";
                return pokemon.Type2.ToString().ToUpper();
            }
        }

        [JsonIgnore]
        public System.Windows.Media.ImageSource? SpriteImage
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string normalPath = System.IO.Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"p{SpeciesId:D3}.bin");
                string shinyPath = System.IO.Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"ps{SpeciesId:D3}.bin");

                string targetPath = (IsShiny && System.IO.File.Exists(shinyPath)) ? shinyPath : normalPath;

                if (System.IO.File.Exists(targetPath))
                {
                    var frames = TamaPoke.Utils.Service.Tpk2Decoder.LoadAnimation(targetPath, 0);
                    if (frames != null && frames.Length > 0) return frames[0];
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}