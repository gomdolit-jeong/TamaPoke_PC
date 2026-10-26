using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    // 🌟 partial 키워드를 사용하여 기존 PokemonState 클래스와 연결합니다!
    public partial class PokemonState
    {
        [JsonIgnore] public bool IsFreeRoaming { get; set; } = false;

        // 🌟 방향을 조절할 수 있는 속성 (0:앞, 2:옆, 4:뒤)
        private int _direction = 0;
        [JsonIgnore] public int Direction
        {
            get => _direction;
            set
            {
                if (SetProperty(ref _direction, value))
                {
                    // 방향이 바뀌면 애니메이션 프레임을 초기화하여 새로 자르도록 유도합니다.
                    _animationFrames = null;
                    CheckStateAndAnimate();
                }
            }
        }

        // 🌟 외부(MainWindow)에서 애니메이션을 강제로 실행하는 메서드
        public void SetPlayModeAction(int actionId, int timer = 9999)
        {
            // 기존 파일에 선언되어 있던 비공개 변수들을 자유롭게 사용할 수 있습니다!
            PlayTempAction(actionId, timer);
            CheckStateAndAnimate();
        }

        // 산책용 펫 창이 닫힐 때 애니메이션 타이머를 멈춰 타이머가 계속 쌓이지 않게 합니다.
        public void StopAnimationLoop()
        {
            _mainLoopTimer?.Stop();
            _animationFrames = null;
        }

        #region 산책 모드 (Walk Mode)
        // 산책 중에도 시간은 흐르지만, 돌봐줄 수 없으므로 스탯은 하한선 아래로 떨어지지 않습니다.
        // (응가/가출/케어 실수 없음, 기술은 배틀 승리 후에만 배움)
        private const int WALK_STAT_FLOOR = 30;          // 산책 중 스탯 하한선
        private const int WALK_ITEM_CHANCE_PER_MIN = 5;  // 분당 아이템 발견 확률(%) → 평균 20분에 1개
        private const int WALK_MAX_ITEMS = 5;            // 한 번 산책에서 주울 수 있는 최대 개수
        private const int WALK_MINUTES_PER_AGE = 3;      // 산책 중에는 3분에 1분씩 나이를 먹음 → 레벨업 속도 절반 (3시간에 1레벨)

        // 산책 중 발견 아이템 가중치 (ItemId, 가중치)
        private static readonly (string ItemId, int Weight)[] WalkItemTable =
        {
            (ItemDex.Potion, 50),
            (ItemDex.MonsterBall, 35),
            (ItemDex.SuperPotion, 12),
            (ItemDex.HyperPotion, 3),
        };

        // 산책 중 아이템을 주웠을 때 (MainWindow가 펫 머리 위 말풍선/알림을 띄움)
        public event Action<ItemDefinition>? WalkItemFound;

        private int _walkMinutes = 0;
        private int _walkStartLevel = 0;
        private readonly List<string> _walkFoundItems = new();

        public void BeginWalk()
        {
            IsFreeRoaming = true;
            SyncMainToLeader(); // 산책 펫이 최신 모습으로 나오도록 반영
            _walkMinutes = 0;
            _walkStartLevel = Level;
            _walkFoundItems.Clear();
        }

        public void EndWalk()
        {
            IsFreeRoaming = false;
            ResetPosition();
            Save();

            if (IsEgg || _walkMinutes == 0) return;

            // 메인 화면 문구 영역이 좁으므로 최대 두 줄로 요약합니다.
            string summary = Level > _walkStartLevel
                ? Loc.F("walk.summary.levelup", _walkStartLevel, Level)
                : Loc.F("walk.summary.minutes", _walkMinutes);
            if (_walkFoundItems.Count > 0)
            {
                summary += _walkFoundItems.Count == 1
                    ? Loc.F("walk.summary.item.one", _walkFoundItems[0])
                    : Loc.F("walk.summary.item.many", _walkFoundItems[0], _walkFoundItems.Count - 1);
            }
            _ = ShowEventMessageAsync(summary, 6);
        }

        // 산책 중 1초마다 호출됩니다. (Tick 대신)
        private void WalkTick()
        {
            _ageSeconds++;
            if (DateTime.Now.Date > LastPlayedDate.Date) CheckDailyStreak();

            if (IsEgg || _ageSeconds % 60 != 0) return; // 알은 산책 중에 부화가 진행되지 않습니다.

            _walkMinutes++;

            // 나이(레벨)는 평소의 절반 속도로 증가
            bool ageThisMinute = _walkMinutes % WALK_MINUTES_PER_AGE == 0;

            // 대표 포켓몬: 나이/레벨 증가 + 산책 규칙으로 스탯 변화 (스탯 주기는 실제 산책 시간 기준)
            if (ageThisMinute)
            {
                AgeMinutes++;
                if (AgeMinutes % MINUTES_PER_LEVEL == 0) { IsEvolutionPostponed = false; IsFarewellPostponed = false; }
            }
            NeglectTicks = 0;

            if (Weight > 0 && _walkMinutes % WEIGHT_BURN_EVERY_MINUTES == 0) Weight--;
            Joy = ApplyWalkStat(Joy, _walkMinutes, 10, +1);
            Fullness = ApplyWalkStat(Fullness, _walkMinutes, 20, -1);
            Energy = ApplyWalkStat(Energy, _walkMinutes, 20, -1);
            Hygiene = ApplyWalkStat(Hygiene, _walkMinutes, 30, -1);

            // 나머지 파티 멤버도 함께 성장 (Party[0]은 대표이므로 Save 시 동기화)
            for (int i = 1; i < Party.Count; i++)
            {
                var m = Party[i];
                if (ageThisMinute)
                {
                    m.AgeMinutes++;
                    m.Level = 1 + (m.AgeMinutes / MINUTES_PER_LEVEL);
                }
                if (m.Weight > 0 && _walkMinutes % WEIGHT_BURN_EVERY_MINUTES == 0) m.Weight--;
                m.Joy = ApplyWalkStat(m.Joy, _walkMinutes, 10, +1);
                m.Fullness = ApplyWalkStat(m.Fullness, _walkMinutes, 20, -1);
                m.Energy = ApplyWalkStat(m.Energy, _walkMinutes, 20, -1);
                m.Hygiene = ApplyWalkStat(m.Hygiene, _walkMinutes, 30, -1);
            }

            TryFindWalkItem();
            Save();
        }

        // everyMinutes마다 delta만큼 변화. 감소는 하한선(30) 아래로 내려가지 않고, 이미 그보다 낮으면 그대로 둡니다.
        private static int ApplyWalkStat(int value, int walkMinutes, int everyMinutes, int delta)
        {
            if (walkMinutes % everyMinutes != 0) return value;
            if (delta > 0) return Math.Min(100, value + delta);
            return value <= WALK_STAT_FLOOR ? value : Math.Max(WALK_STAT_FLOOR, value + delta);
        }

        private void TryFindWalkItem()
        {
            if (_walkFoundItems.Count >= WALK_MAX_ITEMS) return;

            var rand = new Random();
            if (rand.Next(100) >= WALK_ITEM_CHANCE_PER_MIN) return;

            int roll = rand.Next(WalkItemTable.Sum(x => x.Weight));
            string itemId = WalkItemTable[^1].ItemId;
            foreach (var (id, weight) in WalkItemTable)
            {
                if (roll < weight) { itemId = id; break; }
                roll -= weight;
            }

            AddItem(itemId, 1);
            var def = ItemDex.Get(itemId);
            _walkFoundItems.Add(def.Name);
            WalkItemFound?.Invoke(def);
        }

        // 산책 펫이 풀숲을 뒤적이는 중인지 (RoamingView가 흔들리는 풀숲을 그림)
        private bool _isRummaging;
        [JsonIgnore] public bool IsRummaging { get => _isRummaging; set => SetProperty(ref _isRummaging, value); }

        // 산책 펫 머리 위 말풍선 (아이템을 주웠을 때 등)
        private string _roamingBubble = string.Empty;
        [JsonIgnore] public string RoamingBubble { get => _roamingBubble; set => SetProperty(ref _roamingBubble, value); }

        public async void ShowRoamingBubble(string text, int displaySeconds = 3)
        {
            RoamingBubble = text;
            await Task.Delay(displaySeconds * 1000);
            if (RoamingBubble == text) RoamingBubble = string.Empty;
        }
        #endregion
    }
}
