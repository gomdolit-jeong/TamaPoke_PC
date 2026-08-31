using System;
using System.Linq;
using System.ComponentModel; // 🌟 추가됨
using System.Runtime.CompilerServices; // 🌟 추가됨
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    // 🌟 INotifyPropertyChanged 인터페이스를 상속받습니다.
    public class PartyMember : INotifyPropertyChanged
    {
        public int SpeciesId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsShiny { get; set; }

        public int TrAtk { get; set; }
        public int TrDef { get; set; }
        public int TrSpeed { get; set; }
        public int[] Skills { get; set; } = new int[4];
        public PokemonGene Genes { get; set; } = new PokemonGene();

        // 🌟 화면에서 이 카드가 선택되었는지 여부를 저장합니다. (저장 파일에는 무시됨)
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

        // 🌟 UI 업데이트를 위한 이벤트 구현
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}