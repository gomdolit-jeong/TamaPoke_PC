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

        // 🌟 핵심 추가: 파티에 들어가서도 이로치/특수 폼 파일명을 잊어버리지 않게 저장합니다!
        public string SpriteFileName { get; set; } = string.Empty;

        public int Level { get; set; }
        public int AgeMinutes { get; set; }
        public bool IsShiny { get; set; }

        public int TrAtk { get; set; }
        public int TrDef { get; set; }
        public int TrSpeed { get; set; }

        public int Fullness { get; set; }
        public int Joy { get; set; }
        public int Energy { get; set; }
        public int Hygiene { get; set; }
        public int Bond { get; set; }
        public int Weight { get; set; }
        public bool IsEvolutionPostponed { get; set; }
        public int Medals { get; set; }

        public int[] Skills { get; set; } = new int[4];
        public PokemonGene Genes { get; set; } = new PokemonGene();

        private bool _isFirst;
        [JsonIgnore]
        public bool IsFirst
        {
            get => _isFirst;
            set { if (_isFirst != value) { _isFirst = value; OnPropertyChanged(); } }
        }

        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
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

                // 🌟 저장된 고유 파일명이 있다면 그걸 쓰고, 없으면 기본 번호 파일명으로 대처합니다.
                string targetFile = string.IsNullOrEmpty(SpriteFileName) ? $"p{SpeciesId:D4}.bin" : SpriteFileName;
                string targetPath = System.IO.Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", targetFile);

                if (System.IO.File.Exists(targetPath))
                {
                    // 완벽하게 1프레임만 잘라주는 썸네일 리더기를 사용합니다.
                    return TamaPoke.Utils.BinSpriteReader.LoadPokedexThumbnail(targetPath);
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