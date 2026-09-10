using System.Collections.Generic;

namespace TamaPoke.Models
{
    public class GameSettings
    {
        // 시스템 트레이 알림 사용 유무
        public bool UseTrayNotifications { get; set; } = true;

        private int _farewellAgeDays = 3;

        // 🌟 값이 0 이하로 들어오거나 읽힐 때 기본값 3을 보장하도록 안전장치를 더합니다!
        public int FarewellAgeDays
        {
            get => _farewellAgeDays <= 0 ? 3 : _farewellAgeDays;
            set => _farewellAgeDays = value <= 0 ? 3 : value;
        }

        // 🌟 선택된 세대 목록 (기본값: 1~9세대 모두 포함)
        public List<int> SelectedGenerations { get; set; } = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    }
}