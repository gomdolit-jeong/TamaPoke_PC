using System.Linq;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 🌟 1. 체육관 관장 정보 구조체 (원작 고증 데이터 포함)
    public class GymLeaderInfo
    {
        // 순서 번호(0~63). 이름은 현재 언어로 Lang 파일에서 읽습니다. (gym.{Index}.name / .leader / .badge)
        public int Index { get; set; }
        public string GymName => Loc.T($"gym.{Index}.name");
        public string LeaderName => Loc.T($"gym.{Index}.leader");
        public string BadgeName => Loc.T($"gym.{Index}.badge");
        public int PokemonSpeciesId { get; set; }
        public int Level { get; set; }  // 기술은 이 레벨까지 배우는 자력기 중에서 자동으로 고릅니다. (야생과 같은 규칙)
    }

    // 🌟 2. 64명의 전체 체육관 데이터를 관리하는 매니저
    public static class GymManager
    {
        public static readonly GymLeaderInfo[] GymLeaders = new GymLeaderInfo[]
        {
             // =====================================
             // === 1세대 관동지방 (Kanto) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 95, Level = 14 },  // 0: 웅이 / Brock
             new GymLeaderInfo { PokemonSpeciesId = 121, Level = 21 },  // 1: 이슬 / Misty
             new GymLeaderInfo { PokemonSpeciesId = 26, Level = 24 },  // 2: 마티스 / Lt. Surge
             new GymLeaderInfo { PokemonSpeciesId = 45, Level = 29 },  // 3: 민화 / Erika
             new GymLeaderInfo { PokemonSpeciesId = 110, Level = 43 },  // 4: 독수 / Koga
             new GymLeaderInfo { PokemonSpeciesId = 65, Level = 43 },  // 5: 초련 / Sabrina
             new GymLeaderInfo { PokemonSpeciesId = 59, Level = 47 },  // 6: 강연 / Blaine
             new GymLeaderInfo { PokemonSpeciesId = 112, Level = 50 },  // 7: 비주기 / Giovanni

             // =====================================
             // === 2세대 성도지방 (Johto) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 18, Level = 13 },  // 8: 비상 / Falkner
             new GymLeaderInfo { PokemonSpeciesId = 123, Level = 15 },  // 9: 호일 / Bugsy
             new GymLeaderInfo { PokemonSpeciesId = 241, Level = 19 },  // 10: 꼭두 / Whitney
             new GymLeaderInfo { PokemonSpeciesId = 94, Level = 25 },  // 11: 유빈 / Morty
             new GymLeaderInfo { PokemonSpeciesId = 62, Level = 31 },  // 12: 사도 / Chuck
             new GymLeaderInfo { PokemonSpeciesId = 208, Level = 35 },  // 13: 규리 / Jasmine
             new GymLeaderInfo { PokemonSpeciesId = 221, Level = 43 },  // 14: 류옹 / Pryce
             new GymLeaderInfo { PokemonSpeciesId = 230, Level = 41 },  // 15: 이향 / Clair

             // =====================================
             // === 3세대 호연지방 (Hoenn) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 299, Level = 15 },  // 16: 원규 / Roxanne
             new GymLeaderInfo { PokemonSpeciesId = 297, Level = 19 },  // 17: 철구 / Brawly
             new GymLeaderInfo { PokemonSpeciesId = 310, Level = 23 },  // 18: 암전 / Wattson
             new GymLeaderInfo { PokemonSpeciesId = 324, Level = 28 },  // 19: 민지 / Flannery
             new GymLeaderInfo { PokemonSpeciesId = 289, Level = 31 },  // 20: 종길 / Norman
             new GymLeaderInfo { PokemonSpeciesId = 334, Level = 33 },  // 21: 은송 / Winona
             new GymLeaderInfo { PokemonSpeciesId = 338, Level = 42 },  // 22: 풍&란 / Tate & Liza
             new GymLeaderInfo { PokemonSpeciesId = 350, Level = 46 },  // 23: 아단 / Juan

             // =====================================
             // === 4세대 신오지방 (Sinnoh, DP 순서) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 409, Level = 14 },  // 24: 강석 / Roark
             new GymLeaderInfo { PokemonSpeciesId = 407, Level = 22 },  // 25: 유채 / Gardenia
             new GymLeaderInfo { PokemonSpeciesId = 448, Level = 26 },  // 26: 자망 / Maylene
             new GymLeaderInfo { PokemonSpeciesId = 419, Level = 30 },  // 27: 맥실러 / Crasher Wake
             new GymLeaderInfo { PokemonSpeciesId = 429, Level = 36 },  // 28: 멜리사 / Fantina
             new GymLeaderInfo { PokemonSpeciesId = 411, Level = 39 },  // 29: 동관 / Byron
             new GymLeaderInfo { PokemonSpeciesId = 460, Level = 42 },  // 30: 무청 / Candice
             new GymLeaderInfo { PokemonSpeciesId = 466, Level = 49 },  // 31: 전진 / Volkner

             // =====================================
             // === 5세대 하나지방 (Unova) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 512, Level = 14 },  // 32: 덴트/팟/콘 / Cilan/Chili/Cress
             new GymLeaderInfo { PokemonSpeciesId = 505, Level = 20 },  // 33: 알로에 / Lenora
             new GymLeaderInfo { PokemonSpeciesId = 542, Level = 23 },  // 34: 아티 / Burgh
             new GymLeaderInfo { PokemonSpeciesId = 587, Level = 27 },  // 35: 카밀레 / Elesa
             new GymLeaderInfo { PokemonSpeciesId = 530, Level = 31 },  // 36: 야콘 / Clay
             new GymLeaderInfo { PokemonSpeciesId = 581, Level = 35 },  // 37: 풍란 / Skyla
             new GymLeaderInfo { PokemonSpeciesId = 614, Level = 39 },  // 38: 담아 / Brycen
             new GymLeaderInfo { PokemonSpeciesId = 612, Level = 43 },  // 39: 사간/아이리스 / Drayden/Iris

             // =====================================
             // === 6세대 칼로스지방 (Kalos) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 666, Level = 12 },  // 40: 비오라 / Viola
             new GymLeaderInfo { PokemonSpeciesId = 697, Level = 25 },  // 41: 자크로 / Grant
             new GymLeaderInfo { PokemonSpeciesId = 701, Level = 32 },  // 42: 코르니 / Korrina
             new GymLeaderInfo { PokemonSpeciesId = 673, Level = 34 },  // 43: 후쿠지 / Ramos
             new GymLeaderInfo { PokemonSpeciesId = 695, Level = 37 },  // 44: 시트론 / Clemont
             new GymLeaderInfo { PokemonSpeciesId = 700, Level = 42 },  // 45: 마슈 / Valerie
             new GymLeaderInfo { PokemonSpeciesId = 678, Level = 48 },  // 46: 고지카 / Olympia
             new GymLeaderInfo { PokemonSpeciesId = 713, Level = 59 },  // 47: 우루프 / Wulfric

             // =====================================
             // === 8세대 가라르지방 (Galar) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 830, Level = 20 },  // 48: 아킬 / Milo
             new GymLeaderInfo { PokemonSpeciesId = 834, Level = 24 },  // 49: 야청 / Nessa
             new GymLeaderInfo { PokemonSpeciesId = 851, Level = 27 },  // 50: 순무 / Kabu
             new GymLeaderInfo { PokemonSpeciesId = 865, Level = 36 },  // 51: 채두/어니언 / Bea/Allister
             new GymLeaderInfo { PokemonSpeciesId = 869, Level = 38 },  // 52: 포플러 / Opal
             new GymLeaderInfo { PokemonSpeciesId = 875, Level = 42 },  // 53: 마쿠와/멜론 / Gordie/Melony
             new GymLeaderInfo { PokemonSpeciesId = 861, Level = 46 },  // 54: 두송 / Piers
             new GymLeaderInfo { PokemonSpeciesId = 884, Level = 48 },  // 55: 금랑 / Raihan

             // =====================================
             // === 9세대 팔데아지방 (Paldea) ===
             // =====================================
             new GymLeaderInfo { PokemonSpeciesId = 918, Level = 15 },  // 56: 단풍 / Katy
             new GymLeaderInfo { PokemonSpeciesId = 930, Level = 17 },  // 57: 콜사 / Brassius
             new GymLeaderInfo { PokemonSpeciesId = 939, Level = 24 },  // 58: 모야모 / Iono
             new GymLeaderInfo { PokemonSpeciesId = 740, Level = 30 },  // 59: 해대 / Kofu
             new GymLeaderInfo { PokemonSpeciesId = 982, Level = 36 },  // 60: 청목 / Larry
             new GymLeaderInfo { PokemonSpeciesId = 849, Level = 42 },  // 61: 라임 / Ryme
             new GymLeaderInfo { PokemonSpeciesId = 671, Level = 45 },  // 62: 리플 / Tulip
             new GymLeaderInfo { PokemonSpeciesId = 975, Level = 48 }  // 63: 그루샤 / Grusha
        };

        static GymManager()
        {
            for (int i = 0; i < GymLeaders.Length; i++) GymLeaders[i].Index = i;
        }

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