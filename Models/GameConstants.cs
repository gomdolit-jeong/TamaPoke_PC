namespace TamaPoke.Models
{
    public static class GameConstants
    {
        // 🌟 나중에 10세대, 11세대 포켓몬이 추가되면 오직 이 숫자 하나만 늘려주시면 됩니다!
        public const int MAX_POKEMON_ID = 1025;
        public const int MAX_EVOLUTION_CHAIN_ID = 550;

        // ===== 확률 조절 포인트 (이로치 / 전설) =====
        // 부화 이로치: 1/N. 이전 포켓몬과 '이별'로 헤어졌으면 더 잘 나오고, 돌봄 보너스(연속 출석, 유대감)만큼 N이 줄어듭니다.
        public const int SHINY_ODDS_AFTER_FAREWELL = 24;
        public const int SHINY_ODDS_DEFAULT = 48;
        public const int SHINY_ODDS_MIN = 8;              // 보너스가 커도 1/8보다 좋아지지 않음
        public const int WILD_SHINY_CHANCE_PERCENT = 1;   // 야생 이로치 확률(%)

        // 전설: 설정에서 고른 세대의 전설만 나옵니다.
        public const int LEGENDARY_HATCH_MIN_DEX = 30;          // 도감 등록 수가 이 이상이면
        public const int LEGENDARY_HATCH_CHANCE_PERCENT = 3;    // 부화 시 전설 확률(%)
        public const int WILD_LEGENDARY_CHANCE_PERCENT = 1;     // 야생 전설 확률(%)
    }
}