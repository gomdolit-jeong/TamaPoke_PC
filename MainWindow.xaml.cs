using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TamaPoke.Models;
using TamaPoke.Views;
using TamaPoke.Utils; // 🌟 Updater 유틸리티들을 사용하기 위한 네임스페이스

namespace TamaPoke
{
    public partial class MainWindow : Window
    {
        // ==========================================
        // 🌟 알트탭 및 작업표시줄 제어를 위한 Win32 API 선언
        // ==========================================
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
        private readonly double BASE_ROAMING_SPEED = 3.0;

        public PokemonState? MyPet { get; set; }
        private DispatcherTimer? _gameTimer;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        // 산책 모드 상태 관리 변수
        private bool _isWalkModeActive = false;

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

            this.Topmost = false;

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

            // 🌟 산책 모드 메뉴 설정
            var walkModeMenuItem = new System.Windows.Forms.ToolStripMenuItem("산책 모드");
            walkModeMenuItem.CheckOnClick = true;
            walkModeMenuItem.CheckedChanged += (s, e) =>
            {
                _isWalkModeActive = walkModeMenuItem.Checked;

                if (_isWalkModeActive)
                {
                    this.Hide();

                    if (MyPet != null)
                    {
                        MyPet.IsFreeRoaming = true;
                    }

                    var currentSettings = TamaPoke.Utils.SettingsManager.Load();
                    int targetCount = currentSettings != null ? currentSettings.RoamingPokemonCount : 1;

                    if (MyPet != null && MyPet.Party != null && MyPet.Party.Count > 0)
                    {
                        int spawnCount = Math.Min(targetCount, MyPet.Party.Count);

                        for (int i = 0; i < spawnCount; i++)
                        {
                            SpawnPetWindow(MyPet.Party[i]);
                        }
                    }
                }
                else
                {
                    foreach (var pet in _activePets.ToArray())
                    {
                        pet.WindowInstance?.Close();
                    }
                    _activePets.Clear();

                    if (MyPet != null)
                    {
                        MyPet.IsFreeRoaming = false;
                        MyPet.ResetPosition();
                    }

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

            // =========================================================================
            // 🌟 [통합 업데이트 메뉴] 하나의 버튼으로 두 기능을 순차적으로 실행합니다!
            // =========================================================================
            var updateMenuItem = new System.Windows.Forms.ToolStripMenuItem("다마포케 통합 업데이트");
            updateMenuItem.Click += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var updateWindow = new UpdateProgressWindow();
                    if (System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow.IsVisible)
                    {
                        updateWindow.Owner = System.Windows.Application.Current.MainWindow;
                    }
                    updateWindow.ShowDialog();
                });
            };
            // =========================================================================

            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("완전히 종료하기");
            exitMenuItem.Click += (s, e) =>
            {
                var result = System.Windows.MessageBox.Show("정말로 다마포케를 종료하시겠습니까?", "다마포케 종료", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _notifyIcon?.Dispose();
                    foreach (var pet in _activePets.ToArray())
                    {
                        pet.WindowInstance?.Close();
                    }
                    _activePets.Clear();
                    System.Windows.Application.Current.Shutdown();
                }
            };

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(walkModeMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(updateMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SkillDex.LoadSkillData();
            PokemonDex.LoadPokemonData();
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

            var gameSettings = TamaPoke.Utils.SettingsManager.Load();
            double rawScale = (gameSettings != null && gameSettings.RoamingPetScale > 0) ? gameSettings.RoamingPetScale : 2.0;
            double currentScale = Math.Min(rawScale, 3.0);

            double baseWidth = 100.0;
            double baseHeight = 100.0;
            double petWidth = baseWidth * currentScale;
            double petHeight = baseHeight * currentScale;

            Window petWindow = new Window
            {
                Width = petWidth,
                Height = petHeight,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowActivated = false,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            petWindow.SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(petWindow).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW & ~WS_EX_APPWINDOW);
            };

            var roamingView = new RoamingView
            {
                DataContext = petState,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Bottom
            };
            roamingView.LayoutTransform = new System.Windows.Media.ScaleTransform(currentScale, currentScale);

            System.Windows.Controls.Grid containerGrid = new System.Windows.Controls.Grid
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(containerGrid, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
            System.Windows.Media.RenderOptions.SetEdgeMode(containerGrid, System.Windows.Media.EdgeMode.Aliased);
            containerGrid.Children.Add(roamingView);

            petWindow.Content = containerGrid;

            bool isTaskbarMode = (gameSettings != null) ? gameSettings.IsTaskbarMode : false;
            Random rnd = new Random(Guid.NewGuid().GetHashCode());

            (double x, double y) GetTargetOnCurrentScreen(bool taskbarMode)
            {
                int centerX = (int)(petWindow.Left + (petWidth / 2));
                int centerY = (int)(petWindow.Top + (petHeight / 2));
                var currentScreen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(centerX, centerY));

                double sMinX = currentScreen.Bounds.Left;
                double sMinY = currentScreen.Bounds.Top;
                double sMaxX = currentScreen.Bounds.Right;
                double sMaxY = currentScreen.Bounds.Bottom;

                double sFixedY = currentScreen.WorkingArea.Bottom - petHeight;

                double tX = rnd.Next((int)sMinX, (int)Math.Max(sMinX + 10, sMaxX - petWidth));
                double tY = taskbarMode ? sFixedY : rnd.Next((int)sMinY, (int)Math.Max(sMinY + 10, sMaxY - petHeight));
                return (tX, tY);
            }

            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                petWindow.Left = rnd.Next(primaryScreen.Bounds.Left, primaryScreen.Bounds.Right - (int)petWidth);
                petWindow.Top = isTaskbarMode ? primaryScreen.WorkingArea.Bottom - petHeight : rnd.Next(primaryScreen.Bounds.Top, primaryScreen.Bounds.Bottom - (int)petHeight);
            }
            petWindow.Show();

            var currentPetInfo = new PetWindowInfo { WindowInstance = petWindow, State = petState };
            _activePets.Add(currentPetInfo);

            var targetPos = GetTargetOnCurrentScreen(isTaskbarMode);
            double targetX = targetPos.x;
            double targetY = targetPos.y;

            int restCounter = 0;
            int behaviorStep = 0;
            int interactionCooldown = 0;

            double speedVariation = 0.8 + (rnd.NextDouble() * 0.4);
            double currentPetSpeed = BASE_ROAMING_SPEED * speedVariation;

            DispatcherTimer moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };

            petWindow.Closed += (s, e) => { _activePets.Remove(currentPetInfo); };

            petWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    moveTimer.Stop();
                    behaviorStep = 0;
                    SafeSetAction(petState, PokemonState.ANIM_HURT, PokemonState.ANIM_IDLE, 9999);

                    try { petWindow.DragMove(); } catch { }

                    var newTarget = GetTargetOnCurrentScreen(isTaskbarMode);
                    targetX = newTarget.x;
                    targetY = newTarget.y;

                    SafeSetAction(petState, PokemonState.ANIM_WALK);
                    moveTimer.Start();
                }
            };

            moveTimer.Tick += (s, e) =>
            {
                if (!_isWalkModeActive)
                {
                    moveTimer.Stop();
                    petWindow.Close();
                    return;
                }

                if (interactionCooldown > 0) interactionCooldown--;

                if (restCounter > 0)
                {
                    restCounter--;

                    if (behaviorStep == 1) { if (restCounter == 30) SafeSetAction(petState, PokemonState.ANIM_WAKE, PokemonState.ANIM_IDLE, 30); }
                    else if (behaviorStep == 2)
                    {
                        if (restCounter == 60) SafeSetAction(petState, PokemonState.ANIM_LEAPFORTH, PokemonState.ANIM_IDLE, 60);
                        else if (restCounter == 30) SafeSetAction(petState, PokemonState.ANIM_HITGROUND, PokemonState.ANIM_IDLE, 30);
                    }

                    if (restCounter <= 0)
                    {
                        if (behaviorStep == 3)
                        {
                            double escapeAngle = rnd.NextDouble() * Math.PI * 2;
                            double escapeDistance = 120.0;

                            int centerX = (int)(petWindow.Left + (petWidth / 2));
                            int centerY = (int)(petWindow.Top + (petHeight / 2));
                            var currentScreen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(centerX, centerY));

                            double cMinX = currentScreen.Bounds.Left;
                            double cMinY = currentScreen.Bounds.Top;
                            double cMaxX = currentScreen.Bounds.Right;
                            double cMaxY = currentScreen.Bounds.Bottom;
                            double cFixedY = currentScreen.WorkingArea.Bottom - petHeight;

                            targetX = Math.Clamp(petWindow.Left + Math.Cos(escapeAngle) * escapeDistance, cMinX, cMaxX - petWidth);
                            targetY = isTaskbarMode ? cFixedY : Math.Clamp(petWindow.Top + Math.Sin(escapeAngle) * escapeDistance, cMinY, cMaxY - petHeight);
                        }
                        else
                        {
                            var nextTarget = GetTargetOnCurrentScreen(isTaskbarMode);
                            targetX = nextTarget.x;
                            targetY = nextTarget.y;
                        }

                        behaviorStep = 0;
                        SafeSetAction(petState, PokemonState.ANIM_WALK);
                    }
                    return;
                }

                if (interactionCooldown <= 0 && behaviorStep == 0)
                {
                    foreach (var other in _activePets)
                    {
                        if (other == currentPetInfo) continue;

                        double diffX = other.WindowInstance.Left - petWindow.Left;
                        double diffY = other.WindowInstance.Top - petWindow.Top;
                        double distBetweenPets = Math.Sqrt(diffX * diffX + diffY * diffY);

                        if (distBetweenPets < (50.0 * currentScale))
                        {
                            behaviorStep = 3;
                            restCounter = 45;
                            interactionCooldown = 150;

                            petState.Direction = (diffX > 0) ? 2 : 6;

                            int interactionType = rnd.Next(0, 2);
                            if (interactionType == 0) SafeSetAction(petState, PokemonState.ANIM_HOP, PokemonState.ANIM_IDLE, 45);
                            else SafeSetAction(petState, PokemonState.ANIM_STRIKE, PokemonState.ANIM_IDLE, 45);
                            return;
                        }
                    }
                }

                double dx = targetX - petWindow.Left;
                double dy = targetY - petWindow.Top;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 5.0)
                {
                    petState.Direction = 0;
                    petState.FlipX = 1;

                    int actionRoll = rnd.Next(0, 100);

                    if (actionRoll < 18) { SafeSetAction(petState, PokemonState.ANIM_IDLE, PokemonState.ANIM_IDLE, 60); restCounter = 60; }
                    else if (actionRoll < 36) { SafeSetAction(petState, PokemonState.ANIM_POSE, PokemonState.ANIM_IDLE, 40); restCounter = 40; }
                    else if (actionRoll < 52) { SafeSetAction(petState, PokemonState.ANIM_DEEPBREATH, PokemonState.ANIM_IDLE, 60); restCounter = 60; }
                    else if (actionRoll < 68) { SafeSetAction(petState, PokemonState.ANIM_NOD, PokemonState.ANIM_IDLE, 60); restCounter = 60; }
                    else if (actionRoll < 84) { behaviorStep = 1; SafeSetAction(petState, PokemonState.ANIM_SLEEP, PokemonState.ANIM_IDLE, 300); restCounter = 300; }
                    else { behaviorStep = 2; SafeSetAction(petState, PokemonState.ANIM_SINK, PokemonState.ANIM_IDLE, 90); restCounter = 90; }
                }
                else
                {
                    double speed = currentPetSpeed;

                    petWindow.Left += (dx / distance) * speed;
                    petWindow.Top += (dy / distance) * speed;

                    if (isTaskbarMode)
                    {
                        var currentScreen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)petWindow.Left, (int)petWindow.Top));
                        petWindow.Top = currentScreen.WorkingArea.Bottom - petHeight;
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

        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (!_isWalkModeActive)
                {
                    this.DragMove();
                }
            }
        }

        private void SafeSetAction(PokemonState petState, int animId, int fallbackAnim = PokemonState.ANIM_IDLE)
        {
            try { petState.SetPlayModeAction(animId); }
            catch { petState.SetPlayModeAction(fallbackAnim); }
        }

        private void SafeSetAction(PokemonState petState, int animId, int fallbackAnim, int duration)
        {
            try
            {
                if (duration > 0) petState.SetPlayModeAction(animId, duration);
                else petState.SetPlayModeAction(animId);
            }
            catch { petState.SetPlayModeAction(fallbackAnim); }
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
                foreach (var pet in _activePets.ToArray())
                {
                    pet.WindowInstance?.Close();
                }
                _activePets.Clear();
                System.Windows.Application.Current.Shutdown();
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
    }
}