using System;

namespace TamaPoke.Models
{
    // 🌟 파티에 보관될 포켓몬의 상태를 저장하는 데이터 클래스입니다.
    public class PartyMember
    {
        public int SpeciesId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool IsShiny { get; set; }

        // 🌟 PokemonState 본체에 정의된 훈련 스탯 변수명(TrAtk, TrDef, TrSpeed)과 정확히 일치시킵니다.
        public int TrAtk { get; set; }
        public int TrDef { get; set; }
        public int TrSpeed { get; set; }

        // 보유하고 있던 4개의 스킬 배열
        public int[] Skills { get; set; } = new int[4];

        // 🌟 유전자(개체값) 정보 연결
        public PokemonGene Genes { get; set; } = new PokemonGene();
    }
}