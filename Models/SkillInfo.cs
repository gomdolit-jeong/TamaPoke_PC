using System;

namespace TamaPoke.Models
{
    public enum SkillCategory { Physical = 0, Special = 1, Status = 2 }

    // 원본 C++의 EF_* 상수 이식
    public enum SkillEffect { None = 0, Stage, Recoil, Drain, FixedLvl, Fixed, Priority, NeverMiss, Multi, Heal, Recharge, Charge }

    // 원본 C++의 AIL_* 상수 이식
    public enum SkillAilment { None = 0, Para, Burn, Poison, Sleep, Freeze, Confuse }

    public class SkillInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PokemonType Type { get; set; }
        public SkillCategory Category { get; set; }
        public int Power { get; set; }
        public int Accuracy { get; set; }
        public SkillEffect Effect { get; set; } = SkillEffect.None;
        public int EffectParam { get; set; } = 0;
        public int Target { get; set; } // 🌟 누락되었던 타겟 속성 추가
        public SkillAilment Ailment { get; set; } = SkillAilment.None;
        public int AilmentChance { get; set; } = 0;
    }
}