using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        private bool _isInventoryOpen;
        public bool IsInventoryOpen
        {
            get => _isInventoryOpen;
            set { _isInventoryOpen = value; OnPropertyChanged(nameof(IsInventoryOpen)); }
        }

        [JsonInclude]
        public ObservableCollection<ItemInfo> Inventory { get; set; } = new ObservableCollection<ItemInfo>();

        public void InitializeInventory()
        {
            if (Inventory == null) Inventory = new ObservableCollection<ItemInfo>();
            Inventory.Clear();
            Inventory.Add(new ItemInfo { Name = "몬스터볼", Description = "야생 포켓몬 포획", Type = ItemType.monsterball, EffectValue = 0, Quantity = 5 });

            // 🌟 퍼센트에 맞게 EffectValue 수치와 설명을 완벽하게 변경했습니다.
            Inventory.Add(new ItemInfo { Name = "상처약", Description = "체력의 15% 회복", Type = ItemType.Potion, EffectValue = 15, Quantity = 1 });
            Inventory.Add(new ItemInfo { Name = "좋은상처약", Description = "체력의 30% 회복", Type = ItemType.Potion, EffectValue = 30, Quantity = 1 });
            Inventory.Add(new ItemInfo { Name = "고급상처약", Description = "체력의 50% 회복", Type = ItemType.Potion, EffectValue = 50, Quantity = 1 });
        }

        public void AddItemToInventory(string name, string description, ItemType type, int effectValue, int amount)
        {
            var existingItem = Inventory.FirstOrDefault(i => i.Name == name);

            if (existingItem != null)
            {
                existingItem.Quantity += amount;
            }
            else
            {
                Inventory.Add(new ItemInfo
                {
                    Name = name,
                    Description = description,
                    Type = type,
                    EffectValue = effectValue,
                    Quantity = amount
                });
            }
            Save();
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