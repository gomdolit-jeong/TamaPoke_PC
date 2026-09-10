using System.Linq;

namespace TamaPoke.Models
{
    // 🌟 1. 체육관 관장 정보 구조체 (원작 고증 데이터 포함)
    public class GymLeaderInfo
    {
        public string? GymName { get; set; }
        public string? LeaderName { get; set; }
        public string? BadgeName { get; set; }
        public int PokemonSpeciesId { get; set; }
        public int Level { get; set; }
        public int[]? SpecificSkills { get; set; }
    }

    // 🌟 2. 64명의 전체 체육관 데이터를 관리하는 매니저
    public static class GymManager
    {
        public static readonly GymLeaderInfo[] GymLeaders = new GymLeaderInfo[]
        {
             // =====================================
             // === 1세대 관동지방 (Kanto) ===
             // =====================================
             new GymLeaderInfo { GymName = "회색 체육관", LeaderName = "웅이", BadgeName = "회색배지", PokemonSpeciesId = 95, Level = 14, SpecificSkills = new int[] { 60, 44, 1, 0 } },
             new GymLeaderInfo { GymName = "블루 체육관", LeaderName = "이슬", BadgeName = "블루배지", PokemonSpeciesId = 121, Level = 21, SpecificSkills = new int[] { 19, 32, 86, 0 } },
             new GymLeaderInfo { GymName = "갈색 체육관", LeaderName = "마티스", BadgeName = "오렌지배지", PokemonSpeciesId = 26, Level = 24, SpecificSkills = new int[] { 24, 23, 5, 0 } },
             new GymLeaderInfo { GymName = "무지개 체육관", LeaderName = "민화", BadgeName = "무지개배지", PokemonSpeciesId = 45, Level = 29, SpecificSkills = new int[] { 29, 41, 28, 0 } },
             new GymLeaderInfo { GymName = "연분홍 체육관", LeaderName = "독수", BadgeName = "핑크배지", PokemonSpeciesId = 110, Level = 43, SpecificSkills = new int[] { 41, 64, 8, 0 } },
             new GymLeaderInfo { GymName = "노랑 체육관", LeaderName = "초련", BadgeName = "골드배지", PokemonSpeciesId = 65, Level = 43, SpecificSkills = new int[] { 51, 64, 86, 0 } },
             new GymLeaderInfo { GymName = "홍련 체육관", LeaderName = "강연", BadgeName = "진홍배지", PokemonSpeciesId = 59, Level = 47, SpecificSkills = new int[] { 14, 8, 68, 0 } },
             new GymLeaderInfo { GymName = "상록 체육관", LeaderName = "비주기", BadgeName = "그린배지", PokemonSpeciesId = 112, Level = 50, SpecificSkills = new int[] { 44, 60, 55, 9 } },

             // =====================================
             // === 2세대 성도지방 (Johto) ===
             // =====================================
             new GymLeaderInfo { GymName = "도라지 체육관", LeaderName = "비상", BadgeName = "윙배지", PokemonSpeciesId = 18, Level = 13, SpecificSkills = new int[] { 47, 45, 5, 0 } },
             new GymLeaderInfo { GymName = "고동 체육관", LeaderName = "호일", BadgeName = "인세트배지", PokemonSpeciesId = 123, Level = 15, SpecificSkills = new int[] { 57, 45, 75, 0 } },
             new GymLeaderInfo { GymName = "금빛 체육관", LeaderName = "꼭두", BadgeName = "레귤러배지", PokemonSpeciesId = 241, Level = 19, SpecificSkills = new int[] { 7, 86, 68, 0 } },
             new GymLeaderInfo { GymName = "인주 체육관", LeaderName = "유빈", BadgeName = "팬텀배지", PokemonSpeciesId = 94, Level = 25, SpecificSkills = new int[] { 64, 41, 89, 0 } },
             new GymLeaderInfo { GymName = "진청 체육관", LeaderName = "사도", BadgeName = "쇼크배지", PokemonSpeciesId = 62, Level = 31, SpecificSkills = new int[] { 37, 18, 44, 0 } },
             new GymLeaderInfo { GymName = "담청 체육관", LeaderName = "규리", BadgeName = "스틸배지", PokemonSpeciesId = 208, Level = 35, SpecificSkills = new int[] { 70, 44, 69, 0 } },
             new GymLeaderInfo { GymName = "황토 체육관", LeaderName = "류옹", BadgeName = "아이스배지", PokemonSpeciesId = 221, Level = 43, SpecificSkills = new int[] { 33, 44, 7, 0 } },
             new GymLeaderInfo { GymName = "검은먹 체육관", LeaderName = "이향", BadgeName = "라이징배지", PokemonSpeciesId = 230, Level = 41, SpecificSkills = new int[] { 67, 19, 32, 80 } },

             // =====================================
             // === 3세대 호연지방 (Hoenn) ===
             // =====================================
             new GymLeaderInfo { GymName = "금탄 체육관", LeaderName = "원규", BadgeName = "스톤배지", PokemonSpeciesId = 299, Level = 15, SpecificSkills = new int[] { 60, 44, 8, 9 } },
             new GymLeaderInfo { GymName = "무로 체육관", LeaderName = "철구", BadgeName = "너클배지", PokemonSpeciesId = 297, Level = 19, SpecificSkills = new int[] { 44, 8, 5, 0 } },
             new GymLeaderInfo { GymName = "보라 체육관", LeaderName = "암전", BadgeName = "다이나모배지", PokemonSpeciesId = 310, Level = 23, SpecificSkills = new int[] { 24, 23, 5, 0 } },
             new GymLeaderInfo { GymName = "용암 체육관", LeaderName = "민지", BadgeName = "히트배지", PokemonSpeciesId = 324, Level = 28, SpecificSkills = new int[] { 14, 44, 8, 0 } },
             new GymLeaderInfo { GymName = "등화 체육관", LeaderName = "종길", BadgeName = "밸런스배지", PokemonSpeciesId = 289, Level = 31, SpecificSkills = new int[] { 44, 64, 8, 9 } },
             new GymLeaderInfo { GymName = "검방울 체육관", LeaderName = "은송", BadgeName = "깃털배지", PokemonSpeciesId = 334, Level = 33, SpecificSkills = new int[] { 47, 45, 44, 0 } },
             new GymLeaderInfo { GymName = "이끼 체육관", LeaderName = "풍&란", BadgeName = "마인드배지", PokemonSpeciesId = 338, Level = 42, SpecificSkills = new int[] { 51, 60, 64, 86 } },
             new GymLeaderInfo { GymName = "루네 체육관", LeaderName = "아단", BadgeName = "레인배지", PokemonSpeciesId = 350, Level = 46, SpecificSkills = new int[] { 19, 32, 86, 9 } },

             // =====================================
             // === 4세대 신오지방 (Sinnoh) ===
             // =====================================
             new GymLeaderInfo { GymName = "무쇠 체육관", LeaderName = "강석", BadgeName = "콜배지", PokemonSpeciesId = 409, Level = 14, SpecificSkills = new int[] { 60, 44, 8, 9 } },
             new GymLeaderInfo { GymName = "영원 체육관", LeaderName = "유채", BadgeName = "포레스트배지", PokemonSpeciesId = 407, Level = 22, SpecificSkills = new int[] { 29, 41, 86, 0 } },
             new GymLeaderInfo { GymName = "연고 체육관", LeaderName = "멜리사", BadgeName = "레릭배지", PokemonSpeciesId = 429, Level = 26, SpecificSkills = new int[] { 64, 51, 23, 0 } },
             new GymLeaderInfo { GymName = "장막 체육관", LeaderName = "자망", BadgeName = "코블배지", PokemonSpeciesId = 448, Level = 30, SpecificSkills = new int[] { 44, 64, 5, 9 } },
             new GymLeaderInfo { GymName = "들초 체육관", LeaderName = "맥실러", BadgeName = "펜배지", PokemonSpeciesId = 419, Level = 36, SpecificSkills = new int[] { 19, 32, 68, 5 } },
             new GymLeaderInfo { GymName = "운하 체육관", LeaderName = "동관", BadgeName = "마인배지", PokemonSpeciesId = 411, Level = 39, SpecificSkills = new int[] { 60, 44, 8, 86 } },
             new GymLeaderInfo { GymName = "선단 체육관", LeaderName = "무청", BadgeName = "글레이셔배지", PokemonSpeciesId = 460, Level = 42, SpecificSkills = new int[] { 32, 44, 29, 0 } },
             new GymLeaderInfo { GymName = "물가 체육관", LeaderName = "전진", BadgeName = "비컨배지", PokemonSpeciesId = 466, Level = 49, SpecificSkills = new int[] { 24, 23, 44, 9 } },

             // =====================================
             // === 5세대 하나지방 (Unova) ===
             // =====================================
             new GymLeaderInfo { GymName = "성신 체육관", LeaderName = "덴트/팟/콘", BadgeName = "트라이배지", PokemonSpeciesId = 512, Level = 14, SpecificSkills = new int[] { 1, 2, 3, 0 } },
             new GymLeaderInfo { GymName = "칠보 체육관", LeaderName = "알로에", BadgeName = "베이직배지", PokemonSpeciesId = 505, Level = 20, SpecificSkills = new int[] { 4, 5, 6, 0 } },
             new GymLeaderInfo { GymName = "구름 체육관", LeaderName = "아티", BadgeName = "비틀배지", PokemonSpeciesId = 542, Level = 23, SpecificSkills = new int[] { 7, 8, 9, 0 } },
             new GymLeaderInfo { GymName = "뇌문 체육관", LeaderName = "카밀레", BadgeName = "볼트배지", PokemonSpeciesId = 523, Level = 27, SpecificSkills = new int[] { 10, 11, 12, 0 } },
             new GymLeaderInfo { GymName = "물풍경 체육관", LeaderName = "야콘", BadgeName = "퀘이크배지", PokemonSpeciesId = 530, Level = 31, SpecificSkills = new int[] { 13, 14, 15, 0 } },
             new GymLeaderInfo { GymName = "궐수 체육관", LeaderName = "풍란", BadgeName = "제트배지", PokemonSpeciesId = 581, Level = 35, SpecificSkills = new int[] { 16, 17, 18, 0 } },
             new GymLeaderInfo { GymName = "설화 체육관", LeaderName = "담아", BadgeName = "아이스클배지", PokemonSpeciesId = 614, Level = 39, SpecificSkills = new int[] { 19, 20, 21, 0 } },
             new GymLeaderInfo { GymName = "쌍용 체육관", LeaderName = "사간/아이리스", BadgeName = "레전드배지", PokemonSpeciesId = 612, Level = 43, SpecificSkills = new int[] { 22, 23, 24, 0 } },

             // =====================================
             // === 6세대 칼로스지방 (Kalos) ===
             // =====================================
             new GymLeaderInfo { GymName = "백단 체육관", LeaderName = "비오라", BadgeName = "버그배지", PokemonSpeciesId = 666, Level = 12, SpecificSkills = new int[] { 1, 2, 3, 0 } },
             new GymLeaderInfo { GymName = "삼채 체육관", LeaderName = "자크로", BadgeName = "월배지", PokemonSpeciesId = 697, Level = 25, SpecificSkills = new int[] { 4, 5, 6, 0 } },
             new GymLeaderInfo { GymName = "사라 체육관", LeaderName = "코르니", BadgeName = "파이트배지", PokemonSpeciesId = 701, Level = 32, SpecificSkills = new int[] { 7, 8, 9, 0 } },
             new GymLeaderInfo { GymName = "비익 체육관", LeaderName = "후쿠지", BadgeName = "플랜트배지", PokemonSpeciesId = 673, Level = 34, SpecificSkills = new int[] { 10, 11, 12, 0 } },
             new GymLeaderInfo { GymName = "미르 체육관", LeaderName = "시트론", BadgeName = "볼티지배지", PokemonSpeciesId = 695, Level = 37, SpecificSkills = new int[] { 13, 14, 15, 0 } },
             new GymLeaderInfo { GymName = "후늬 체육관", LeaderName = "마슈", BadgeName = "페어리배지", PokemonSpeciesId = 700, Level = 42, SpecificSkills = new int[] { 16, 17, 18, 0 } },
             new GymLeaderInfo { GymName = "향전 체육관", LeaderName = "고지카", BadgeName = "사이킥배지", PokemonSpeciesId = 678, Level = 48, SpecificSkills = new int[] { 19, 20, 21, 0 } },
             new GymLeaderInfo { GymName = "이설 체육관", LeaderName = "우루프", BadgeName = "아이스버그배지", PokemonSpeciesId = 713, Level = 59, SpecificSkills = new int[] { 22, 23, 24, 0 } },

             // =====================================
             // === 8세대 가라르지방 (Galar) ===
             // =====================================
             new GymLeaderInfo { GymName = "터프 체육관", LeaderName = "아킬", BadgeName = "풀배지", PokemonSpeciesId = 830, Level = 20, SpecificSkills = new int[] { 1, 2, 3, 0 } },
             new GymLeaderInfo { GymName = "바우 체육관", LeaderName = "야청", BadgeName = "물배지", PokemonSpeciesId = 834, Level = 24, SpecificSkills = new int[] { 4, 5, 6, 0 } },
             new GymLeaderInfo { GymName = "엔진 체육관", LeaderName = "순무", BadgeName = "불꽃배지", PokemonSpeciesId = 851, Level = 27, SpecificSkills = new int[] { 7, 8, 9, 0 } },
             new GymLeaderInfo { GymName = "래터럴 체육관", LeaderName = "채두/어니언", BadgeName = "격투/고스트배지", PokemonSpeciesId = 865, Level = 36, SpecificSkills = new int[] { 10, 11, 12, 0 } },
             new GymLeaderInfo { GymName = "아라베스크 체육관", LeaderName = "포플러", BadgeName = "페어리배지", PokemonSpeciesId = 869, Level = 38, SpecificSkills = new int[] { 13, 14, 15, 0 } },
             new GymLeaderInfo { GymName = "키르쿠스 체육관", LeaderName = "마쿠와/멜론", BadgeName = "바위/얼음배지", PokemonSpeciesId = 875, Level = 42, SpecificSkills = new int[] { 16, 17, 18, 0 } },
             new GymLeaderInfo { GymName = "스파이크 체육관", LeaderName = "두송", BadgeName = "악배지", PokemonSpeciesId = 861, Level = 46, SpecificSkills = new int[] { 19, 20, 21, 0 } },
             new GymLeaderInfo { GymName = "너클 체육관", LeaderName = "금랑", BadgeName = "드래곤배지", PokemonSpeciesId = 884, Level = 48, SpecificSkills = new int[] { 22, 23, 24, 0 } },

             // =====================================
             // === 9세대 팔데아지방 (Paldea) ===
             // =====================================
             new GymLeaderInfo { GymName = "세르클 체육관", LeaderName = "단풍", BadgeName = "벌레배지", PokemonSpeciesId = 918, Level = 15, SpecificSkills = new int[] { 1, 2, 3, 0 } },
             new GymLeaderInfo { GymName = "보울 체육관", LeaderName = "콜사", BadgeName = "풀배지", PokemonSpeciesId = 930, Level = 17, SpecificSkills = new int[] { 4, 5, 6, 0 } },
             new GymLeaderInfo { GymName = "누룩스 체육관", LeaderName = "모야모", BadgeName = "전기배지", PokemonSpeciesId = 939, Level = 24, SpecificSkills = new int[] { 7, 8, 9, 0 } },
             new GymLeaderInfo { GymName = "카라프 체육관", LeaderName = "해대", BadgeName = "물배지", PokemonSpeciesId = 962, Level = 30, SpecificSkills = new int[] { 10, 11, 12, 0 } },
             new GymLeaderInfo { GymName = "참푸르 체육관", LeaderName = "청목", BadgeName = "노말배지", PokemonSpeciesId = 982, Level = 36, SpecificSkills = new int[] { 13, 14, 15, 0 } },
             new GymLeaderInfo { GymName = "프리지 체육관", LeaderName = "라임", BadgeName = "고스트배지", PokemonSpeciesId = 974, Level = 42, SpecificSkills = new int[] { 16, 17, 18, 0 } },
             new GymLeaderInfo { GymName = "베이크 체육관", LeaderName = "리플", BadgeName = "에스퍼배지", PokemonSpeciesId = 956, Level = 45, SpecificSkills = new int[] { 19, 20, 21, 0 } },
             new GymLeaderInfo { GymName = "나페산 체육관", LeaderName = "그루샤", BadgeName = "얼음배지", PokemonSpeciesId = 975, Level = 48, SpecificSkills = new int[] { 22, 23, 24, 0 } }
        };

        // 🌟 인덱스(0~63)를 주면 관장 정보를 반환하는 메서드
        public static GymLeaderInfo? GetLeader(int globalIndex)
        {
            if (globalIndex >= 0 && globalIndex < GymLeaders.Length)
            {
                return GymLeaders[globalIndex];
            }
            return null;
        }
    }
}