using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TamaPoke.Utils;
using TamaPoke.Utils.Service;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 애니메이션 (Animation)
        // 포켓몬의 다양한 행동과 감정 표현을 담당하는 애니메이션 상수입니다.
        public const int ANIM_WALK = 0;         // 걷기 (이동할 때 기본 동작)
        public const int ANIM_ATTACK = 1;       // 공격 (물리적인 기본 타격)
        public const int ANIM_STRIKE = 2;       // 강하게 치기 (연속 치기나 강한 타격)
        public const int ANIM_SHOOT = 3;        // 발사 (광선이나 특수 공격 발사)
        public const int ANIM_SHAKE = 4;        // 몸 털기 (목욕 후 물기를 털어내는 동작 등)
        public const int ANIM_SLEEP = 5;        // 잠자기 (바닥에 웅크리거나 엎드려 쿨쿨 잠듦)
        public const int ANIM_HURT = 6;         // 맞아서 아파하기 (가벼운 데미지를 입었을 때)
        public const int ANIM_IDLE = 7;         // 대기 상태 (가만히 서서 숨만 쉬는 기본 동작)
        public const int ANIM_SWING = 8;        // 크게 휘두르기 (꼬리나 팔을 넓게 휘두름)
        public const int ANIM_DOUBLE = 9;       // 연속 공격 / 두 번 공격
        public const int ANIM_HOP = 10;         // 제자리 점프 (기분 좋을 때 깡총깡총 뜀)
        public const int ANIM_CHARGE = 11;      // 기 모으기 / 돌진 준비
        public const int ANIM_ROTATE = 12;      // 제자리에서 빙글빙글 돌기
        public const int ANIM_EVENTSLEEP = 13;  // 특수 수면 (이벤트 중 기절하듯 잠듦)
        public const int ANIM_WAKE = 14;        // 잠에서 깨어나기 (기지개를 켜거나 일어남)
        public const int ANIM_EAT = 15;         // 먹방 (입을 오물거리며 먹이를 먹는 동작)
        public const int ANIM_TUMBLE = 16;      // 앞으로 구르기 / 덤블링
        public const int ANIM_POSE = 17;        // 포즈 취하기 (승리하거나 기분 좋을 때 폼 잡기)
        public const int ANIM_PULL = 18;        // 무언가를 힘껏 당기기
        public const int ANIM_PAIN = 19;        // 고통스러워하기 (응가가 쌓였거나 많이 아플 때)
        public const int ANIM_FLOAT = 20;       // 공중에 둥둥 떠있기 (진화할 때나 초능력 사용 시)
        public const int ANIM_DEEPBREATH = 21;  // 심호흡 / 크게 포효하기
        public const int ANIM_NOD = 22;         // 고개 끄덕이기 (긍정적인 반응이나 호응)
        public const int ANIM_SIT = 23;         // 자리에 얌전히 앉기
        public const int ANIM_LOOKUP = 24;      // 하늘 위 쳐다보기
        public const int ANIM_SINK = 25;        // 풀썩 주저앉기 (기운이 다 빠졌거나 크게 실망함)
        public const int ANIM_TRIP = 26;        // 발에 걸려 넘어질 뻔하기 (비틀거림)
        public const int ANIM_LAYING = 27;      // 바닥에 완전 납작하게 엎드리기
        public const int ANIM_LEAPFORTH = 28;   // 앞으로 펄쩍 뛰어들기 (강력한 돌진)
        public const int ANIM_HEAD = 29;        // 머리 쓰기 / 박치기
        public const int ANIM_CRINGE = 30;      // 몸을 잔뜩 움츠리기 (무섭거나 심하게 배고플 때)
        public const int ANIM_LOSTBALANCE = 31; // 중심 잃기 (어지러워서 비틀거림)
        public const int ANIM_TUMBLEBACK = 32;  // 뒤로 튕겨져 나가기 / 뒤로 구르기
        public const int ANIM_FAINT = 33;       // 기절 (체력이 0이 되어 완전히 쓰러짐)
        public const int ANIM_HITGROUND = 34;   // 땅에 강하게 부딪히기 / 거친 착지

        private BitmapSource[]? _animationFrames;
        private int _currentFrameIndex = 0;

        // 프레임별 재생 시간 (1/60초 단위, AnimData.xml의 Durations). 없으면 DEFAULT_FRAME_MS로 고정 재생합니다.
        private int[]? _animationDurations;
        private const double DEFAULT_FRAME_MS = 200.0;
        private const double PMD_TICK_MS = 1000.0 / 60.0;
        private double _frameElapsedMs = 0;
        private long _lastLoopTimestamp = 0;
        private DispatcherTimer? _mainLoopTimer;
        private BitmapSource? _currentFrame;

        [JsonIgnore] public BitmapSource? CurrentFrame { get => _currentFrame; set => SetProperty(ref _currentFrame, value); }

        private BitmapSource? _enemyCurrentFrame;
        [JsonIgnore] public BitmapSource? EnemyCurrentFrame { get => _enemyCurrentFrame; set => SetProperty(ref _enemyCurrentFrame, value); }

        private double _posX = 0;
        [JsonIgnore] public double PosX { get => _posX; set => SetProperty(ref _posX, value); }
        private double _posY = 0;
        [JsonIgnore] public double PosY { get => _posY; set => SetProperty(ref _posY, value); }
        [JsonIgnore] public double TargetPosX { get; set; } = 0;
        [JsonIgnore] public double TargetPosY { get; set; } = 0;

        private double _flipX = 1;
        [JsonIgnore] public double FlipX { get => _flipX; set => SetProperty(ref _flipX, value); }

        public void ResetPosition()
        {
            PosX = 0; PosY = 0; TargetPosX = 0; TargetPosY = 0; FlipX = 1;
            PlayTempAction(ANIM_IDLE, 0);
            CheckStateAndAnimate();
        }

        public void ReloadSprite()
        {
            _animationFrames = null;
            _currentActionId = -1;
            CheckStateAndAnimate();
        }

        private int _tempActionId = ANIM_IDLE;
        private int _tempActionTimer = 0;
        // true: 혼자 노는 자동 행동(걷기/포즈 등), false: 유저 상호작용이나 이벤트로 시작된 행동
        private bool _isAutoAction = false;

        // 유저 행동/이벤트 모션 재생 (응가·청결 상태 모션보다 우선합니다)
        private void PlayTempAction(int actionId, int ticks)
        {
            _tempActionId = actionId;
            _tempActionTimer = ticks;
            _isAutoAction = false;
        }

        // 자동 행동 모션 재생 (응가가 생기거나 더러워지면 중단됩니다)
        private void PlayAutoAction(int actionId, int ticks)
        {
            PlayTempAction(actionId, ticks);
            _isAutoAction = true;
        }
        private int _currentActionId = ANIM_IDLE;
        private int _enemyTempActionId = ANIM_IDLE;
        private int _enemyTempActionTimer = 0;

        [JsonIgnore] public bool IsEating => _tempActionTimer > 0 && _tempActionId == ANIM_EAT;
        [JsonIgnore] public bool IsPlaying => _tempActionTimer > 0 && (_tempActionId == ANIM_POSE || _tempActionId == ANIM_HOP);

        public void StartSubPetAnimationOnly()
        {
            EnsureMainLoopTimer();
            CheckStateAndAnimate();
            _mainLoopTimer.Start();
        }

        private double GetCurrentFrameDurationMs()
        {
            if (_animationDurations != null && _currentFrameIndex < _animationDurations.Length)
                return _animationDurations[_currentFrameIndex] * PMD_TICK_MS;
            return DEFAULT_FRAME_MS;
        }

        // 실제 경과 시간만큼 프레임을 넘깁니다. (DispatcherTimer 간격이 흔들려도 재생 속도가 일정)
        private void AdvanceAnimationFrame()
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            if (_lastLoopTimestamp != 0)
                _frameElapsedMs += (now - _lastLoopTimestamp) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            _lastLoopTimestamp = now;

            if (_animationFrames == null || _animationFrames.Length <= 1) { _frameElapsedMs = 0; return; }

            // 창이 잠시 멈췄다 돌아온 경우 한꺼번에 여러 바퀴 돌지 않도록 상한을 둡니다.
            if (_frameElapsedMs > 1000) _frameElapsedMs = 1000;

            bool changed = false;
            double frameMs = GetCurrentFrameDurationMs();
            while (_frameElapsedMs >= frameMs)
            {
                _frameElapsedMs -= frameMs;
                _currentFrameIndex = (_currentFrameIndex + 1) % _animationFrames.Length;
                frameMs = GetCurrentFrameDurationMs();
                changed = true;
            }
            if (changed) CurrentFrame = _animationFrames[_currentFrameIndex];
        }

        private void MainLoopTimer_Tick(object? sender, EventArgs e)
        {
            AdvanceAnimationFrame();

            // 🌟 1. 걷기 중 양 끝에 도달했을 때 즉시 턴하는 로직!
            if (!IsFreeRoaming && _currentActionId == ANIM_WALK && _tempActionTimer > 0 && !IsBattleOpen)
            {
                // 🌟 유저님께서 맞춰주신 완벽한 비대칭 좌표를 적용합니다.
                double limitRight = 100;  // 오른쪽 끝
                double limitLeft = -50;  // 왼쪽 끝 (오른쪽에 비해 덜 가게 설정)
                double moveSpeed = 1.5;

                // FlipX가 1이면 왼쪽(-), -1이면 오른쪽(+)으로 이동합니다.
                PosX += (-FlipX * moveSpeed);

                // 오른쪽 끝에 도달했을 때
                if (PosX >= limitRight)
                {
                    PosX = limitRight;
                    FlipX = 1; // 대기 모션 없이 즉시 왼쪽으로 몸을 돌리고 계속 걷습니다.
                }
                // 왼쪽 끝에 도달했을 때
                else if (PosX <= limitLeft)
                {
                    PosX = limitLeft;
                    FlipX = -1; // 즉시 오른쪽으로 몸을 돌리고 계속 걷습니다.
                }
            }

            // 🌟 2. 애니메이션 타이머 감소 및 상태 갱신
            if (_tempActionTimer > 0)
            {
                _tempActionTimer--;
                if (_tempActionTimer <= 0)
                {
                    IsBathing = false;
                    CheckStateAndAnimate(); // 걷기 시간이 완전히 끝나면 기본 IDLE 상태 등으로 돌아갑니다.
                }
            }

            if (_enemyTempActionTimer > 0)
            {
                _enemyTempActionTimer--;
                if (_enemyTempActionTimer <= 0 && IsBattleOpen) UpdateEnemyAnimation(ANIM_IDLE);
            }

            if (IsBallGameOpen) StepBallGame();
            if (IsCatchGameOpen) StepCatchGame();
            if (IsCleanGameOpen) StepCleanGame();
        }

        private int GetTargetAction()
        {
            if (IsProfileOpen || IsBattleOpen) return ANIM_IDLE;

            if (Ceremony == 1) return ANIM_POSE;
            if (Ceremony == 2) return ANIM_WALK;
            if (Ceremony == 3) return ANIM_HOP;
            if (Ceremony == 4) return ANIM_FLOAT;

            if (IsAnyMiniGameOpen) return ANIM_IDLE;

            if (IsSleeping) return ANIM_SLEEP;

            // 유저가 직접 한 행동(먹기/쓰다듬기/씻기 등)은 상태 모션보다 먼저 보여줍니다.
            if (_tempActionTimer > 0 && !_isAutoAction) return _tempActionId;

            if (Poops > 0) return ANIM_PAIN;
            if (Hygiene < 30) return ANIM_HURT;

            if (_tempActionTimer > 0) return _tempActionId;

            if (Fullness < 30) return ANIM_CRINGE;
            if (Joy < 30) return ANIM_SINK;

            if (Energy < 30) return ANIM_EVENTSLEEP;
            return ANIM_IDLE;
        }

        public void CheckStateAndAnimate()
        {
            OnPropertyChanged(nameof(MoodText));
            OnPropertyChanged(nameof(CanEvolveNow));
            OnPropertyChanged(nameof(CanShowEvolvePrompt));
            OnPropertyChanged(nameof(CanFarewellNow));
            OnPropertyChanged(nameof(CanShowFarewellPrompt));
            OnPropertyChanged(nameof(IsEating));
            OnPropertyChanged(nameof(IsPlaying));

            int targetAction = GetTargetAction();
            if (_currentActionId != targetAction || _animationFrames == null)
                UpdateAnimation(targetAction);
        }

        private static readonly HashSet<int> SideViewActions = new()
        {
            ANIM_WALK, ANIM_SLEEP, ANIM_EVENTSLEEP, ANIM_LAYING, ANIM_ATTACK,
            ANIM_STRIKE, ANIM_SHOOT, ANIM_HOP, ANIM_CHARGE, ANIM_LEAPFORTH,
            ANIM_TUMBLE, ANIM_HURT, ANIM_PAIN, ANIM_CRINGE, ANIM_FAINT
        };

        private void UpdateAnimation(int actionToLoad)
        {
            _currentActionId = actionToLoad;
            string spriteFolder = AppPaths.SpriteFolder;

            List<SpriteFrameData>? rawStrips = null;

            if (IsEgg)
            {
                string eggPath = Path.Combine(spriteFolder, "p0000_Egg.bin");
                if (File.Exists(eggPath)) rawStrips = Utils.BinSpriteReader.LoadFramesFromBin(eggPath);
            }
            else
            {
                string targetPath = Path.Combine(spriteFolder, SpriteFileName);
                if (!File.Exists(targetPath)) targetPath = Path.Combine(spriteFolder, $"p{SpeciesId:D4}.bin");

                if (File.Exists(targetPath))
                {
                    rawStrips = Utils.BinSpriteReader.LoadFramesFromBin(targetPath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[에러] 스프라이트 파일을 찾을 수 없습니다: {targetPath}");
                }
            }

            if (rawStrips != null && rawStrips.Count > 0)
            {
                // 요청한 모션이 없는(비어 있는) 스프라이트는 IDLE → 첫 번째 유효 모션 순으로 대체합니다.
                SpriteFrameData? PickStrip(int index) => index >= 0 && index < rawStrips.Count ? rawStrips[index] : null;
                var targetData = PickStrip(actionToLoad) ?? PickStrip(ANIM_IDLE) ?? rawStrips.FirstOrDefault(r => r != null);
                if (targetData != null && targetData.Image != null)
                {
                    int directionRow = Direction;

                    if (!IsFreeRoaming)
                    {
                        // 애니메이션을 강제하는 코드를 빼고 깔끔하게 복구!
                        directionRow = SideViewActions.Contains(actionToLoad) ? 6 : 0;
                    }

                    if (targetData.Image.PixelHeight / targetData.FrameHeight < 2)
                    {
                        directionRow = 0;
                    }

                    List<BitmapSource> slicedFrames = SliceStripIntoFrames(targetData.Image, targetData.FrameWidth, targetData.FrameHeight, directionRow);
                    if (slicedFrames.Count > 0)
                    {
                        _animationFrames = slicedFrames.ToArray();
                        _animationDurations = targetData.Durations.Length == slicedFrames.Count ? targetData.Durations : null;
                        _currentFrameIndex = 0;
                        _frameElapsedMs = 0;
                        CurrentFrame = _animationFrames[0];
                    }
                }
            }
        }

        private void UpdateEnemyAnimation(int actionToLoad)
        {
            _enemyTempActionId = actionToLoad;
            string spriteFolder = AppPaths.SpriteFolder;
            string enemyPath = Path.Combine(spriteFolder, $"p{EnemySpeciesId:D4}.bin");

            if (Directory.Exists(spriteFolder))
            {
                var matchedFiles = Directory.GetFiles(spriteFolder, $"p{EnemySpeciesId:D4}*.bin");
                if (matchedFiles.Length > 0) enemyPath = matchedFiles[0];
            }

            if (File.Exists(enemyPath))
            {
                var rawStrips = Utils.BinSpriteReader.LoadFramesFromBin(enemyPath);
                if (rawStrips != null && rawStrips.Count > 0)
                {
                    int stripIndex = actionToLoad;
                    if (stripIndex >= rawStrips.Count) stripIndex = rawStrips.Count - 1;
                    if (stripIndex < 0) stripIndex = 0;

                    var targetData = rawStrips[stripIndex];
                    if (targetData != null && targetData.Image != null)
                    {
                        int directionRow = 6;

                        List<BitmapSource> slicedFrames = SliceStripIntoFrames(targetData.Image, targetData.FrameWidth, targetData.FrameHeight, directionRow);
                        if (slicedFrames.Count > 0) EnemyCurrentFrame = slicedFrames[0];
                    }
                }
            }
        }

        private List<BitmapSource> SliceStripIntoFrames(BitmapSource strip, int fw, int fh, int targetRow = 0)
        {
            List<BitmapSource> frames = new List<BitmapSource>();

            if (fw <= 0 || fh <= 0) return frames;

            int columns = strip.PixelWidth / fw;
            int maxRow = (strip.PixelHeight / fh) - 1;
            int safeRow = Math.Max(0, Math.Min(targetRow, maxRow));

            for (int i = 0; i < columns; i++)
            {
                System.Windows.Int32Rect cropRect = new System.Windows.Int32Rect(i * fw, safeRow * fh, fw, fh);
                CroppedBitmap croppedFrame = new CroppedBitmap(strip, cropRect);
                croppedFrame.Freeze();
                frames.Add(croppedFrame);
            }
            return frames;
        }

        // 프레임 재생/임시 모션 타이머/미니게임 진행을 담당하는 33ms 루프
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_mainLoopTimer))]
        private void EnsureMainLoopTimer()
        {
            if (_mainLoopTimer != null) return;
            _mainLoopTimer = new DispatcherTimer(DispatcherPriority.Render);
            _mainLoopTimer.Interval = TimeSpan.FromMilliseconds(33);
            _mainLoopTimer.Tick += MainLoopTimer_Tick;
        }
        #endregion
    }
}
