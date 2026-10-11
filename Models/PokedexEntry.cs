using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows.Media;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public class PokedexEntry : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string SpeciesName { get; set; } = string.Empty;
        private bool _isUnlocked;
        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                if (_isUnlocked != value)
                {
                    _isUnlocked = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsUnlocked)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpriteImage)));
                }
            }
        }
        public string DisplayName => IsUnlocked ? SpeciesName : "???";

        [JsonIgnore]
        public ImageSource? SpriteImage
        {
            get
            {
                string spriteFolder = AppPaths.SpriteFolder;

                if (Directory.Exists(spriteFolder))
                {
                    string pattern = $"p{Id:D4}*.bin";
                    var matchedFiles = Directory.GetFiles(spriteFolder, pattern);

                    if (matchedFiles.Length > 0)
                    {
                        return BinSpriteReader.LoadPokedexThumbnail(matchedFiles[0]);
                    }
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
