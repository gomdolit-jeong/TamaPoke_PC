using System;
using System.Linq;

namespace TamaPoke.Models
{
    public static class SkillDex
    {
        // 🌟 원본 moves.h의 90개 기술 데이터를 완벽하게 C#으로 이식
        public static readonly SkillInfo[] MoveTable = new SkillInfo[]
        {
            new SkillInfo { Id = 0, Name = "-", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0 },
            new SkillInfo { Id = 1, Name = "몸통박치기", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 40, Accuracy = 100 },
            new SkillInfo { Id = 2, Name = "할퀴기", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 40, Accuracy = 100 },
            new SkillInfo { Id = 3, Name = "막치기", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 40, Accuracy = 100 },
            new SkillInfo { Id = 4, Name = "마구찌르기", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 15, Accuracy = 85, Effect = SkillEffect.Multi },
            new SkillInfo { Id = 5, Name = "전광석화", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 40, Accuracy = 100, Effect = SkillEffect.Priority },
            new SkillInfo { Id = 6, Name = "스피드스타", Type = PokemonType.Normal, Category = SkillCategory.Special, Power = 60, Accuracy = 0, Effect = SkillEffect.NeverMiss },
            new SkillInfo { Id = 7, Name = "누르기", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 85, Accuracy = 100 },
            new SkillInfo { Id = 8, Name = "이판사판태클", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 120, Accuracy = 100, Effect = SkillEffect.Recoil },
            new SkillInfo { Id = 9, Name = "파괴광선", Type = PokemonType.Normal, Category = SkillCategory.Special, Power = 150, Accuracy = 90, Effect = SkillEffect.Recharge },
            new SkillInfo { Id = 10, Name = "쪼기", Type = PokemonType.Flying, Category = SkillCategory.Physical, Power = 35, Accuracy = 100 },
            new SkillInfo { Id = 11, Name = "불꽃세례", Type = PokemonType.Fire, Category = SkillCategory.Special, Power = 40, Accuracy = 100, Ailment = SkillAilment.Burn, AilmentChance = 10 },
            new SkillInfo { Id = 12, Name = "불꽃펀치", Type = PokemonType.Fire, Category = SkillCategory.Physical, Power = 75, Accuracy = 100, Ailment = SkillAilment.Burn, AilmentChance = 10 },
            new SkillInfo { Id = 13, Name = "화염방사", Type = PokemonType.Fire, Category = SkillCategory.Special, Power = 90, Accuracy = 100, Ailment = SkillAilment.Burn, AilmentChance = 10 },
            new SkillInfo { Id = 14, Name = "불대문자", Type = PokemonType.Fire, Category = SkillCategory.Special, Power = 110, Accuracy = 85, Ailment = SkillAilment.Burn, AilmentChance = 10 },
            new SkillInfo { Id = 15, Name = "거품", Type = PokemonType.Water, Category = SkillCategory.Special, Power = 30, Accuracy = 100 },
            new SkillInfo { Id = 16, Name = "물대포", Type = PokemonType.Water, Category = SkillCategory.Special, Power = 40, Accuracy = 100 },
            new SkillInfo { Id = 17, Name = "폭포오르기", Type = PokemonType.Water, Category = SkillCategory.Physical, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 18, Name = "파도타기", Type = PokemonType.Water, Category = SkillCategory.Special, Power = 90, Accuracy = 100 },
            new SkillInfo { Id = 19, Name = "하이드로펌프", Type = PokemonType.Water, Category = SkillCategory.Special, Power = 110, Accuracy = 80 },
            new SkillInfo { Id = 20, Name = "스파크", Type = PokemonType.Electric, Category = SkillCategory.Physical, Power = 35, Accuracy = 100, Ailment = SkillAilment.Para, AilmentChance = 20 },
            new SkillInfo { Id = 21, Name = "전기쇼크", Type = PokemonType.Electric, Category = SkillCategory.Special, Power = 40, Accuracy = 100, Ailment = SkillAilment.Para, AilmentChance = 10 },
            new SkillInfo { Id = 22, Name = "번개펀치", Type = PokemonType.Electric, Category = SkillCategory.Physical, Power = 75, Accuracy = 100, Ailment = SkillAilment.Para, AilmentChance = 10 },
            new SkillInfo { Id = 23, Name = "10만볼트", Type = PokemonType.Electric, Category = SkillCategory.Special, Power = 90, Accuracy = 100, Ailment = SkillAilment.Para, AilmentChance = 10 },
            new SkillInfo { Id = 24, Name = "번개", Type = PokemonType.Electric, Category = SkillCategory.Special, Power = 110, Accuracy = 70, Ailment = SkillAilment.Para, AilmentChance = 30 },
            new SkillInfo { Id = 25, Name = "흡수", Type = PokemonType.Grass, Category = SkillCategory.Special, Power = 20, Accuracy = 100, Effect = SkillEffect.Drain },
            new SkillInfo { Id = 26, Name = "덩굴채찍", Type = PokemonType.Grass, Category = SkillCategory.Physical, Power = 45, Accuracy = 100 },
            new SkillInfo { Id = 27, Name = "잎날가르기", Type = PokemonType.Grass, Category = SkillCategory.Physical, Power = 55, Accuracy = 95 },
            new SkillInfo { Id = 28, Name = "메가드레인", Type = PokemonType.Grass, Category = SkillCategory.Special, Power = 40, Accuracy = 100, Effect = SkillEffect.Drain },
            new SkillInfo { Id = 29, Name = "솔라빔", Type = PokemonType.Grass, Category = SkillCategory.Special, Power = 120, Accuracy = 100, Effect = SkillEffect.Charge },
            new SkillInfo { Id = 30, Name = "오로라빔", Type = PokemonType.Ice, Category = SkillCategory.Special, Power = 65, Accuracy = 100 },
            new SkillInfo { Id = 31, Name = "냉동펀치", Type = PokemonType.Ice, Category = SkillCategory.Physical, Power = 75, Accuracy = 100, Ailment = SkillAilment.Freeze, AilmentChance = 10 },
            new SkillInfo { Id = 32, Name = "냉동빔", Type = PokemonType.Ice, Category = SkillCategory.Special, Power = 90, Accuracy = 100, Ailment = SkillAilment.Freeze, AilmentChance = 10 },
            new SkillInfo { Id = 33, Name = "눈보라", Type = PokemonType.Ice, Category = SkillCategory.Special, Power = 110, Accuracy = 70, Ailment = SkillAilment.Freeze, AilmentChance = 10 },
            new SkillInfo { Id = 34, Name = "태권당수", Type = PokemonType.Fighting, Category = SkillCategory.Physical, Power = 50, Accuracy = 100 },
            new SkillInfo { Id = 35, Name = "지구던지기", Type = PokemonType.Fighting, Category = SkillCategory.Physical, Power = 0, Accuracy = 100, Effect = SkillEffect.FixedLvl },
            new SkillInfo { Id = 36, Name = "지옥의바퀴", Type = PokemonType.Fighting, Category = SkillCategory.Physical, Power = 80, Accuracy = 80, Effect = SkillEffect.Recoil },
            new SkillInfo { Id = 37, Name = "무릎차기", Type = PokemonType.Fighting, Category = SkillCategory.Physical, Power = 100, Accuracy = 90 },
            new SkillInfo { Id = 38, Name = "독침", Type = PokemonType.Poison, Category = SkillCategory.Physical, Power = 15, Accuracy = 100, Ailment = SkillAilment.Poison, AilmentChance = 30 },
            new SkillInfo { Id = 39, Name = "용해액", Type = PokemonType.Poison, Category = SkillCategory.Special, Power = 40, Accuracy = 100, Ailment = SkillAilment.Poison, AilmentChance = 10 },
            new SkillInfo { Id = 40, Name = "오물", Type = PokemonType.Poison, Category = SkillCategory.Special, Power = 65, Accuracy = 100, Ailment = SkillAilment.Poison, AilmentChance = 30 },
            new SkillInfo { Id = 41, Name = "오물폭탄", Type = PokemonType.Poison, Category = SkillCategory.Special, Power = 90, Accuracy = 100, Ailment = SkillAilment.Poison, AilmentChance = 30 },
            new SkillInfo { Id = 42, Name = "뼈다귀치기", Type = PokemonType.Ground, Category = SkillCategory.Physical, Power = 65, Accuracy = 85 },
            new SkillInfo { Id = 43, Name = "구멍파기", Type = PokemonType.Ground, Category = SkillCategory.Physical, Power = 80, Accuracy = 100, Effect = SkillEffect.Charge },
            new SkillInfo { Id = 44, Name = "지진", Type = PokemonType.Ground, Category = SkillCategory.Physical, Power = 100, Accuracy = 100 },
            new SkillInfo { Id = 45, Name = "날개치기", Type = PokemonType.Flying, Category = SkillCategory.Physical, Power = 60, Accuracy = 100 },
            new SkillInfo { Id = 46, Name = "회전부리", Type = PokemonType.Flying, Category = SkillCategory.Physical, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 47, Name = "공중날기", Type = PokemonType.Flying, Category = SkillCategory.Physical, Power = 90, Accuracy = 95, Effect = SkillEffect.Charge },
            new SkillInfo { Id = 48, Name = "사이코웨이브", Type = PokemonType.Psychic, Category = SkillCategory.Special, Power = 30, Accuracy = 100 },
            new SkillInfo { Id = 49, Name = "염동력", Type = PokemonType.Psychic, Category = SkillCategory.Special, Power = 50, Accuracy = 100, Ailment = SkillAilment.Confuse, AilmentChance = 10 },
            new SkillInfo { Id = 50, Name = "환상빔", Type = PokemonType.Psychic, Category = SkillCategory.Special, Power = 65, Accuracy = 100, Ailment = SkillAilment.Confuse, AilmentChance = 10 },
            new SkillInfo { Id = 51, Name = "사이코키네시스", Type = PokemonType.Psychic, Category = SkillCategory.Special, Power = 90, Accuracy = 100 },
            new SkillInfo { Id = 52, Name = "벌레먹기", Type = PokemonType.Bug, Category = SkillCategory.Physical, Power = 30, Accuracy = 100 },
            new SkillInfo { Id = 53, Name = "바늘미사일", Type = PokemonType.Bug, Category = SkillCategory.Physical, Power = 25, Accuracy = 95, Effect = SkillEffect.Multi },
            new SkillInfo { Id = 54, Name = "흡혈", Type = PokemonType.Bug, Category = SkillCategory.Physical, Power = 80, Accuracy = 100, Effect = SkillEffect.Drain },
            new SkillInfo { Id = 55, Name = "메가혼", Type = PokemonType.Bug, Category = SkillCategory.Physical, Power = 120, Accuracy = 85 },
            new SkillInfo { Id = 56, Name = "벌레의야단법석", Type = PokemonType.Bug, Category = SkillCategory.Special, Power = 90, Accuracy = 100 },
            new SkillInfo { Id = 57, Name = "시저크로스", Type = PokemonType.Bug, Category = SkillCategory.Physical, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 58, Name = "바위깨기", Type = PokemonType.Fighting, Category = SkillCategory.Physical, Power = 40, Accuracy = 100 },
            new SkillInfo { Id = 59, Name = "돌떨구기", Type = PokemonType.Rock, Category = SkillCategory.Physical, Power = 50, Accuracy = 90 },
            new SkillInfo { Id = 60, Name = "스톤샤워", Type = PokemonType.Rock, Category = SkillCategory.Physical, Power = 75, Accuracy = 90 },
            new SkillInfo { Id = 61, Name = "원시의힘", Type = PokemonType.Rock, Category = SkillCategory.Special, Power = 60, Accuracy = 100 },
            new SkillInfo { Id = 62, Name = "핥기", Type = PokemonType.Ghost, Category = SkillCategory.Physical, Power = 30, Accuracy = 100, Ailment = SkillAilment.Para, AilmentChance = 30 },
            new SkillInfo { Id = 63, Name = "나이트헤드", Type = PokemonType.Ghost, Category = SkillCategory.Special, Power = 0, Accuracy = 100, Effect = SkillEffect.FixedLvl },
            new SkillInfo { Id = 64, Name = "섀도볼", Type = PokemonType.Ghost, Category = SkillCategory.Special, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 65, Name = "용의분노", Type = PokemonType.Dragon, Category = SkillCategory.Special, Power = 0, Accuracy = 100, Effect = SkillEffect.Fixed },
            new SkillInfo { Id = 66, Name = "드래곤크루", Type = PokemonType.Dragon, Category = SkillCategory.Physical, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 67, Name = "역린", Type = PokemonType.Dragon, Category = SkillCategory.Physical, Power = 120, Accuracy = 100 },
            new SkillInfo { Id = 68, Name = "물기", Type = PokemonType.Dark, Category = SkillCategory.Physical, Power = 60, Accuracy = 100 },
            new SkillInfo { Id = 69, Name = "깨물어부수기", Type = PokemonType.Dark, Category = SkillCategory.Physical, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 70, Name = "아이언헤드", Type = PokemonType.Steel, Category = SkillCategory.Physical, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 71, Name = "러스터캐논", Type = PokemonType.Steel, Category = SkillCategory.Special, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 72, Name = "매지컬샤인", Type = PokemonType.Fairy, Category = SkillCategory.Special, Power = 80, Accuracy = 100 },
            new SkillInfo { Id = 73, Name = "치근거리기", Type = PokemonType.Fairy, Category = SkillCategory.Physical, Power = 90, Accuracy = 90 },
            new SkillInfo { Id = 74, Name = "문포스", Type = PokemonType.Fairy, Category = SkillCategory.Special, Power = 95, Accuracy = 100 },
            new SkillInfo { Id = 75, Name = "칼춤", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 76, Name = "고속이동", Type = PokemonType.Psychic, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 77, Name = "배리어", Type = PokemonType.Psychic, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 78, Name = "망각술", Type = PokemonType.Psychic, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 79, Name = "나쁜음모", Type = PokemonType.Dark, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 80, Name = "용의춤", Type = PokemonType.Dragon, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 81, Name = "벌크업", Type = PokemonType.Fighting, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 82, Name = "울음소리", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 83, Name = "째려보기", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 84, Name = "싫은소리", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 85, Name = "실뿜기", Type = PokemonType.Bug, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Stage },
            new SkillInfo { Id = 86, Name = "HP회복", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Heal },
            new SkillInfo { Id = 87, Name = "알낳기", Type = PokemonType.Normal, Category = SkillCategory.Status, Power = 0, Accuracy = 0, Effect = SkillEffect.Heal },
            new SkillInfo { Id = 88, Name = "발버둥", Type = PokemonType.Normal, Category = SkillCategory.Physical, Power = 50, Accuracy = 0, Effect = SkillEffect.Recoil },
            new SkillInfo { Id = 89, Name = "악의파동", Type = PokemonType.Dark, Category = SkillCategory.Special, Power = 80, Accuracy = 100 }
        };

        public static readonly ushort[] LearnOffset = new ushort[] { 0, 0, 13, 26, 41 }; // 임시 1~4번 데이터
        public static readonly (byte Move, byte Level)[] LearnTable = new (byte, byte)[] { (1, 1) }; // 임시

        public static SkillInfo? GetSkill(int id) => id >= 0 && id < MoveTable.Length ? MoveTable[id] : null;

        public static int GetLearnCount(int dex)
        {
            if (dex < 1 || dex > 493) return 0;
            if (dex + 1 >= LearnOffset.Length) return 0;
            return LearnOffset[dex + 1] - LearnOffset[dex];
        }

        public static byte GetLearnMove(int dex, int index)
        {
            if (index >= GetLearnCount(dex)) return 88;
            return LearnTable[LearnOffset[dex] + index].Move;
        }

        public static byte GetLearnLevel(int dex, int index)
        {
            if (index >= GetLearnCount(dex)) return 0;
            return LearnTable[LearnOffset[dex] + index].Level;
        }

        // 🌟 안전장치: 방대한 레벨업 데이터가 채워지지 않았을 때 자동으로 속성에 맞는 기술을 반환합니다.
        public static int GetFallbackMove(PokemonType type)
        {
            return type switch
            {
                PokemonType.Normal => 1, // 몸통박치기
                PokemonType.Fire => 11, // 불꽃세례
                PokemonType.Water => 16, // 물대포
                PokemonType.Electric => 21, // 전기쇼크
                PokemonType.Grass => 26, // 덩굴채찍
                PokemonType.Ice => 30, // 오로라빔
                PokemonType.Fighting => 34, // 태권당수
                PokemonType.Poison => 38, // 독침
                PokemonType.Ground => 42, // 뼈다귀치기
                PokemonType.Flying => 45, // 날개치기
                PokemonType.Psychic => 49, // 염동력
                PokemonType.Bug => 52, // 벌레먹기
                PokemonType.Rock => 59, // 돌떨구기
                PokemonType.Ghost => 62, // 핥기
                PokemonType.Dragon => 65, // 용의분노
                PokemonType.Dark => 68, // 물기
                PokemonType.Steel => 70, // 아이언헤드
                PokemonType.Fairy => 72, // 매지컬샤인
                _ => 1
            };
        }
    }
}