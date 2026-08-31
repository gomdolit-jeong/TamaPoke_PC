using System.Collections.Generic;
// 🌟 핵심 해결책: Models 네임스페이스를 가져와서 단일 PokemonType을 사용하게 합니다!
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    public static class TypeMatchupHelper
    {
        // 🌟 6세대 이후(Gen 6+) 최신 18가지 타입 상성표 (0배, 0.5배, 1배, 2배)
        // 정의되지 않은 상성(예: 불꽃 -> 노말)은 기본값인 1.0배로 처리됩니다.
        private static readonly Dictionary<PokemonType, Dictionary<PokemonType, double>> MatchupChart = new()
        {
            { PokemonType.Normal, new() { { PokemonType.Rock, 0.5 }, { PokemonType.Ghost, 0.0 }, { PokemonType.Steel, 0.5 } } },

            { PokemonType.Fire, new() { { PokemonType.Fire, 0.5 }, { PokemonType.Water, 0.5 }, { PokemonType.Grass, 2.0 }, { PokemonType.Ice, 2.0 }, { PokemonType.Bug, 2.0 }, { PokemonType.Rock, 0.5 }, { PokemonType.Dragon, 0.5 }, { PokemonType.Steel, 2.0 } } },

            { PokemonType.Water, new() { { PokemonType.Fire, 2.0 }, { PokemonType.Water, 0.5 }, { PokemonType.Grass, 0.5 }, { PokemonType.Ground, 2.0 }, { PokemonType.Rock, 2.0 }, { PokemonType.Dragon, 0.5 } } },

            { PokemonType.Electric, new() { { PokemonType.Water, 2.0 }, { PokemonType.Electric, 0.5 }, { PokemonType.Grass, 0.5 }, { PokemonType.Ground, 0.0 }, { PokemonType.Flying, 2.0 }, { PokemonType.Dragon, 0.5 } } },

            { PokemonType.Grass, new() { { PokemonType.Fire, 0.5 }, { PokemonType.Water, 2.0 }, { PokemonType.Grass, 0.5 }, { PokemonType.Poison, 0.5 }, { PokemonType.Ground, 2.0 }, { PokemonType.Flying, 0.5 }, { PokemonType.Bug, 0.5 }, { PokemonType.Rock, 2.0 }, { PokemonType.Dragon, 0.5 }, { PokemonType.Steel, 0.5 } } },

            { PokemonType.Ice, new() { { PokemonType.Fire, 0.5 }, { PokemonType.Water, 0.5 }, { PokemonType.Grass, 2.0 }, { PokemonType.Ice, 0.5 }, { PokemonType.Ground, 2.0 }, { PokemonType.Flying, 2.0 }, { PokemonType.Dragon, 2.0 }, { PokemonType.Steel, 0.5 } } },

            { PokemonType.Fighting, new() { { PokemonType.Normal, 2.0 }, { PokemonType.Ice, 2.0 }, { PokemonType.Poison, 0.5 }, { PokemonType.Flying, 0.5 }, { PokemonType.Psychic, 0.5 }, { PokemonType.Bug, 0.5 }, { PokemonType.Rock, 2.0 }, { PokemonType.Ghost, 0.0 }, { PokemonType.Dark, 2.0 }, { PokemonType.Steel, 2.0 }, { PokemonType.Fairy, 0.5 } } },

            { PokemonType.Poison, new() { { PokemonType.Grass, 2.0 }, { PokemonType.Poison, 0.5 }, { PokemonType.Ground, 0.5 }, { PokemonType.Rock, 0.5 }, { PokemonType.Ghost, 0.5 }, { PokemonType.Steel, 0.0 }, { PokemonType.Fairy, 2.0 } } },

            { PokemonType.Ground, new() { { PokemonType.Fire, 2.0 }, { PokemonType.Electric, 2.0 }, { PokemonType.Grass, 0.5 }, { PokemonType.Poison, 2.0 }, { PokemonType.Flying, 0.0 }, { PokemonType.Bug, 0.5 }, { PokemonType.Rock, 2.0 }, { PokemonType.Steel, 2.0 } } },

            { PokemonType.Flying, new() { { PokemonType.Electric, 0.5 }, { PokemonType.Grass, 2.0 }, { PokemonType.Fighting, 2.0 }, { PokemonType.Bug, 2.0 }, { PokemonType.Rock, 0.5 }, { PokemonType.Steel, 0.5 } } },

            { PokemonType.Psychic, new() { { PokemonType.Fighting, 2.0 }, { PokemonType.Poison, 2.0 }, { PokemonType.Psychic, 0.5 }, { PokemonType.Dark, 0.0 }, { PokemonType.Steel, 0.5 } } },

            { PokemonType.Bug, new() { { PokemonType.Fire, 0.5 }, { PokemonType.Grass, 2.0 }, { PokemonType.Fighting, 0.5 }, { PokemonType.Poison, 0.5 }, { PokemonType.Flying, 0.5 }, { PokemonType.Psychic, 2.0 }, { PokemonType.Ghost, 0.5 }, { PokemonType.Dark, 2.0 }, { PokemonType.Steel, 0.5 }, { PokemonType.Fairy, 0.5 } } },

            { PokemonType.Rock, new() { { PokemonType.Fire, 2.0 }, { PokemonType.Ice, 2.0 }, { PokemonType.Fighting, 0.5 }, { PokemonType.Ground, 0.5 }, { PokemonType.Flying, 2.0 }, { PokemonType.Bug, 2.0 }, { PokemonType.Steel, 0.5 } } },

            { PokemonType.Ghost, new() { { PokemonType.Normal, 0.0 }, { PokemonType.Psychic, 2.0 }, { PokemonType.Ghost, 2.0 }, { PokemonType.Dark, 0.5 } } },

            { PokemonType.Dragon, new() { { PokemonType.Dragon, 2.0 }, { PokemonType.Steel, 0.5 }, { PokemonType.Fairy, 0.0 } } },

            { PokemonType.Dark, new() { { PokemonType.Fighting, 0.5 }, { PokemonType.Psychic, 2.0 }, { PokemonType.Ghost, 2.0 }, { PokemonType.Dark, 0.5 }, { PokemonType.Fairy, 0.5 } } },

            { PokemonType.Steel, new() { { PokemonType.Fire, 0.5 }, { PokemonType.Water, 0.5 }, { PokemonType.Electric, 0.5 }, { PokemonType.Ice, 2.0 }, { PokemonType.Rock, 2.0 }, { PokemonType.Steel, 0.5 }, { PokemonType.Fairy, 2.0 } } },

            { PokemonType.Fairy, new() { { PokemonType.Fire, 0.5 }, { PokemonType.Fighting, 2.0 }, { PokemonType.Poison, 0.5 }, { PokemonType.Dragon, 2.0 }, { PokemonType.Dark, 2.0 }, { PokemonType.Steel, 0.5 } } }
        };

        /// <summary>
        /// 공격 타입과 방어 타입을 비교하여 데미지 배율(Multiplier)을 반환합니다.
        /// </summary>
        public static double GetMultiplier(PokemonType attackType, PokemonType defenseType)
        {
            // 공격 타입이나 방어 타입이 지정되지 않은(None) 경우 1배의 데미지
            if (attackType == PokemonType.None || defenseType == PokemonType.None)
                return 1.0;

            // 상성표에 공격 타입의 데이터가 있고, 그 안에 방어 타입에 대한 배율이 정의되어 있다면 해당 값을 반환
            if (MatchupChart.TryGetValue(attackType, out var defenseDict))
            {
                if (defenseDict.TryGetValue(defenseType, out double multiplier))
                {
                    return multiplier;
                }
            }

            // 상성표에 없는 조합(예: 불꽃 -> 노말)은 기본값인 1.0배(보통 데미지) 반환
            return 1.0;
        }
    }
}