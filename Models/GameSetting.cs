using System.Collections.Generic;

namespace TamaPoke.Models
{
    public class GameSettings
    {
        // 시스템 트레이 알림 사용 유무
        public bool UseTrayNotifications { get; set; } = true;

        // 🌟 선택된 세대 목록 (기본값: 1~9세대 모두 포함)
        public List<int> SelectedGenerations { get; set; } = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    }
}