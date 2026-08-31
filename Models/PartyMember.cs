using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    public class PartyMember
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
                // 객체가 null이거나 2번째 타입이 없는 경우를 완벽하고 안전하게 걸러냅니다.
                if (pokemon == null || pokemon.Type2 == PokemonType.None) return "";
                return pokemon.Type2.ToString().ToUpper();
            }
        }

        [JsonIgnore]
        public System.Windows.Media.ImageSource? SpriteImage
        {
            get
            {
                // 🌟 메인 화면과 동일하게 Assets/Resource/PokemonSprites 폴더에서 .bin 파일을 찾습니다[cite: 10].
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string normalPath = System.IO.Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"p{SpeciesId:D3}.bin");
                string shinyPath = System.IO.Path.Combine(baseDir, "Assets", "Resource", "PokemonSprites", $"ps{SpeciesId:D3}.bin");

                string targetPath = (IsShiny && System.IO.File.Exists(shinyPath)) ? shinyPath : normalPath;

                if (System.IO.File.Exists(targetPath))
                {
                    // 🌟 Tpk2Decoder를 이용해 0번 애니메이션(대기 상태)의 프레임들을 가져옵니다[cite: 10].
                    var frames = TamaPoke.Utils.Service.Tpk2Decoder.LoadAnimation(targetPath, 0);
                    if (frames != null && frames.Length > 0)
                    {
                        return frames[0]; // 첫 번째 프레임 이미지만 반환하여 정지된 이미지로 띄웁니다.
                    }
                }
                return null;
            }
        }
    }
}