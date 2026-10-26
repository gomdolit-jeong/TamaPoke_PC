using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 가방의 메가스톤 목록 한 줄 (완성한 스톤 / 모으는 중인 조각)
    public record MegaStoneRow(string Name, string Progress, bool IsComplete);

    public partial class PokemonState
    {
        #region 메가스톤 (Mega Stone)

        public const int MEGA_SHARDS_PER_STONE = 10;  // 조각 10개 = 메가스톤 1개
        public const int MEGA_RING_BADGES = 8;        // 체육관 배지 8개(관동 체육관 전부)를 모으면 메가링 획득
        private const int WILD_SHARD_CHANCE = 10;     // 야생 배틀 보상으로 조각이 나올 확률(%)
        private const int GYM_SHARDS = 3;             // 체육관을 처음 이겼을 때 받는 조각 수
        private const int GYM_REMATCH_SHARDS = 1;     // 이미 깬 체육관에 재도전해서 이겼을 때
        private const int WALK_SHARD_CHANCE = 30;     // 산책 중 찾은 아이템이 조각일 확률(%)

        // 어떤 스톤의 조각이 나올지: 대표 포켓몬 50% → 나머지 파티 30% → 아무 스톤 20%
        // (해당하는 스톤이 없으면 다음 범위에서 고릅니다)
        private const int LEAD_SHARD_CHANCE = 50;
        private const int PARTY_SHARD_CHANCE = 30;

        private bool _hasMegaRing;
        public bool HasMegaRing
        {
            get => _hasMegaRing;
            set { if (SetProperty(ref _hasMegaRing, value)) RaiseMegaStoneChanged(); }
        }

        // 모으는 중인 조각(스톤 ID → 개수)과 완성한 메가스톤 ID
        public Dictionary<string, int> MegaShards { get; set; } = new();
        public List<string> MegaStones { get; set; } = new();

        public bool HasMegaStone(string stoneId) => MegaStones.Contains(stoneId);

        // 이 포켓몬이 지금 메가진화할 수 있는 폼: 메가링 + 메가스톤(레쿠쟈는 불필요) + 이미지
        public List<MegaFormInfo> GetUsableMegaForms(int speciesId) =>
            !HasMegaRing ? new List<MegaFormInfo>()
                         : MegaDex.GetAvailableForms(speciesId).Where(f => string.IsNullOrEmpty(f.StoneId) || HasMegaStone(f.StoneId)).ToList();

        // 체육관 배지가 기준에 도달했으면 메가링을 줍니다. 새로 받았으면 true
        private bool TryGrantMegaRing()
        {
            if (HasMegaRing || GymBadges < MEGA_RING_BADGES) return false;
            HasMegaRing = true;
            Save();
            return true;
        }

        // 메가스톤 조각을 줍니다. 대표 포켓몬 → 파티 포켓몬의 스톤이 우선 나오고, 다 모이면 스톤이 완성됩니다.
        // 받은 내용을 메시지로 돌려줍니다. (메가링이 없거나 모을 스톤이 없으면 null)
        public string? GiveMegaShards(int amount, Random rand)
        {
            if (!HasMegaRing || amount <= 0) return null;

            var candidates = MegaDex.CollectibleStones().Where(s => !HasMegaStone(s.StoneId)).ToList();
            if (candidates.Count == 0) return null;

            var leadStones = candidates.Where(s => s.SpeciesId == SpeciesId).ToList();
            var partyIds = (Party ?? new()).Select(m => m.SpeciesId).ToHashSet();
            var partyStones = candidates.Where(s => s.SpeciesId != SpeciesId && partyIds.Contains(s.SpeciesId)).ToList();

            int roll = rand.Next(100);
            var pool = roll < LEAD_SHARD_CHANCE && leadStones.Count > 0 ? leadStones
                     : roll < LEAD_SHARD_CHANCE + PARTY_SHARD_CHANCE && partyStones.Count > 0 ? partyStones
                     : candidates;
            var stone = pool[rand.Next(pool.Count)];

            int count = Math.Min(MEGA_SHARDS_PER_STONE, MegaShards.GetValueOrDefault(stone.StoneId) + amount);
            string message;
            if (count >= MEGA_SHARDS_PER_STONE)
            {
                MegaShards.Remove(stone.StoneId);
                MegaStones.Add(stone.StoneId);
                message = Loc.F("mega.stone.complete", stone.StoneName);
            }
            else
            {
                MegaShards[stone.StoneId] = count;
                message = Loc.F("mega.shard.got", stone.StoneName, count, MEGA_SHARDS_PER_STONE);
            }

            RaiseMegaStoneChanged();
            Save();
            return message;
        }

        // 가방 표시: 메가링 상태와 스톤 목록 (완성한 스톤 → 모으는 중인 조각 순)
        [JsonIgnore]
        public string MegaRingText => HasMegaRing
            ? Loc.F("bag.mega.ring.has", MegaStones.Count)
            : Loc.F("bag.mega.ring.hint", MEGA_RING_BADGES);

        [JsonIgnore]
        public List<MegaStoneRow> MegaStoneRows =>
            MegaStones.Select(id => new MegaStoneRow(MegaDex.FindStone(id)?.StoneName ?? id, Loc.T("bag.mega.complete"), true))
                .Concat(MegaShards.OrderByDescending(p => p.Value)
                    .Select(p => new MegaStoneRow(MegaDex.FindStone(p.Key)?.StoneName ?? p.Key, $"{p.Value}/{MEGA_SHARDS_PER_STONE}", false)))
                .ToList();

        private void RaiseMegaStoneChanged()
        {
            OnPropertyChanged(nameof(MegaRingText));
            OnPropertyChanged(nameof(MegaStoneRows));
        }

        #endregion
    }
}
