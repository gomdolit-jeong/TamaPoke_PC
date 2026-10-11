using System.Linq;

namespace TamaPoke.Models
{
    // 세대(지방)별 도감 번호 범위
    public record RegionInfo(int Generation, string Name, int StartId, int EndId)
    {
        public int Count => EndId - StartId + 1;
        public bool Contains(int speciesId) => speciesId >= StartId && speciesId <= EndId;
    }

    public static class GameRegions
    {
        public static readonly RegionInfo[] All =
        {
            new(1, "관동", 1, 151),
            new(2, "성도", 152, 251),
            new(3, "호연", 252, 386),
            new(4, "신오", 387, 493),
            new(5, "하나", 494, 649),
            new(6, "칼로스", 650, 721),
            new(7, "알로라", 722, 809),
            new(8, "가라르", 810, 905),
            new(9, "팔데아", 906, GameConstants.MAX_POKEMON_ID),
        };

        public static RegionInfo? ByGeneration(int generation) => All.FirstOrDefault(r => r.Generation == generation);
    }
}
