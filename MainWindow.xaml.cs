using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TamaPoke.Models;
using TamaPoke.Views;

namespace TamaPoke
{
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        // ==========================================
        // 🌟 포켓몬 공통 설정 변수
        // ==========================================
        private readonly double PET_SIZE = 80.0;          // 포켓몬 창 크기
        private readonly double BASE_ROAMING_SPEED = 3.0;   // 포켓몬 기본 이동 속도

        public PokemonState? MyPet { get; set; }
        private DispatcherTimer? _gameTimer;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        // 놀아주기 모드 상태 관리 변수
        private bool _isPlayModeActive = false;

        private static readonly System.Collections.Generic.List<PetWindowInfo> _activePets = new System.Collections.Generic.List<PetWindowInfo>();

        public class PetWindowInfo
        {
            public required Window WindowInstance { get; set; }
            public required PokemonState State { get; set; }
        }

        public MainWindow()
        {
            try { InitializeComponent(); } catch (Exception ex) { System.Windows.MessageBox.Show($"XAML 로드 에러: {ex.InnerException?.Message ?? ex.Message}", "에러 추적기"); }
            try { SetupSystemTray(); } catch (Exception ex) { System.Windows.MessageBox.Show($"트레이 설정 에러: {ex.Message}", "에러 추적기"); }

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "monsterball.ico");

            if (System.IO.File.Exists(iconPath)) _notifyIcon.Icon = new System.Drawing.Icon(iconPath);

            _notifyIcon.Text = "다마포케 (TamaPoke)";
            _notifyIcon.Visible = true;

            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openMenuItem = new System.Windows.Forms.ToolStripMenuItem("다마포케 화면에 띄우기");
            openMenuItem.Click += (s, e) => { this.Show(); this.WindowState = WindowState.Normal; this.Activate(); };

            // 🌟 놀아주기 모드 메뉴 설정
            var playModeMenuItem = new System.Windows.Forms.ToolStripMenuItem("놀아주기 모드");
            playModeMenuItem.CheckOnClick = true;
            playModeMenuItem.CheckedChanged += (s, e) =>
            {
                _isPlayModeActive = playModeMenuItem.Checked;

                if (_isPlayModeActive)
                {
                    // 놀아주기 모드 시작: 메인 창은 숨기고 트레이로 보냅니다.
                    this.Hide();

                    if (MyPet != null && MyPet.Party != null && MyPet.Party.Count > 0)
                    {
                        // 설정된 최대 표시 수와 실제 파티원 수 중 작은 값을 선택
                        int targetCount = Math.Min(MyPet.Settings.RoamingPokemonCount, MyPet.Party.Count);

                        // 파티원 전체를 독립된 서브 창(드래그 및 작업표시줄 모드 지원)으로 소환합니다!
                        for (int i = 0; i < targetCount; i++)
                        {
                            SpawnPetWindow(MyPet.Party[i]);
                        }
                    }
                }
                else
                {
                    // 놀아주기 모드 종료 시 메인 창 복구 (서브 창들은 타이머 안에서 스스로 닫힙니다)
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
            };

            var settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem("설정");
            settingsMenuItem.Click += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var settingsWindow = new SettingsWindow();
                    if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow.IsVisible)
                    {
                        settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
                    }
                    settingsWindow.ShowDialog();
                });
            };

            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("완전히 종료하기");
            exitMenuItem.Click += (s, e) =>
            {
                var result = System.Windows.MessageBox.Show("정말로 다마포케를 종료하시겠습니까?", "다마포케 종료", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _notifyIcon?.Dispose();
                    this.Close();
                }
            };

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(playModeMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MyPet = PokemonState.Load();
            if (MyPet != null) MyPet.TrayNotificationRequested += ShowTrayNotification;

            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();

            IdleView startingView = new IdleView();
            startingView.DataContext = MyPet;
            NavigateTo(startingView);
        }

        // =========================================================================
        // 🌟 [통합 핵심 메서드] 모든 포켓몬(리더 포함)을 독립 서브 창으로 생성하고 움직이는 로직
        // =========================================================================
        private void SpawnPetWindow(PartyMember partyMember)
        {
            var petState = new PokemonState
            {
                SpeciesId = partyMember.SpeciesId,
                SpriteFileName = partyMember.SpriteFileName,
                IsShiny = partyMember.IsShiny,
                IsFreeRoaming = true
            };
            petState.StartSubPetAnimationOnly();

            Window petWindow = new Window
            {
                Width = PET_SIZE,
                Height = PET_SIZE,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                // 🌟 [핵심 추가] 알트탭 목록과 작업표시줄에 표시되지 않는 도구 창 스타일로 설정합니다.
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowActivated = false
            };

            System.Windows.Controls.Viewbox viewBox = new System.Windows.Controls.Viewbox
            {
                Stretch = System.Windows.Media.Stretch.Uniform,
                Child = new RoamingView { DataContext = petState }
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(viewBox, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
            System.Windows.Media.RenderOptions.SetEdgeMode(viewBox, System.Windows.Media.EdgeMode.Aliased);
            petWindow.Content = viewBox;

            bool isTaskbarMode = TamaPoke.Utils.SettingsManager.Load().IsTaskbarMode;
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;
            double fixedY = screenHeight - PET_SIZE;

            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            petWindow.Left = rnd.Next(0, (int)Math.Max(10, screenWidth - PET_SIZE));
            petWindow.Top = isTaskbarMode ? fixedY : rnd.Next(0, (int)Math.Max(10, screenHeight - PET_SIZE));

            petWindow.SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(petWindow).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW & ~WS_EX_APPWINDOW);
            };

            petWindow.Show();

            // 🌟 생성된 창 정보를 공유 리스트에 등록
            var currentPetInfo = new PetWindowInfo { WindowInstance = petWindow, State = petState };
            _activePets.Add(currentPetInfo);

            double targetX = rnd.Next(0, (int)Math.Max(10, screenWidth - PET_SIZE));
            double targetY = isTaskbarMode ? fixedY : rnd.Next(0, (int)Math.Max(10, screenHeight - PET_SIZE));

            int restCounter = 0;
            int behaviorStep = 0; // 0: 일반 이동, 1: 수면, 2: 땅파기, 3: 상호작용 중
            int interactionCooldown = 0; // 연속 상호작용 방지 쿨타임
            bool isDragging = false;

            DispatcherTimer moveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            petWindow.Closed += (s, e) =>
            {
                _activePets.Remove(currentPetInfo);
            };

            petWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    isDragging = true;
                    moveTimer.Stop();
                    behaviorStep = 0;
                    SafeSetAction(petState, PokemonState.ANIM_HURT, PokemonState.ANIM_IDLE, 9999);
                    petWindow.DragMove();
                }
            };

            petWindow.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    targetX = rnd.Next(0, (int)Math.Max(10, screenWidth - PET_SIZE));
                    targetY = isTaskbarMode ? fixedY : rnd.Next(0, (int)Math.Max(10, screenHeight - PET_SIZE));
                    SafeSetAction(petState, PokemonState.ANIM_WALK);
                    moveTimer.Start();
                }
            };

            moveTimer.Tick += (s, e) =>
            {
                if (!_isPlayModeActive)
                {
                    moveTimer.Stop();
                    petWindow.Close();
                    return;
                }

                if (interactionCooldown > 0) interactionCooldown--;

                // 🌟 특별 행동 또는 상호작용 진행 중 처리
                if (restCounter > 0)
                {
                    restCounter--;

                    if (behaviorStep == 1) // 수면
                    {
                        if (restCounter == 30) SafeSetAction(petState, PokemonState.ANIM_WAKE, PokemonState.ANIM_IDLE, 30);
                    }
                    else if (behaviorStep == 2) // 땅파기
                    {
                        if (restCounter == 60) SafeSetAction(petState, PokemonState.ANIM_LEAPFORTH, PokemonState.ANIM_IDLE, 60);
                        else if (restCounter == 30) SafeSetAction(petState, PokemonState.ANIM_HITGROUND, PokemonState.ANIM_IDLE, 30);
                    }

                    if (restCounter <= 0)
                    {
                        // 🌟 [핵심 개선] 상호작용(인사/티격태격) 직후였다면 서로 엉켜서 멈추지 않도록 각자 다른 방향으로 확실하게 멀어지게 만듭니다!
                        if (behaviorStep == 3)
                        {
                            double escapeAngle = rnd.NextDouble() * Math.PI * 2;
                            double escapeDistance = 120.0; // 상호작용 후 최소 120픽셀 이상 떨어진 곳으로 이동

                            targetX = Math.Clamp(petWindow.Left + Math.Cos(escapeAngle) * escapeDistance, 0, screenWidth - PET_SIZE);
                            targetY = isTaskbarMode ? fixedY : Math.Clamp(petWindow.Top + Math.Sin(escapeAngle) * escapeDistance, 0, screenHeight - PET_SIZE);
                        }
                        else
                        {
                            // 일반적인 목적지 도착 후 휴식 끝났을 때의 무작위 이동
                            targetX = rnd.Next(0, (int)Math.Max(10, screenWidth - PET_SIZE));
                            targetY = isTaskbarMode ? fixedY : rnd.Next(0, (int)Math.Max(10, screenHeight - PET_SIZE));
                        }

                        behaviorStep = 0;
                        SafeSetAction(petState, PokemonState.ANIM_WALK);
                    }
                    return;
                }

                // 🌟 [핵심 추가] 다른 포켓몬과의 근접 충돌(마주침) 감지 로직
                if (interactionCooldown <= 0 && behaviorStep == 0)
                {
                    foreach (var other in _activePets)
                    {
                        if (other == currentPetInfo) continue;

                        double diffX = other.WindowInstance.Left - petWindow.Left;
                        double diffY = other.WindowInstance.Top - petWindow.Top;
                        double distBetweenPets = Math.Sqrt(diffX * diffX + diffY * diffY);

                        // 두 포켓몬의 거리가 50픽셀 이내로 가까워졌을 때 마주침 발생!
                        if (distBetweenPets < 50.0)
                        {
                            behaviorStep = 3; // 상호작용 상태 진입
                            restCounter = 45; // 약 1.5초 동안 상호작용 모션 유지
                            interactionCooldown = 150; // 재발동 쿨타임 설정

                            // 서로 마주 보도록 방향 설정
                            petState.Direction = (diffX > 0) ? 2 : 6;

                            // 50% 확률로 '인사하기(ANIM_HOP)' 또는 '티격태격하기(ANIM_STRIKE)' 선택
                            int interactionType = rnd.Next(0, 2);
                            if (interactionType == 0)
                            {
                                SafeSetAction(petState, PokemonState.ANIM_HOP, PokemonState.ANIM_IDLE, 45);
                            }
                            else
                            {
                                SafeSetAction(petState, PokemonState.ANIM_STRIKE, PokemonState.ANIM_IDLE, 45);
                            }
                            return;
                        }
                    }
                }

                double dx = targetX - petWindow.Left;
                double dy = targetY - petWindow.Top;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                // 목적지 도착 시 행동 추첨
                if (distance < 5.0)
                {
                    petState.Direction = 0;
                    petState.FlipX = 1;

                    int actionRoll = rnd.Next(0, 100);

                    if (actionRoll < 18)
                    {
                        SafeSetAction(petState, PokemonState.ANIM_IDLE, PokemonState.ANIM_IDLE, 60);
                        restCounter = 60;
                    }
                    else if (actionRoll < 36)
                    {
                        SafeSetAction(petState, PokemonState.ANIM_POSE, PokemonState.ANIM_IDLE, 40);
                        restCounter = 40;
                    }
                    else if (actionRoll < 52)
                    {
                        SafeSetAction(petState, PokemonState.ANIM_DEEPBREATH, PokemonState.ANIM_IDLE, 60);
                        restCounter = 60;
                    }
                    else if (actionRoll < 68)
                    {
                        SafeSetAction(petState, PokemonState.ANIM_NOD, PokemonState.ANIM_IDLE, 60);
                        restCounter = 60;
                    }
                    else if (actionRoll < 84)
                    {
                        behaviorStep = 1;
                        SafeSetAction(petState, PokemonState.ANIM_SLEEP, PokemonState.ANIM_IDLE, 300);
                        restCounter = 300;
                    }
                    else
                    {
                        behaviorStep = 2;
                        SafeSetAction(petState, PokemonState.ANIM_SINK, PokemonState.ANIM_IDLE, 90);
                        restCounter = 90;
                    }
                }
                else
                {
                    double speed = BASE_ROAMING_SPEED;

                    petWindow.Left += (dx / distance) * speed;
                    petWindow.Top += (dy / distance) * speed;

                    if (isTaskbarMode)
                    {
                        petWindow.Top = fixedY;
                    }

                    if (isTaskbarMode)
                    {
                        petState.Direction = (dx > 0) ? 2 : 6;
                    }
                    else
                    {
                        double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                        if (angle < 0) angle += 360;

                        petState.FlipX = 1;
                        if (angle >= 337.5 || angle < 22.5) petState.Direction = 2;
                        else if (angle >= 22.5 && angle < 67.5) petState.Direction = 1;
                        else if (angle >= 67.5 && angle < 112.5) petState.Direction = 0;
                        else if (angle >= 112.5 && angle < 157.5) petState.Direction = 7;
                        else if (angle >= 157.5 && angle < 202.5) petState.Direction = 6;
                        else if (angle >= 202.5 && angle < 247.5) petState.Direction = 5;
                        else if (angle >= 247.5 && angle < 292.5) petState.Direction = 4;
                        else if (angle >= 292.5 && angle < 337.5) petState.Direction = 3;
                    }
                }
            };

            SafeSetAction(petState, PokemonState.ANIM_WALK);
            moveTimer.Start();
        }

        // 🌟 안전한 애니메이션 실행 헬퍼 메서드
        private void SafeSetAction(PokemonState petState, int animId, int fallbackAnim = PokemonState.ANIM_IDLE)
        {
            try
            {
                petState.SetPlayModeAction(animId);
            }
            catch
            {
                petState.SetPlayModeAction(fallbackAnim);
            }
        }

        // 🌟 2. 지속 시간(Duration)까지 함께 지정할 때 호출되는 오버로딩 메서드
        private void SafeSetAction(PokemonState petState, int animId, int fallbackAnim, int duration)
        {
            try
            {
                if (duration > 0)
                {
                    petState.SetPlayModeAction(animId, duration);
                }
                else
                {
                    petState.SetPlayModeAction(animId);
                }
            }
            catch
            {
                petState.SetPlayModeAction(fallbackAnim);
            }
        }

        private void ShowTrayNotification(string title, string message)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                if (_notifyIcon != null && _notifyIcon.Visible)
                {
                    _notifyIcon.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Info);
                }
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MyPet != null) MyPet.Save();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (MyPet != null) MyPet.Tick();
        }

        public void NavigateTo(System.Windows.Controls.UserControl view)
        {
            MainContentFrame.Content = view;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("정말로 다마포케를 종료하시겠습니까?", "다마포케 종료", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _notifyIcon?.Dispose();
                this.Close();
            }
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var border = button.Template.FindName("PinBorder", button) as System.Windows.Controls.Border;
                if (border != null)
                {
                    border.Background = Topmost
                        ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF2196F3"))
                        : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#80000000"));
                }
            }
        }
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // 놀아주기 모드가 아닐 때만 메인 창 드래그 이동을 허용합니다.
                if (!_isPlayModeActive)
                {
                    this.DragMove();
                }
            }
        }
    }
}