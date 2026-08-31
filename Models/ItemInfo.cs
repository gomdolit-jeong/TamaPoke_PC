using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TamaPoke.Models
{
    // 아이템의 종류를 구분하는 열거형
    public enum ItemType
    {
        Pokeball,   // 몬스터볼 (포획용)
        Potion,     // 상처약 (회복용)
        Berry       // 나무열매 (기분/포만감 용)
    }

    public class ItemInfo : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; }

        // 아이템의 성능 (예: 상처약이면 20 회복, 몬스터볼이면 포획률 보정치)
        public int EffectValue { get; set; }

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}