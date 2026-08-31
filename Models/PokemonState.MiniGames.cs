using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using TamaPoke.Utils.Service; // SoundManager 사용을 위해 필요

namespace TamaPoke.Models
{
    // 🌟 partial 키워드를 통해 기존 PokemonState와 하나의 클래스로 합쳐집니다.
    public partial class PokemonState
    {
        #region 미니게임 상태 관리 (Mini Games)

        // 🌟 위치 헬퍼: 어느 상황에서든 닫을 때 무조건 중앙 복귀
        private void ResetPosition()
        {
            PosX = 0; PosY = 0; TargetPosX = 0; TargetPosY = 0; FlipX = 1;
            _tempActionId = 0; // ANIM_IDLE
            _tempActionTimer = 0;
            CheckStateAndAnimate();
        }

        [JsonIgnore] public bool IsMinigameOpen => IsBallGameOpen;

        private bool _isBallGameOpen = false;
        public bool IsBallGameOpen { get => _isBallGameOpen; set { if (SetProperty(ref _isBallGameOpen, value)) { if (!value) ResetPosition(); OnPropertyChanged(nameof(IsMinigameOpen)); OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

        private bool _isCatchGameOpen = false;
        public bool IsCatchGameOpen { get => _isCatchGameOpen; set { if (SetProperty(ref _isCatchGameOpen, value)) { if (!value) ResetPosition(); OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

        private bool _isMemoGameOpen = false;
        public bool IsMemoGameOpen { get => _isMemoGameOpen; set { if (SetProperty(ref _isMemoGameOpen, value)) { if (!value) ResetPosition(); OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

        private bool _isCleanGameOpen = false;
        public bool IsCleanGameOpen { get => _isCleanGameOpen; set { if (SetProperty(ref _isCleanGameOpen, value)) { if (!value) ResetPosition(); OnPropertyChanged(nameof(IsAliveAndNotEgg)); OnPropertyChanged(nameof(MoodText)); } } }

        [JsonIgnore]
        public bool IsAnyMiniGameOpen => IsBallGameOpen || IsCatchGameOpen || IsMemoGameOpen || IsCleanGameOpen;

        public double TargetPosX { get; set; } = 0;
        public double TargetPosY { get; set; } = 0;

        // --- 1. Ball Game ---
        private int _gameScore = 0;
        public int GameScore { get => _gameScore; set => SetProperty(ref _gameScore, value); }
        private int _gameHighScore = 0;
        public int GameHighScore { get => _gameHighScore; set => SetProperty(ref _gameHighScore, value); }
        private int _gameMisses = 0;
        public int GameMisses { get => _gameMisses; set => SetProperty(ref _gameMisses, value); }
        private double _ballX = 144;
        public double BallX { get => _ballX; set => SetProperty(ref _ballX, value); }
        private double _ballY = 60;
        public double BallY { get => _ballY; set => SetProperty(ref _ballY, value); }
        private double _ballVelX = 0;
        private double _ballVelY = 0;

        // --- 2. Catch Game ---
        private int _catchScore;
        public int CatchScore { get => _catchScore; set { _catchScore = value; OnPropertyChanged(); } }
        private int _catchHighScore;
        public int CatchHighScore { get => _catchHighScore; set { _catchHighScore = value; OnPropertyChanged(); } }
        private double _catchTimeLeft;
        public double CatchTimeLeft { get => _catchTimeLeft; set { _catchTimeLeft = value; OnPropertyChanged(); } }
        private string _currentFruitIcon = "🍎";
        public string CurrentFruitIcon { get => _currentFruitIcon; set { _currentFruitIcon = value; OnPropertyChanged(); } }
        private double _fruitX;
        public double FruitX { get => _fruitX; set { _fruitX = value; OnPropertyChanged(); } }
        private double _fruitY;
        public double FruitY { get => _fruitY; set { _fruitY = value; OnPropertyChanged(); } }

        // --- 3. Memo Game ---
        private int _memoScore;
        public int MemoScore { get => _memoScore; set { _memoScore = value; OnPropertyChanged(); } }
        private int _memoHighScore;
        public int MemoHighScore { get => _memoHighScore; set { _memoHighScore = value; OnPropertyChanged(); } }
        private string _memoStatusText = "";
        public string MemoStatusText { get => _memoStatusText; set { _memoStatusText = value; OnPropertyChanged(); } }
        private bool _isMemoBtn0Lit; public bool IsMemoBtn0Lit { get => _isMemoBtn0Lit; set { _isMemoBtn0Lit = value; OnPropertyChanged(); } }
        private bool _isMemoBtn1Lit; public bool IsMemoBtn1Lit { get => _isMemoBtn1Lit; set { _isMemoBtn1Lit = value; OnPropertyChanged(); } }
        private bool _isMemoBtn2Lit; public bool IsMemoBtn2Lit { get => _isMemoBtn2Lit; set { _isMemoBtn2Lit = value; OnPropertyChanged(); } }
        private bool _isMemoBtn3Lit; public bool IsMemoBtn3Lit { get => _isMemoBtn3Lit; set { _isMemoBtn3Lit = value; OnPropertyChanged(); } }
        private List<int> _memoSequence = new List<int>();
        private int _memoInputIndex = 0;
        private bool _isMemoShowingSequence = false;

        // --- 4. Clean Game ---
        private int _cleanScore;
        public int CleanScore { get => _cleanScore; set { _cleanScore = value; OnPropertyChanged(); } }
        private int _cleanHighScore;
        public int CleanHighScore { get => _cleanHighScore; set { _cleanHighScore = value; OnPropertyChanged(); } }
        private string _cleanStatusText = "";
        public string CleanStatusText { get => _cleanStatusText; set { _cleanStatusText = value; OnPropertyChanged(); } }
        public ObservableCollection<DirtItem> DirtItems { get; } = new();
        private double _dirtSpawnTimer = 0;
        private int _dirtIdCounter = 0;
        #endregion

        #region 미니게임 세부 로직 (Mini Game Details)
        public async void TriggerAttackMotion()
        {
            if (!IsCleanGameOpen) return;
            if (!IsMuted) SoundManager.Play(SoundManager.N_TAP);
            UpdateAnimation(6); // ANIM_ATTACK
            await Task.Delay(500);
            if (IsCleanGameOpen) CheckStateAndAnimate();
        }

        // --- 1. Ball Game ---
        public void StartBallGame()
        {
            if (IsEgg || IsSleeping || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;
            IsPlayMenuOpen = false; IsBallGameOpen = true; GameScore = 0; GameMisses = 0; TargetPosX = 0; PosX = 0; RespawnBall(); CheckStateAndAnimate();
        }
        private void RespawnBall() { Random rand = new Random(); BallX = 50 + rand.Next(80); BallY = 40; double sp = 1.2 + GameScore * 0.04; if (sp > 3.0) sp = 3.0; _ballVelX = (rand.Next(2) == 0) ? sp : -sp; _ballVelY = 0; }
        private void StepBallGame()
        {
            double grav = 0.35 + GameScore * 0.01; if (grav > 0.7) grav = 0.7; _ballVelY += grav;
            BallX += _ballVelX; BallY += _ballVelY;

            double dx = BallX - 144; double dy = BallY - 144; double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > 130)
            {
                double nx = dx / dist; double ny = dy / dist; double dot = _ballVelX * nx + _ballVelY * ny;
                if (dot > 0) { _ballVelX = (_ballVelX - 2 * dot * nx) * 0.85; _ballVelY = (_ballVelY - 2 * dot * ny) * 0.85; }
                BallX = 144 + nx * 130; BallY = 144 + ny * 130;
            }

            if (BallY > 260) { GameMisses++; if (GameMisses >= 3) { EndBallGame(); } else { RespawnBall(); } }

            double chase = (TargetPosX - PosX) * 0.35; if (chase > 12) chase = 12; if (chase < -12) chase = -12;
            PosX += chase; if (chase > 1.0) FlipX = 1; else if (chase < -1.0) FlipX = -1;
        }

        public void TapBall()
        {
            if (!IsBallGameOpen) return;

            double ballCenterX = BallX + 16;
            double ballCenterY = BallY + 16;
            double pokeCenterX = 144 + PosX;
            double pokeCenterY = 160;

            double dx = ballCenterX - pokeCenterX;
            double dy = ballCenterY - pokeCenterY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= 70)
            {
                GameScore++; double lift = 5.0 + (GameScore > 16 ? 2.5 : GameScore * 0.15); _ballVelY = -lift;
                _ballVelX += dx * 0.05;
                if (_ballVelX > 5.0) _ballVelX = 5.0; if (_ballVelX < -5.0) _ballVelX = -5.0;

                _tempActionId = 8; // ANIM_HOP
                _tempActionTimer = 15;
                CheckStateAndAnimate();
            }
        }

        private void EndBallGame()
        {
            IsBallGameOpen = false; if (GameScore > GameHighScore) GameHighScore = GameScore;
            int v = TrSpeed + GameScore / 5; TrSpeed = v > 100 ? 100 : v;
            Joy = Clamp100(Joy + 5 + (GameScore > 15 ? 30 : GameScore * 2)); Energy = Math.Max(Energy - (10 + GameScore / 2), 5); Fullness = Math.Max(Fullness - 5, 5);
            int burn = Weight - GameScore * 2; Weight = Math.Max(burn, 0); Bond = Clamp100(Bond + 2);
            ResetPosition(); _tempActionId = 7; // ANIM_POSE
            _tempActionTimer = 90;
            OnPropertyChanged(nameof(IsPlaying)); CheckStateAndAnimate();
        }

        // --- 2. Catch Game ---
        public void StartCatchGame() { if (IsEgg || IsSleeping || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return; IsPlayMenuOpen = false; IsCatchGameOpen = true; CatchScore = 0; CatchTimeLeft = 10.0; TargetPosX = 0; PosX = 0; FlipX = 1; SpawnFruit(); CheckStateAndAnimate(); }
        private void SpawnFruit() { FruitY = -30; Random rand = new Random(); FruitX = rand.Next(30, 250); string[] fruits = { "🍎", "🍌", "🍇", "🍓", "🍊", "🍉", "🍒", "🍑" }; CurrentFruitIcon = fruits[rand.Next(fruits.Length)]; }
        private void StepCatchGame()
        {
            CatchTimeLeft -= 0.033; if (CatchTimeLeft <= 0) { CatchTimeLeft = 0; EndCatchGame(); return; }
            double chase = (TargetPosX - PosX) * 0.25; if (chase > 12) chase = 12; if (chase < -12) chase = -12;
            PosX += chase; if (chase > 0.5) FlipX = 1; else if (chase < -0.5) FlipX = -1;
            FruitY += 3.5 + (CatchScore * 0.15);
            double pokeAbsoluteX = 144 + PosX; double pokeAbsoluteY = 170;
            if (FruitY > pokeAbsoluteY - 30 && FruitY < pokeAbsoluteY + 30) { if (Math.Abs(pokeAbsoluteX - FruitX) < 45) { CatchScore++; SpawnFruit(); } }
            if (FruitY > 288) SpawnFruit();
        }
        private void EndCatchGame() { IsCatchGameOpen = false; if (CatchScore > CatchHighScore) CatchHighScore = CatchScore; TrSpeed = Math.Min(100, TrSpeed + CatchScore / 5); Fullness = Clamp100(Fullness + 10 + CatchScore); Energy = Math.Max(Energy - 10, 5); ResetPosition(); _tempActionId = 7; /* ANIM_POSE */ _tempActionTimer = 90; OnPropertyChanged(nameof(IsPlaying)); CheckStateAndAnimate(); }

        // --- 3. Memo Game ---
        public async void StartMemoGame() { if (IsEgg || IsSleeping || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return; IsPlayMenuOpen = false; IsMemoGameOpen = true; MemoScore = 0; _memoSequence.Clear(); _memoSequence.Add(new Random().Next(4)); TargetPosX = 0; PosX = 0; FlipX = 1; MemoStatusText = "준비... 시작! 🎮"; CheckStateAndAnimate(); await Task.Delay(2000); await PlayMemoSequence(); }
        private async Task PlayMemoSequence()
        {
            _isMemoShowingSequence = true; MemoStatusText = "잘 보고 기억하세요! 👀"; await Task.Delay(500);
            foreach (int btnIndex in _memoSequence) { if (!IsMemoGameOpen) return; SetMemoBtnState(btnIndex, true); await Task.Delay(400); SetMemoBtnState(btnIndex, false); await Task.Delay(200); }
            _memoInputIndex = 0; _isMemoShowingSequence = false; MemoStatusText = "이제 직접 눌러보세요! ✨";
        }
        private void SetMemoBtnState(int index, bool isLit) { if (index == 0) IsMemoBtn0Lit = isLit; else if (index == 1) IsMemoBtn1Lit = isLit; else if (index == 2) IsMemoBtn2Lit = isLit; else if (index == 3) IsMemoBtn3Lit = isLit; }
        public async void SubmitMemoInput(int btnIndex)
        {
            if (_isMemoShowingSequence || !IsMemoGameOpen) return; _isMemoShowingSequence = true; SetMemoBtnState(btnIndex, true); await Task.Delay(200); SetMemoBtnState(btnIndex, false);
            if (_memoSequence[_memoInputIndex] == btnIndex) { _memoInputIndex++; if (_memoInputIndex >= _memoSequence.Count) { MemoScore++; _memoSequence.Add(new Random().Next(4)); MemoStatusText = "정답! 다음 라운드! 👏"; await Task.Delay(1000); await PlayMemoSequence(); } else { _isMemoShowingSequence = false; } }
            else { EndMemoGame(); }
        }
        private void EndMemoGame() { MemoStatusText = "앗, 틀렸어요! 😢"; IsMemoGameOpen = false; if (MemoScore > MemoHighScore) MemoHighScore = MemoScore; Bond = Clamp100(Bond + 5 + (MemoScore * 2)); Energy = Math.Max(Energy - 5, 5); Fullness = Math.Max(Fullness - 2, 5); ResetPosition(); _tempActionId = 7; /* ANIM_POSE */ _tempActionTimer = 90; OnPropertyChanged(nameof(IsPlaying)); CheckStateAndAnimate(); }

        // --- 4. Clean Game ---
        public void StartCleanGame() { if (IsEgg || IsSleeping || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return; IsPlayMenuOpen = false; IsCleanGameOpen = true; CleanScore = 0; DirtItems.Clear(); _dirtIdCounter = 0; _dirtSpawnTimer = 0; TargetPosX = 0; TargetPosY = 0; PosX = 0; PosY = 0; FlipX = 1; CleanStatusText = "세균을 클릭해서 청소하세요!"; CheckStateAndAnimate(); }
        private void StepCleanGame()
        {
            double chaseX = (TargetPosX - PosX) * 0.25; if (chaseX > 12) chaseX = 12; if (chaseX < -12) chaseX = -12; PosX += chaseX;
            double chaseY = (TargetPosY - PosY) * 0.25; if (chaseY > 12) chaseY = 12; if (chaseY < -12) chaseY = -12; PosY += chaseY;

            if (chaseX > 0.5) FlipX = 1; else if (chaseX < -0.5) FlipX = -1;

            _dirtSpawnTimer += 0.033; double spawnInterval = Math.Max(0.3, 1.5 - (CleanScore * 0.05));
            if (_dirtSpawnTimer >= spawnInterval) { _dirtSpawnTimer = 0; SpawnDirt(); }
        }
        private void SpawnDirt()
        {
            Random rand = new Random(); double spawnX, spawnY; bool isOverlappingButton;
            do { double angle = rand.NextDouble() * Math.PI * 2; double radius = rand.NextDouble() * 110; spawnX = 144 + Math.Cos(angle) * radius - 15; spawnY = 144 + Math.Sin(angle) * radius - 15; isOverlappingButton = (spawnX > 90 && spawnX < 190 && spawnY > 230); } while (isOverlappingButton);
            DirtItems.Add(new DirtItem { Id = ++_dirtIdCounter, X = spawnX, Y = spawnY });
            if (DirtItems.Count >= 3) EndCleanGame();
        }

        public void TapDirt(int id)
        {
            if (!IsCleanGameOpen) return;
            var dirt = DirtItems.FirstOrDefault(d => d.Id == id);
            if (dirt != null)
            {
                DirtItems.Remove(dirt);
                CleanScore++;
                TriggerAttackMotion();
            }
        }

        private void EndCleanGame()
        {
            if (DirtItems.Count >= 3)
            {
                CleanStatusText = "세균이 너무 많아요! 😭";
            }
            else
            {
                CleanStatusText = "청소를 완료했습니다! ✨";
            }

            IsCleanGameOpen = false;

            if (CleanScore > CleanHighScore) CleanHighScore = CleanScore;
            Hygiene = Clamp100(Hygiene + 10 + CleanScore);
            Bond = Clamp100(Bond + 2 + (CleanScore / 5));
            Energy = Math.Max(Energy - 5, 5);

            ResetPosition();
            _tempActionId = 7; // ANIM_POSE
            _tempActionTimer = 30;

            OnPropertyChanged(nameof(IsPlaying));
            CheckStateAndAnimate();
        }
        #endregion
    }
}