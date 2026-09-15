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
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
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
            petWindow.Show();

            double targetX = rnd.Next(0, (int)Math.Max(10, screenWidth - PET_SIZE));
            double targetY = isTaskbarMode ? fixedY : rnd.Next(0, (int)Math.Max(10, screenHeight - PET_SIZE));
            int restCounter = 0;
            bool isDragging = false;

            DispatcherTimer moveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };

            petWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    isDragging = true;
                    moveTimer.Stop();
                    petState.SetPlayModeAction(PokemonState.ANIM_HURT, 9999);
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
                    petState.SetPlayModeAction(PokemonState.ANIM_WALK);
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

                if (restCounter > 0)
                {
                    restCounter--;
                    if (restCounter <= 0)
                    {
                        targetX = rnd.Next(0, (int)Math.Max(10, screenWidth - PET_SIZE));
                        targetY = isTaskbarMode ? fixedY : rnd.Next(0, (int)Math.Max(10, screenHeight - PET_SIZE));
                        petState.SetPlayModeAction(PokemonState.ANIM_WALK);
                    }
                    return;
                }

                double dx = targetX - petWindow.Left;
                double dy = targetY - petWindow.Top;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance < 5.0)
                {
                    petState.Direction = 0;
                    petState.FlipX = 1;

                    int rndAction = rnd.Next(0, 100);
                    if (rndAction < 50) { petState.SetPlayModeAction(PokemonState.ANIM_IDLE, 60); restCounter = 60; }
                    else { petState.SetPlayModeAction(PokemonState.ANIM_POSE, 40); restCounter = 40; }
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

            petState.SetPlayModeAction(PokemonState.ANIM_WALK);
            moveTimer.Start();
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
    }
}