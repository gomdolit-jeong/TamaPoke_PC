using System;
using System.Collections.Generic;
using System.IO; // 🌟 오류 해결의 핵심! 파일 경로 도구를 사용하겠다고 선언합니다.

namespace TamaPoke.Models
{
    // 🌟 배지 1개의 정보를 담는 클래스
    public class GymBadgeInfo
    {
        public string RegionName { get; set; } = string.Empty;
        public int BadgeNumber { get; set; }

        // 버튼을 눌렀을 때 몇 번째 배지인지(1~72) 식별하기 위한 고유 번호
        public int GlobalIndex { get; set; }

        // "Kanto_1.png" 형태로 경로를 자동 생성합니다!
        public string ImagePath => Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "Badges", $"{RegionName}_{BadgeNumber}.png");
    }

    // 🌟 8개의 배지를 묶어서 "1세대 관동지방" 같은 헤더를 달아주는 그룹 클래스
    public class BadgeRegionGroup
    {
        public string HeaderText { get; set; } = string.Empty;
        public List<GymBadgeInfo> Badges { get; set; } = new List<GymBadgeInfo>();
    }
}