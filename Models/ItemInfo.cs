using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TamaPoke.Models
{
    // 아이템의 종류를 구분하는 열거형
    public enum ItemType
    {
        monsterball,   // 몬스터볼 (포획용)
        Potion,        // 상처약 (회복용)
        Berry          // 나무열매 (기분/포만감 용)
    }

    // 가방 한 칸: '어떤 아이템(ItemId)을 몇 개(Quantity)' 가졌는지만 저장합니다.
    // 이름/설명/효과/아이콘은 ItemDex 테이블에서 읽어 옵니다.
    public class ItemInfo : INotifyPropertyChanged
    {
        private string _itemId = string.Empty;
        public string ItemId
        {
            get => _itemId;
            set
            {
                if (_itemId != value)
                {
                    _itemId = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Description));
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }

        private ItemDefinition Definition => ItemDex.Get(ItemId);

        [JsonIgnore] public string Name => Definition.Name;
        [JsonIgnore] public string Description => Definition.Description;
        [JsonIgnore] public ItemType Type => Definition.Type;
        [JsonIgnore] public int EffectValue => Definition.EffectValue; // 상처약: 최대 체력 대비 회복 %
        [JsonIgnore] public string Icon => Definition.Icon;

        // 이전 버전 세이브에 저장된 아이템 이름입니다. 불러온 직후 ItemId로 변환되고, 다시 저장되지 않습니다.
        [JsonInclude, JsonPropertyName("Name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LegacyName { get; set; }

        // 언어가 바뀌었을 때 이름/설명 바인딩을 다시 읽게 합니다.
        public void RefreshTexts() => OnPropertyChanged(string.Empty);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
