using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    public class LearnMove
    {
        public int Level { get; set; }
        public int MoveId { get; set; }
    }

    public class PokemonLearnset
    {
        public int SpeciesId { get; set; }
        // 🌟 세대별 스킬 트리를 담는 딕셔너리
        public Dictionary<string, List<LearnMove>> MovesByVersion { get; set; } = new();
    }

    public static class SkillDex
    {
        public static Dictionary<int, SkillInfo> MoveDatabase { get; private set; } = new();

        // 🌟 세대별 스킬 트리 딕셔너리
        public static Dictionary<int, Dictionary<string, List<LearnMove>>> PokemonLearnsets { get; private set; } = new();

        public static void LoadSkillData()
        {
            try
            {
                string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

                string learnsetPath = Path.Combine(dataFolder, "pokemon_skills.json");
                if (File.Exists(learnsetPath))
                {
                    string jsonString = File.ReadAllText(learnsetPath);
                    var learnsetData = JsonSerializer.Deserialize<List<PokemonLearnset>>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (learnsetData != null)
                    {
                        foreach (var data in learnsetData)
                        {
                            PokemonLearnsets[data.SpeciesId] = data.MovesByVersion;
                        }
                    }
                }

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

        // 🌟 [핵심] 포켓몬에게 가장 알맞은 최신 세대 스킬트리 찾아주기
        public static List<LearnMove> GetActiveLearnset(int speciesId)
        {
            if (!PokemonLearnsets.ContainsKey(speciesId)) return new List<LearnMove>();

            var versions = PokemonLearnsets[speciesId];
            string[] priority = { "scarlet-violet", "sword-shield", "brilliant-diamond-and-shining-pearl", "ultra-sun-ultra-moon", "sun-moon" };

            foreach (var version in priority)
            {
                if (versions.ContainsKey(version)) return versions[version];
            }

            return versions.Values.FirstOrDefault() ?? new List<LearnMove>();
        }

        public static SkillInfo? GetSkill(int skillId)
        {
            if (MoveDatabase.TryGetValue(skillId, out var skill)) return skill;
            return null;
        }

        // ==========================================
        // 🌟 기존 PokemonState.Skills.cs 와의 호환성을 위한 헬퍼 메서드
        // ==========================================
        public static int GetLearnCount(int speciesId) => GetActiveLearnset(speciesId).Count;

        public static int GetLearnLevel(int speciesId, int index)
        {
            var moves = GetActiveLearnset(speciesId);
            if (index >= 0 && index < moves.Count) return moves[index].Level;
            return 0;
        }

        public static int GetLearnMove(int speciesId, int index)
        {
            var moves = GetActiveLearnset(speciesId);
            if (index >= 0 && index < moves.Count) return moves[index].MoveId;
            return 0;
        }

        public static int GetFallbackMove(PokemonType type)
        {
            return 33; // 기본 땜빵 스킬 (몸통박치기)
        }
    }
}