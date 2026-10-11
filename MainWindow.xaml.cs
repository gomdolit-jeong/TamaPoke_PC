using System;
using System.Collections.Generic;
using System.Linq;
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
        public PokemonState? MyPet { get; set; }
        private DispatcherTimer? _gameTimer;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        // 산책 모드 상태 관리 변수
        private bool _isWalkModeActive = false;
        private System.Windows.Forms.ToolStripMenuItem? _walkModeMenuItem;
        private readonly List<RoamingPet> _activePets = new();

        public MainWindow()
        {
            try { InitializeComponent(); } catch (Exception ex) { System.Windows.MessageBox.Show($"XAML 로드 에러: {ex.InnerException?.Message ?? ex.Message}", "에러 추적기"); }
            try { SetupSystemTray(); } catch (Exception ex) { System.Windows.MessageBox.Show($"트레이 설정 에러: {ex.Message}", "에러 추적기"); }

            this.Topmost = false;

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            string iconPath = System.IO.Path.Combine(AppPaths.ResourceFolder, "monsterball.ico");

            if (System.IO.File.Exists(iconPath)) _notifyIcon.Icon = new System.Drawing.Icon(iconPath);

            _notifyIcon.Text = "다마포케 (TamaPoke)";
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openMenuItem = new System.Windows.Forms.ToolStripMenuItem("다마포케 화면에 띄우기");
            openMenuItem.Click += (s, e) => ShowMainWindow();

            // 🌟 산책 모드 메뉴 설정
            _walkModeMenuItem = new System.Windows.Forms.ToolStripMenuItem("산책 모드");
            _walkModeMenuItem.ShortcutKeyDisplayString = "Ctrl+W";
            _walkModeMenuItem.CheckOnClick = true;
            _walkModeMenuItem.CheckedChanged += (s, e) =>
            {
                _isWalkModeActive = _walkModeMenuItem.Checked;
                if (_isWalkModeActive) StartWalk(); else StopWalk();
            };

            var settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem("게임 설정");
            settingsMenuItem.ShortcutKeyDisplayString = "Ctrl+O"; // 👈 우측에 단축키 텍스트를 예쁘게 띄워줍니다.
            settingsMenuItem.Click += (s, e) => OpenSettingsWindow();

            // 🌟 [통합 업데이트 메뉴] 하나의 버튼으로 두 기능을 순차적으로 실행합니다!
            var updateMenuItem = new System.Windows.Forms.ToolStripMenuItem("다마포케 통합 업데이트");
            updateMenuItem.ShortcutKeyDisplayString = "Ctrl+U"; // 👈 우측에 단축키 텍스트를 예쁘게 띄워줍니다.
            updateMenuItem.Click += (s, e) => OpenUpdateWindow();

            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("완전히 종료하기");
            exitMenuItem.Click += (s, e) => ConfirmAndExit();

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(_walkModeMenuItem);
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
            MyPet = PokemonState.Load();
            MyPet.TrayNotificationRequested += ShowTrayNotification;
            MyPet.WalkItemFound += OnWalkItemFound;

            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();

            ShowIdleView(MyPet);
        }

        // ==========================================
        // 🌟 화면 전환 / 공통 창 열기
        // ==========================================
        public void NavigateTo(System.Windows.Controls.UserControl view)
        {
            MainContentFrame.Content = view;
        }

        // 메인(대기) 화면으로 돌아갑니다. (미니게임 종료, 공장 초기화 등)
        public void ShowIdleView(PokemonState pet)
        {
            NavigateTo(new IdleView { DataContext = pet });
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        // 메인 창이 보일 때만 소유자로 지정합니다. (산책 모드 중에는 메인 창이 숨겨져 있음)
        private void ShowDialogOverMain(Window dialog)
        {
            if (this.IsVisible) dialog.Owner = this;
            dialog.ShowDialog();
        }

        public void OpenSettingsWindow() => Dispatcher.Invoke(() => ShowDialogOverMain(new SettingsWindow()));
        public void OpenUpdateWindow() => Dispatcher.Invoke(() => ShowDialogOverMain(new UpdateProgressWindow()));

        public void ConfirmAndExit()
        {
            var result = System.Windows.MessageBox.Show("정말로 다마포케를 종료하시겠습니까?", "다마포케 종료", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            _notifyIcon?.Dispose();
            CloseAllRoamingPets();
            System.Windows.Application.Current.Shutdown();
        }

        // 단축키: Ctrl+O 설정, Ctrl+U 업데이트, Ctrl+W 산책 (메인 대기 화면에서만)
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control || MainContentFrame.Content is not IdleView) return;

            switch (e.Key)
            {
                case Key.O: OpenSettingsWindow(); break;
                case Key.U: OpenUpdateWindow(); break;
                case Key.W: ToggleWalkMode(); break;
            }
        }

        // ==========================================
        // 🌟 산책 모드
        // ==========================================
        public void ToggleWalkMode()
        {
            // 메뉴의 체크 상태를 반대로 뒤집으면 자동으로 CheckedChanged 이벤트가 발동합니다.
            if (_walkModeMenuItem != null) _walkModeMenuItem.Checked = !_walkModeMenuItem.Checked;
        }

        private void StartWalk()
        {
            this.Hide();
            if (MyPet == null) return;

            MyPet.BeginWalk();

            var settings = SettingsManager.Current;
            int spawnCount = Math.Min(settings.RoamingPokemonCount, MyPet.Party.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                var pet = new RoamingPet(MyPet.Party[i], _activePets, settings);
                pet.ToggleWalkRequested += ToggleWalkMode;
                pet.Closed += p => _activePets.Remove(p);
                _activePets.Add(pet);
                pet.Show();
            }
        }

        private void StopWalk()
        {
            CloseAllRoamingPets();
            MyPet?.EndWalk(); // 돌아오면 메인 화면에 산책 요약을 보여줍니다.
            ShowMainWindow();
        }

        private void CloseAllRoamingPets()
        {
            foreach (var pet in _activePets.ToArray()) pet.Close();
            _activePets.Clear();
        }

        // 산책 중 아이템 발견: 바탕화면의 펫 하나가 풀숲을 뒤적여서 찾아낸 것처럼 연출하고 트레이 알림을 보냅니다.
        private void OnWalkItemFound(ItemDefinition item)
        {
            if (_activePets.Count > 0)
            {
                // 이미 뒤적이기 대기 중인 펫은 피해서, 연출이 겹쳐 하나가 묻히지 않게 합니다.
                var candidates = _activePets.Where(p => p.PendingFoundItem == null).ToList();
                if (candidates.Count == 0) candidates = _activePets;
                candidates[new Random().Next(candidates.Count)].PendingFoundItem = item;
            }

            if (MyPet != null && MyPet.Settings.UseTrayNotifications && _notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(3000, "산책 중 발견!", $"{item.Name}을(를) 주웠어요!", System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        // 공장 초기화: 산책 모드를 끄고, 데이터를 초기화한 뒤 메인 화면(알)으로 돌아옵니다.
        // 배틀/미니게임/이별 연출 중에는 진행 중인 처리와 충돌하므로 false를 반환합니다.
        public bool TryFactoryReset()
        {
            if (MyPet == null || !MyPet.CanFactoryReset) return false;

            if (_walkModeMenuItem != null && _walkModeMenuItem.Checked)
            {
                _walkModeMenuItem.Checked = false; // 산책 펫 창 정리 + 메인 창 복귀
            }

            MyPet.FactoryReset();
            ShowIdleView(MyPet);
            return true;
        }

        // ==========================================
        // 🌟 창 이벤트
        // ==========================================
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !_isWalkModeActive) this.DragMove();
        }

        private void ShowTrayNotification(string title, string message)
        {
            if (this.WindowState == WindowState.Minimized && _notifyIcon != null && _notifyIcon.Visible)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            MyPet?.Save();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            MyPet?.Tick();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => ConfirmAndExit();

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            if (sender is System.Windows.Controls.Button button &&
                button.Template.FindName("PinBorder", button) is System.Windows.Controls.Border border)
            {
                border.Background = BrushCache.Get(Topmost ? "#FF2196F3" : "#80000000");
            }
        }
    }
}
