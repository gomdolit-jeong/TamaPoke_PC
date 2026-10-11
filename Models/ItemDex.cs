using System.Collections.Generic;

namespace TamaPoke.Models
{
    // 아이템 한 종류의 고정 정의 (모든 플레이어에게 동일, 세이브에 저장하지 않음)
    public record ItemDefinition(string Name, string Description, ItemType Type, int EffectValue, string Icon);

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
            [MonsterBall] = new("몬스터볼", "야생 포켓몬을 잡을 때 쓴다.", ItemType.monsterball, 0, "🔴"),
            [Potion] = new("상처약", "포켓몬의 체력을 15% 회복한다.", ItemType.Potion, 15, "🧪"),
            [SuperPotion] = new("좋은상처약", "포켓몬의 체력을 30% 회복한다.", ItemType.Potion, 30, "💊"),
            [HyperPotion] = new("고급상처약", "포켓몬의 체력을 50% 회복한다.", ItemType.Potion, 50, "💉"),
        };

        private static readonly ItemDefinition Unknown = new("알 수 없는 아이템", "", ItemType.Berry, 0, "📦");

        public static ItemDefinition Get(string itemId) =>
            itemId != null && _items.TryGetValue(itemId, out var def) ? def : Unknown;

        public static bool Exists(string itemId) => itemId != null && _items.ContainsKey(itemId);

        // 이전 버전 세이브(아이템 이름으로 저장)를 ItemId로 변환합니다.
        public static string? FindIdByName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var pair in _items)
            {
                if (pair.Value.Name == name) return pair.Key;
            }
            return null;
        }
    }
}
