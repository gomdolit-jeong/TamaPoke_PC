using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        private bool _isInventoryOpen;
        [JsonIgnore] public bool IsInventoryOpen
        {
            get => _isInventoryOpen;
            set { _isInventoryOpen = value; OnPropertyChanged(nameof(IsInventoryOpen)); }
        }

        [JsonInclude]
        public ObservableCollection<ItemInfo> Inventory { get; set; } = new ObservableCollection<ItemInfo>();

        // 새 게임 시작 시 지급하는 기본 아이템
        public void InitializeInventory()
        {
            if (Inventory == null) Inventory = new ObservableCollection<ItemInfo>();
            Inventory.Clear();
            Inventory.Add(new ItemInfo { ItemId = ItemDex.MonsterBall, Quantity = 5 });
            Inventory.Add(new ItemInfo { ItemId = ItemDex.Potion, Quantity = 1 });
            Inventory.Add(new ItemInfo { ItemId = ItemDex.SuperPotion, Quantity = 1 });
            Inventory.Add(new ItemInfo { ItemId = ItemDex.HyperPotion, Quantity = 1 });
        }

        // 아이템 지급: AddItem(ItemDex.Potion, 1)
        public void AddItem(string itemId, int amount)
        {
            if (!ItemDex.Exists(itemId) || amount <= 0) return;

            var existingItem = Inventory.FirstOrDefault(i => i.ItemId == itemId);
            if (existingItem != null) existingItem.Quantity += amount;
            else Inventory.Add(new ItemInfo { ItemId = itemId, Quantity = amount });

            OnPropertyChanged(nameof(MonsterBalls));
            OnPropertyChanged(nameof(Potions));
            Save();
        }

        // 이전 버전 세이브(이름/설명/효과를 통째로 저장)를 ItemId 방식으로 변환합니다.
        // 같은 아이템이 여러 칸으로 나뉘어 있으면 하나로 합치고, 알 수 없는 아이템은 버립니다.
        private void MigrateLegacyInventory()
        {
            if (Inventory == null) return;

            var merged = new ObservableCollection<ItemInfo>();
            foreach (var item in Inventory)
            {
                string? id = !string.IsNullOrEmpty(item.ItemId) ? item.ItemId : ItemDex.FindIdByName(item.LegacyName);
                if (id == null || !ItemDex.Exists(id) || item.Quantity <= 0) continue;

                var existing = merged.FirstOrDefault(i => i.ItemId == id);
                if (existing != null) existing.Quantity += item.Quantity;
                else merged.Add(new ItemInfo { ItemId = id, Quantity = item.Quantity });
            }
            Inventory = merged;
        }

        [JsonIgnore]
        public int MonsterBalls
        {
            get => Inventory?.FirstOrDefault(x => x.Type == ItemType.monsterball)?.Quantity ?? 0;
            set
            {
                var item = Inventory?.FirstOrDefault(x => x.Type == ItemType.monsterball);
                if (item != null)
                {
                    item.Quantity = value;
                    if (item.Quantity <= 0) Inventory?.Remove(item);
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public int Potions
        {
            get => Inventory?.Where(x => x.Type == ItemType.Potion).Sum(x => x.Quantity) ?? 0;
        }

        public void ResetIdleMenus()
        {
            IsInventoryOpen = false;
            IsFeedMenuOpen = false;
            IsPlayMenuOpen = false;
        }
    }
}