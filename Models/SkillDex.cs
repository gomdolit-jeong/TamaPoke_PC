using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TamaPoke.Models
{
    public class PokemonLearnset
    {
        public int SpeciesId { get; set; }
        public List<LearnMove> Moves { get; set; } = new();
    }

    public class LearnMove
    {
        public int Level { get; set; }
        public int MoveId { get; set; }
    }

    public static class SkillDex
    {
        // 🌟 하드코딩된 89개 스킬 테이블 삭제! 
        // 대신 JSON에서 900개의 스킬을 읽어와서 저장할 딕셔너리를 만듭니다.
        public static Dictionary<int, SkillInfo> MoveDatabase { get; private set; } = new();
        public static Dictionary<int, List<LearnMove>> PokemonLearnsets { get; private set; } = new();

        public static void LoadSkillData()
        {
            try
            {
                string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

                // 1. 레벨업 학습 데이터 로드
                string learnsetPath = Path.Combine(dataFolder, "pokemon_skills.json");
                if (File.Exists(learnsetPath))
                {
                    string jsonString = File.ReadAllText(learnsetPath);
                    var learnsetData = JsonSerializer.Deserialize<List<PokemonLearnset>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (learnsetData != null)
                    {
                        foreach (var data in learnsetData) PokemonLearnsets[data.SpeciesId] = data.Moves;
                    }
                }

                // 2. 🌟 전체 스킬 도감(데이터베이스) 로드
                string moveDbPath = Path.Combine(dataFolder, "pokemon_movedb.json");
                if (File.Exists(moveDbPath))
                {
                    string dbJson = File.ReadAllText(moveDbPath);
                    var moveData = JsonSerializer.Deserialize<List<SkillInfo>>(dbJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (moveData != null)
                    {
                        foreach (var move in moveData) MoveDatabase[move.Id] = move;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"스킬 데이터 로드 에러: {ex.Message}");
            }
        }

        public static SkillInfo? GetSkill(int id) => MoveDatabase.ContainsKey(id) ? MoveDatabase[id] : null;

        public static int GetLearnCount(int dex)
        {
            if (PokemonLearnsets.ContainsKey(dex)) return PokemonLearnsets[dex].Count;
            return 0;
        }

        public static int GetLearnMove(int dex, int index)
        {
            if (PokemonLearnsets.ContainsKey(dex) && index < PokemonLearnsets[dex].Count)
                return PokemonLearnsets[dex][index].MoveId;
            return 33; // 33번은 PokéAPI 기준으로 '몸통박치기(Tackle)' 입니다.
        }

        public static int GetLearnLevel(int dex, int index)
        {
            if (PokemonLearnsets.ContainsKey(dex) && index < PokemonLearnsets[dex].Count)
                return PokemonLearnsets[dex][index].Level;
            return 0;
        }

        public static int GetFallbackMove(PokemonType type)
        {
            // PokéAPI 공식 ID 기준으로 기본 공격기 반환 (예: 33은 몸통박치기)
            return type switch
            {
                PokemonType.Water => 55,  // 물대포
                PokemonType.Fire => 52,   // 불꽃세례
                PokemonType.Grass => 71,  // 흡수
                PokemonType.Electric => 84, // 전기쇼크
                _ => 33 // 몸통박치기
            };
        }
    }
}