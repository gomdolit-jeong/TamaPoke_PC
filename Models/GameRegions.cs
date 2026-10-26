using System.Linq;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 세대(지방)별 도감 번호 범위
    public record RegionInfo(int Generation, int StartId, int EndId)
    {
        public string Name => Loc.T($"region.{Generation}");   // 관동/Kanto ...
        public int Count => EndId - StartId + 1;
        public bool Contains(int speciesId) => speciesId >= StartId && speciesId <= EndId;
    }

    public static class GameRegions
    {
        public static readonly RegionInfo[] All =
        {
            new(1, 1, 151),
            new(2, 152, 251),
            new(3, 252, 386),
            new(4, 387, 493),
            new(5, 494, 649),
            new(6, 650, 721),
            new(7, 722, 809),
            new(8, 810, 905),
            new(9, 906, GameConstants.MAX_POKEMON_ID),
        };

        public static RegionInfo? ByGeneration(int generation) => All.FirstOrDefault(r => r.Generation == generation);
    }
}
