using System;
using System.Collections.Generic;

namespace TamaPoke.Models
{
    public class GameSettings
    {
        // 표시 언어 ("ko" / "en"). 비어 있으면 윈도우 언어를 따라갑니다. (Loc.ResolveLanguage)
        public string? Language { get; set; }

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

        private int _roamingPokemonCount = 1;

        // 🌟 바탕화면에 띄울 포켓몬 수 (안전장치 추가!)
        // 값이 무조건 1에서 6 사이가 되도록 Math.Clamp를 사용하여 보장합니다.
        public int RoamingPokemonCount
        {
            get => Math.Clamp(_roamingPokemonCount, 1, 6);
            set => _roamingPokemonCount = Math.Clamp(value, 1, 6);
        }

        public bool IsTaskbarMode { get; set; } = false; // 작업표시줄 모드 기본값은 끄기(false)

        // 산책 포켓몬 크기 배율 (설정 창 슬라이더와 산책 펫이 같은 범위를 씁니다)
        public const double MinPetScale = 0.5;
        public const double MaxPetScale = 5.0;

        private double _roamingPetScale = 2.0;
        public double RoamingPetScale
        {
            get => Math.Clamp(_roamingPetScale, MinPetScale, MaxPetScale);
            set => _roamingPetScale = Math.Clamp(value, MinPetScale, MaxPetScale);
        }

    }
}