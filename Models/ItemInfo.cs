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

    public class ItemInfo : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; }

        // 아이템의 성능 (기력 회복량)
        public int EffectValue { get; set; }

        // 🌟 아이템 종류와 수치에 따라 색상과 모양이 다른 이모지를 스마트하게 반환합니다.
        public string Icon
        {
            get
            {
                if (Type == ItemType.monsterball)
                    return "🔴";

                if (Type == ItemType.Potion)
                {
                    if (EffectValue == 20) return "🧪"; // 일반 상처약 (초록색 물약)
                    if (EffectValue == 50) return "💊"; // 좋은 상처약 (알약)
                    if (EffectValue >= 100) return "💉"; // 고급 상처약 (주사기)

                    return "🩹"; // 기타 치료제 기본값
                }

                return "📦"; // 기본 아이템 박스
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}