using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    // 시작할 때 게임에 필요한 리소스(Data 파일, 포켓몬 이미지)가 갖춰져 있는지 확인합니다.
    public static class ResourceCheck
    {
        public enum SpriteState { Ok, Partial, None }

        // 일부 이미지는 변환에 실패할 수 있어, 기대 개수의 90% 이상이면 정상으로 봅니다.
        private const double SPRITE_OK_RATIO = 0.9;

        // 읽지 못한 Data 파일 이름 목록
        public static List<string> FindBrokenDataFiles()
        {
            var broken = new List<string>();
            if (!PokemonDex.IsLoaded) broken.Add("pokemon_data.json");
            if (!SkillDex.IsMoveDbLoaded) broken.Add("pokemon_movedb.json");
            if (!SkillDex.IsLearnsetLoaded) broken.Add("pokemon_skills.json");
            return broken;
        }

        // 기본 이미지(pNNNN.bin)가 몇 종이나 있는지 셉니다. 원본 저장소에 이미지가 없는 번호(missing_sprites.json)는 제외합니다.
        public static SpriteState GetSpriteState(out int present, out int expected)
        {
            var noSprite = new HashSet<int>();
            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(File.ReadAllText(AppPaths.DataPath("missing_sprites.json")));
                if (ids != null) noSprite.UnionWith(ids);
            }
            catch (Exception) { /* 목록이 없으면 전체를 기준으로 셉니다. */ }

            present = 0; expected = 0;
            for (int id = 1; id <= GameConstants.MAX_POKEMON_ID; id++)
            {
                if (noSprite.Contains(id)) continue;
                expected++;
                if (File.Exists(AppPaths.SpritePath($"p{id:D4}.bin"))) present++;
            }

            if (present == 0) return SpriteState.None;
            return present < expected * SPRITE_OK_RATIO ? SpriteState.Partial : SpriteState.Ok;
        }
    }
}
