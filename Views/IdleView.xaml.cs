using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TamaPoke.Models;
using TamaPoke.Utils.Service;
using TamaPoke.Utils;

namespace TamaPoke.Views
{
    public partial class IdleView : System.Windows.Controls.UserControl
    {
        public IdleView()
        {
            InitializeComponent();
            OpacityTrack.Data = OpacityHitArea.Data = ArcGeometry(OPACITY_ARC_TOP_DEG, OPACITY_ARC_BOTTOM_DEG);
            Loaded += (s, e) => UpdateOpacityArc(); // 창이 붙은 뒤 현재 투명도로 손잡이 위치를 맞춤
        }

        // ==========================================
        // 투명도 원호 슬라이더 (몬스터볼 빨간 띠를 따라 움직임)
        // ==========================================
        // 각도는 화면 기준(오른쪽 = 0°, 위 = -90°, 시계 방향으로 증가). 위쪽 끝이 불투명, 아래쪽 끝이 가장 투명합니다.
        private const double OPACITY_ARC_CENTER = 160;      // 몬스터볼(320×320) 중심
        private const double OPACITY_ARC_RADIUS = 150;      // 빨간 띠의 가운데 (게임 영역 144 ~ 테두리 156)
        private const double OPACITY_ARC_TOP_DEG = -72;
        private const double OPACITY_ARC_BOTTOM_DEG = -12;  // 가운데 검은 띠에 닿기 전까지
        private const double MIN_WINDOW_OPACITY = 0.15, MAX_WINDOW_OPACITY = 1.0;

        private static System.Windows.Point ArcPoint(double degrees)
        {
            double rad = degrees * Math.PI / 180;
            return new System.Windows.Point(OPACITY_ARC_CENTER + OPACITY_ARC_RADIUS * Math.Cos(rad), OPACITY_ARC_CENTER + OPACITY_ARC_RADIUS * Math.Sin(rad));
        }

        private static System.Windows.Media.Geometry ArcGeometry(double fromDegrees, double toDegrees)
        {
            var figure = new System.Windows.Media.PathFigure { StartPoint = ArcPoint(fromDegrees) };
            figure.Segments.Add(new System.Windows.Media.ArcSegment(ArcPoint(toDegrees), new System.Windows.Size(OPACITY_ARC_RADIUS, OPACITY_ARC_RADIUS),
                0, false, System.Windows.Media.SweepDirection.Clockwise, true));
            return new System.Windows.Media.PathGeometry(new[] { figure });
        }

        // 현재 창 투명도에 맞춰 손잡이와 채워진 부분(아래쪽 끝 ~ 손잡이)을 그립니다.
        private void UpdateOpacityArc()
        {
            double opacity = Window.GetWindow(this)?.Opacity ?? MAX_WINDOW_OPACITY;
            double t = Math.Clamp((opacity - MIN_WINDOW_OPACITY) / (MAX_WINDOW_OPACITY - MIN_WINDOW_OPACITY), 0, 1);
            double degrees = OPACITY_ARC_BOTTOM_DEG + (OPACITY_ARC_TOP_DEG - OPACITY_ARC_BOTTOM_DEG) * t;

            var p = ArcPoint(degrees);
            Canvas.SetLeft(OpacityThumb, p.X - OpacityThumb.Width / 2);
            Canvas.SetTop(OpacityThumb, p.Y - OpacityThumb.Height / 2);
            OpacityFill.Data = ArcGeometry(degrees, OPACITY_ARC_BOTTOM_DEG);
        }

        // 마우스 위치의 각도로 투명도를 정합니다. (원호 범위 밖은 양 끝으로)
        private void SetOpacityFromPoint(System.Windows.Point p)
        {
            var window = Window.GetWindow(this);
            if (window == null) return;

            double degrees = Math.Atan2(p.Y - OPACITY_ARC_CENTER, p.X - OPACITY_ARC_CENTER) * 180 / Math.PI;
            degrees = Math.Clamp(degrees, OPACITY_ARC_TOP_DEG, OPACITY_ARC_BOTTOM_DEG);
            double t = (OPACITY_ARC_BOTTOM_DEG - degrees) / (OPACITY_ARC_BOTTOM_DEG - OPACITY_ARC_TOP_DEG);
            window.Opacity = MIN_WINDOW_OPACITY + (MAX_WINDOW_OPACITY - MIN_WINDOW_OPACITY) * t;
            UpdateOpacityArc();
        }

        private void OpacityArc_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var arcCanvas = (Canvas)((FrameworkElement)sender).Parent;
            ((UIElement)sender).CaptureMouse();
            SetOpacityFromPoint(e.GetPosition(arcCanvas));
            e.Handled = true; // 창 드래그(이동)로 넘어가지 않게
        }

        private void OpacityArc_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is UIElement element && element.IsMouseCaptured)
                SetOpacityFromPoint(e.GetPosition((Canvas)((FrameworkElement)sender).Parent));
        }

        private void OpacityArc_MouseUp(object sender, MouseButtonEventArgs e) => ((UIElement)sender).ReleaseMouseCapture();

        private PokemonState? GetPet() => DataContext as PokemonState;

        private void GameArea_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsBallGameOpen)
            {
                System.Windows.Point pos = e.GetPosition((UIElement)sender);
                double targetX = pos.X - 160;
                pet.TargetPosX = Math.Max(-100, Math.Min(100, targetX));
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e) => GetPet()?.ToggleMute();

        private void ToggleFeedMenu_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.PlaySound(SoundManager.N_TAP);
                pet.IsPlayMenuOpen = false;
                pet.IsFeedMenuOpen = !pet.IsFeedMenuOpen;
            }
            else pet?.PlaySound(SoundManager.N_DENY);
        }

        private void FeedItem_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out int foodType))
            {
                pet.PlaySound(SoundManager.N_EAT);
                pet.Feed(foodType);
            }
        }

        private void TogglePlayMenu_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.PlaySound(SoundManager.N_TAP);
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = !pet.IsPlayMenuOpen;
            }
            else pet?.PlaySound(SoundManager.N_DENY);
        }

        // ==========================================
        // 🌟 5종 미니게임 선택 메뉴 버튼 이벤트
        // ==========================================
        private void PlayBall_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.PlaySound(SoundManager.N_PLAY);
                pet.StartBallGame();

                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    BallGameView ballView = new BallGameView();
                    ballView.DataContext = pet;
                    mainWindow.NavigateTo(ballView); // 🌟 메인 프레임에 공 튕기기 뷰 로드!
                }
            }
        }

        private void PlayCatch_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.PlaySound(SoundManager.N_PLAY);
                pet.StartCatchGame(); // 게임 시작!

                // 캐싱된 뷰를 사용하여 부드럽게 전환
                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    CatchGameView catchView = CatchGameView.GetInstance(pet);
                    mainWindow.NavigateTo(catchView);
                }
            }
        }

        private void PlayMemo_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.PlaySound(SoundManager.N_PLAY);
                pet.StartMemoGame(); // 🌟 게임 시작

                // 뷰 통째로 전환
                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    MemoGameView memoView = MemoGameView.GetInstance(pet);
                    mainWindow.NavigateTo(memoView);
                }
            }
        }

        private void PlayClean_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.PlaySound(SoundManager.N_PLAY);
                pet.StartCleanGame(); // 게임 시작

                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    // 최적화된 캐싱 뷰 사용
                    CleanGameView cleanView = CleanGameView.GetInstance(pet);
                    mainWindow.NavigateTo(cleanView);
                }
            }
        }

        private void TapBall_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsBallGameOpen)
            {
                pet.PlaySound(SoundManager.N_TAP);
                pet.TapBall();
            }
        }
        
        private void SleepButton_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg)
            {
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = false;
                pet.PlaySound(SoundManager.N_TAP);
                pet.ToggleSleep();
            }
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg)
            {
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = false;
                pet.PlaySound(SoundManager.N_MEDAL);
                pet.Clean();
            }
            else pet?.PlaySound(SoundManager.N_DENY);
        }

        private void Evolve_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.PlaySound(SoundManager.N_MEDAL);
                pet.StartEvolutionCeremony();
            }
        }

        private void PostponeEvolve_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.PlaySound(SoundManager.N_TAP);
                pet.PostponeEvolve();
            }
        }

        private void StartFarewell_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) { pet.PlaySound(SoundManager.N_MEDAL); pet.StartFarewell(); }
        }

        private void PostponeFarewell_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.PlaySound(SoundManager.N_TAP);
                pet.PostponeFarewell();
            }
        }

        private void StartRunaway_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) { pet.PlaySound(SoundManager.N_DENY); pet.StartRunaway(); }
        }

        private void DexButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.IsDexOpen = !pet.IsDexOpen;
            }
        }

        private void PetImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Screen_MouseLeftButtonDown(sender, e);

        private void Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PokemonState pet) return;

            if (pet.IsEgg)
            {
                pet.PlaySound(SoundManager.N_TAP);
                pet.TapEgg();
            }
            else if (pet.IsAlive && !pet.IsSleeping && !pet.IsCeremony)
            {
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = false;
                pet.PlaySound(SoundManager.N_HEART);
                pet.Pet();
            }
        }

        private void MenuDex_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.IsDexOpen = true;
                if (pet.IsDexOpen) pet.IsProfileOpen = false;
            }
        }

        private void MenuProfile_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.IsProfileOpen = true;
                pet.ProfilePage = 0;
                if (pet.IsProfileOpen) pet.IsDexOpen = false;
            }
        }
        private void MenuInventory_Click(object sender, RoutedEventArgs e)
        {
            // 현재 화면의 데이터를 담당하는 PokemonState를 가져옵니다.
            if (this.DataContext is Models.PokemonState state && state.IsAlive)
            {
                // 가방이 열려있으면 닫고, 닫혀있으면 엽니다.
                state.IsInventoryOpen = !state.IsInventoryOpen;

                // UI가 겹쳐서 지저분해지는 것을 막기 위해, 가방을 열 때는 다른 하위 메뉴들을 닫아줍니다.
                if (state.IsInventoryOpen)
                {
                    state.IsFeedMenuOpen = false;
                    state.IsPlayMenuOpen = false;
                }
            }
        }

        private void MenuRelease_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                MessageBoxResult result = System.Windows.MessageBox.Show(
                    Loc.T("release.confirm.message"),
                    Loc.T("release.confirm.title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    pet.PlaySound(SoundManager.N_DENY);
                    pet.IsDexOpen = false;
                    pet.IsProfileOpen = false;
                    pet.StartRelease();
                }
            }
            else
            {
                GetPet()?.PlaySound(SoundManager.N_DENY);
            }
        }
        private void MenuItem_OpenParty_Click(object sender, RoutedEventArgs e)
        {
            // DataContext가 PokemonState인지 확인하고 상태를 변경합니다.
            if (this.DataContext is PokemonState state)
            {
                state.OpenParty();
            }
        }
    }
}
