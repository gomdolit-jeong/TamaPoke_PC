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
        private const int TOXIC_MOVE_ID = 92;

        // 🌟 세대별 스킬 트리 딕셔너리
        public static Dictionary<int, Dictionary<string, List<LearnMove>>> PokemonLearnsets { get; private set; } = new();

        // 기술 습득표 / 기술 데이터를 정상적으로 읽었는지 (파일이 없거나 깨졌으면 false)
        public static bool IsLearnsetLoaded => PokemonLearnsets.Count > 0;
        public static bool IsMoveDbLoaded => MoveDatabase.Count > 0;

        public static void LoadSkillData()
        {
            string dataFolder = AppPaths.DataFolder;

            // 파일마다 따로 읽어서, 하나가 깨져도 다른 하나는 정상으로 불러옵니다.
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"기술 습득표 로드 에러: {ex.Message}");
            }

            try
            {
                string moveDbPath = Path.Combine(dataFolder, "pokemon_movedb.json");
                if (File.Exists(moveDbPath))
                {
                    string dbJson = File.ReadAllText(moveDbPath);
                    var moveData = JsonSerializer.Deserialize<List<SkillInfo>>(dbJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (moveData != null)
                    {
                        foreach (var move in moveData)
                        {
                            // 맹독(Toxic, 92번)은 데이터상 일반 독으로 들어오므로 맹독으로 보정합니다.
                            if (move.Id == TOXIC_MOVE_ID && move.Ailment == SkillAilment.Poison) move.Ailment = SkillAilment.BadPoison;
                            MoveDatabase[move.Id] = move;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"기술 데이터 로드 에러: {ex.Message}");
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

        // 타입별 기본 공격기 (자력기가 없거나 자기 타입 기술이 없을 때 채워 넣는 용도). 데이터에 없으면 몸통박치기
        private static readonly Dictionary<PokemonType, int> FallbackMoves = new()
        {
            [PokemonType.Normal] = 33,    // 몸통박치기
            [PokemonType.Fire] = 52,      // 불꽃세례
            [PokemonType.Water] = 55,     // 물대포
            [PokemonType.Electric] = 84,  // 전기쇼크
            [PokemonType.Grass] = 22,     // 덩굴채찍
            [PokemonType.Ice] = 181,      // 눈싸라기
            [PokemonType.Fighting] = 2,   // 태권당수
            [PokemonType.Poison] = 40,    // 독침
            [PokemonType.Ground] = 189,   // 진흙뿌리기
            [PokemonType.Flying] = 16,    // 바람일으키기
            [PokemonType.Psychic] = 93,   // 염동력
            [PokemonType.Bug] = 522,      // 벌레의저항
            [PokemonType.Rock] = 88,      // 돌떨구기
            [PokemonType.Ghost] = 122,    // 핥기
            [PokemonType.Dragon] = 225,   // 용의숨결
            [PokemonType.Dark] = 44,      // 물기
            [PokemonType.Steel] = 232,    // 메탈크로우
            [PokemonType.Fairy] = 584,    // 요정의바람
        };

        public static int GetFallbackMove(PokemonType type) =>
            FallbackMoves.TryGetValue(type, out int id) && MoveDatabase.ContainsKey(id) ? id : 33;
    }
}