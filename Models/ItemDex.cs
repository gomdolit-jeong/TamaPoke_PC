using System.Collections.Generic;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 아이템 한 종류의 고정 정의. 이름/설명은 현재 언어로 Lang 파일에서 읽습니다. (item.{Id}.name / .desc)
    // LegacyKoreanName: 이전 버전 세이브(아이템을 한국어 이름으로 저장)를 변환할 때만 사용
    public record ItemDefinition(string Id, string LegacyKoreanName, ItemType Type, int EffectValue, string Icon)
    {
        public string Name => Loc.T($"item.{Id}.name");
        public string Description => Loc.T($"item.{Id}.desc");
    }

    // 🌟 아이템 정의 테이블: 새 아이템은 여기에 한 줄 추가하면 됩니다.
    // 키(ItemId)는 세이브 파일에 저장되므로 한 번 정한 뒤에는 바꾸지 마세요. (표시 이름은 자유롭게 변경 가능)
    public static class ItemDex
    {
        public const string MonsterBall = "monsterball";
        public const string Potion = "potion";
        public const string SuperPotion = "super_potion";
        public const string HyperPotion = "hyper_potion";

        private static readonly Dictionary<string, ItemDefinition> _items = new()
        {
            [MonsterBall] = new(MonsterBall, "몬스터볼", ItemType.monsterball, 0, "🔴"),
            [Potion] = new(Potion, "상처약", ItemType.Potion, 15, "🧪"),
            [SuperPotion] = new(SuperPotion, "좋은상처약", ItemType.Potion, 30, "💊"),
            [HyperPotion] = new(HyperPotion, "고급상처약", ItemType.Potion, 50, "💉"),
        };

        private static readonly ItemDefinition Unknown = new("unknown", "", ItemType.Berry, 0, "📦");

        public static ItemDefinition Get(string itemId) =>
            itemId != null && _items.TryGetValue(itemId, out var def) ? def : Unknown;

        public static bool Exists(string itemId) => itemId != null && _items.ContainsKey(itemId);

        // 이전 버전 세이브(아이템 이름으로 저장)를 ItemId로 변환합니다.
        public static string? FindIdByName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var pair in _items)
            {
                if (pair.Value.LegacyKoreanName == name) return pair.Key;
            }
            return null;
        }
    }
}
