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
            try { InitializeComponent(); } catch (Exception ex) { System.Windows.MessageBox.Show(Loc.F("app.error.xaml", ex.InnerException?.Message ?? ex.Message), Loc.T("app.error.title")); }
            try { SetupSystemTray(); } catch (Exception ex) { System.Windows.MessageBox.Show(Loc.F("app.error.tray", ex.Message), Loc.T("app.error.title")); }

            this.Topmost = false;

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.KeyDown += MainWindow_KeyDown;

            // 언어가 바뀌면 트레이 메뉴와 코드에서 만든 문구(기분 등)를 다시 그립니다.
            Loc.LanguageChanged += () =>
            {
                ApplyTrayTexts();
                MyPet?.RefreshLocalizedTexts();
            };
        }

        private System.Windows.Forms.ToolStripMenuItem? _openMenuItem, _settingsMenuItem, _manualMenuItem, _updateMenuItem, _exitMenuItem;

        // 사용 설명서 (GitHub에서 보면 표와 이미지가 제대로 보이므로 브라우저로 엽니다)
        private const string MANUAL_URL_KO = "https://github.com/gomdolit-jeong/TamaPoke_PC/blob/main/MANUAL.ko.md";
        private const string MANUAL_URL_EN = "https://github.com/gomdolit-jeong/TamaPoke_PC/blob/main/MANUAL.md";

        private void ApplyTrayTexts()
        {
            if (_notifyIcon == null) return;
            _notifyIcon.Text = Loc.T("app.name");
            if (_openMenuItem != null) _openMenuItem.Text = Loc.T("tray.open");
            if (_walkModeMenuItem != null) _walkModeMenuItem.Text = Loc.T("tray.walk");
            if (_settingsMenuItem != null) _settingsMenuItem.Text = Loc.T("tray.settings");
            if (_manualMenuItem != null) _manualMenuItem.Text = Loc.T("tray.manual");
            if (_updateMenuItem != null) _updateMenuItem.Text = Loc.T("tray.update");
            if (_exitMenuItem != null) _exitMenuItem.Text = Loc.T("tray.exit");
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            string iconPath = System.IO.Path.Combine(AppPaths.ResourceFolder, "monsterball.ico");

            if (System.IO.File.Exists(iconPath)) _notifyIcon.Icon = new System.Drawing.Icon(iconPath);


            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => BringBackToMain();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openMenuItem = _openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openMenuItem.Click += (s, e) => BringBackToMain();

            // 🌟 산책 모드 메뉴 설정
            _walkModeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            _walkModeMenuItem.ShortcutKeyDisplayString = "Ctrl+W";
            _walkModeMenuItem.Click += (s, e) => ToggleWalkMode();

            var settingsMenuItem = _settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            settingsMenuItem.ShortcutKeyDisplayString = "Ctrl+O"; // 👈 우측에 단축키 텍스트를 예쁘게 띄워줍니다.
            settingsMenuItem.Click += (s, e) => OpenSettingsWindow();

            var manualMenuItem = _manualMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            manualMenuItem.Click += (s, e) => OpenManual();

            // 🌟 [통합 업데이트 메뉴] 하나의 버튼으로 두 기능을 순차적으로 실행합니다!
            var updateMenuItem = _updateMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            updateMenuItem.ShortcutKeyDisplayString = "Ctrl+U"; // 👈 우측에 단축키 텍스트를 예쁘게 띄워줍니다.
            updateMenuItem.Click += (s, e) => OpenUpdateWindow();

            var exitMenuItem = _exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitMenuItem.Click += (s, e) => ConfirmAndExit();

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(_walkModeMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(manualMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(updateMenuItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            ApplyTrayTexts();
        }

        // 지금 언어에 맞는 사용 설명서를 기본 브라우저로 엽니다.
        private void OpenManual()
        {
            string url = Loc.Instance.IsEnglish ? MANUAL_URL_EN : MANUAL_URL_KO;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(Loc.F("tray.manual.error", url, ex.Message), Loc.T("app.name"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

            // 화면이 뜬 뒤에 세이브/데이터 이상을 안내합니다.
            Dispatcher.BeginInvoke(ShowStartupWarnings, DispatcherPriority.ApplicationIdle);
        }

        // 세이브 복구/손상, 필요한 리소스(Data 파일, 포켓몬 이미지)가 없거나 깨진 경우를 알립니다.
        private void ShowStartupWarnings()
        {
            string corruptName = PokemonState.CorruptSaveFileName ?? "-";
            if (PokemonState.LastLoadResult == PokemonState.SaveLoadResult.RestoredFromBackup)
                System.Windows.MessageBox.Show(this, Loc.F("save.restored", corruptName), Loc.T("common.notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
            else if (PokemonState.LastLoadResult == PokemonState.SaveLoadResult.Corrupted)
                System.Windows.MessageBox.Show(this, Loc.F("save.corrupted", corruptName), Loc.T("common.error"), MessageBoxButton.OK, MessageBoxImage.Error);

            var broken = ResourceCheck.FindBrokenDataFiles();
            var sprites = ResourceCheck.GetSpriteState(out int present, out int expected);
            if (broken.Count == 0 && sprites == ResourceCheck.SpriteState.Ok) return;

            var lines = new List<string>(broken);
            if (sprites == ResourceCheck.SpriteState.None) lines.Add(Loc.T("resource.sprites.none"));
            else if (sprites == ResourceCheck.SpriteState.Partial) lines.Add(Loc.F("resource.sprites.partial", present, expected));
            string list = string.Join("\n", lines.Select(l => "• " + l));

            // 세이브도 이미지도 없으면 처음 실행으로 보고 환영 안내를 띄웁니다.
            bool firstRun = PokemonState.LastLoadResult == PokemonState.SaveLoadResult.NewGame && sprites == ResourceCheck.SpriteState.None;
            var result = firstRun
                ? System.Windows.MessageBox.Show(this, Loc.F("resource.firstRun", list), Loc.T("common.notice"), MessageBoxButton.YesNo, MessageBoxImage.Information)
                : System.Windows.MessageBox.Show(this, Loc.F("resource.missing", list), Loc.T("common.error"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            bool skillBroken = broken.Contains("pokemon_movedb.json") || broken.Contains("pokemon_skills.json");
            OpenUpdateWindow(broken.Contains("pokemon_data.json"), skillBroken, sprites != ResourceCheck.SpriteState.Ok);
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

        public void OpenSettingsWindow() => Dispatcher.Invoke(() =>
        {
            ShowDialogOverMain(new SettingsWindow());
            ApplyWalkSettings(); // 산책 중이면 바뀐 마릿수/크기를 바로 반영
        });
        public void OpenUpdateWindow() => Dispatcher.Invoke(() => ShowDialogOverMain(new UpdateProgressWindow()));
        public void OpenUpdateWindow(bool data, bool skill, bool sprite) => Dispatcher.Invoke(() => ShowDialogOverMain(new UpdateProgressWindow(data, skill, sprite)));

        public void ConfirmAndExit()
        {
            var result = System.Windows.MessageBox.Show(Loc.T("exit.confirm.message"), Loc.T("exit.confirm.title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
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
            if (_isWalkModeActive) StopWalk();
            else StartWalk();
        }

        // 산책 중이면 산책을 끝내고 메인 화면으로, 아니면 메인 창만 앞으로 (트레이 "화면에 띄우기")
        private void BringBackToMain()
        {
            if (_isWalkModeActive) StopWalk();
            else ShowMainWindow();
        }

        private void StartWalk()
        {
            if (MyPet == null || _isWalkModeActive) return;

            // 배틀/미니게임/진화·이별 연출 중에는 진행 중인 처리와 겹치므로 나가지 않습니다.
            if (!MyPet.CanStartWalk)
            {
                string message = Loc.T("walk.cannot");
                if (IsVisible) _ = MyPet.ShowEventMessageAsync(message);
                else _notifyIcon?.ShowBalloonTip(3000, Loc.T("tray.walk"), message, System.Windows.Forms.ToolTipIcon.Info);
                return;
            }

            _isWalkModeActive = true;
            if (_walkModeMenuItem != null) _walkModeMenuItem.Checked = true;
            this.Hide();

            MyPet.BeginWalk();

            var settings = SettingsManager.Current;
            int spawnCount = Math.Min(settings.RoamingPokemonCount, MyPet.Party.Count);
            for (int i = 0; i < spawnCount; i++) SpawnRoamingPet(i, settings);
        }

        private void SpawnRoamingPet(int partyIndex, GameSettings settings)
        {
            var member = MyPet!.Party[partyIndex];
            var pet = new RoamingPet(member, partyIndex, _activePets, settings)
            {
                MegaForms = MyPet.GetUsableMegaForms(member.SpeciesId), // 산책 중 가끔 메가진화 (메가링 + 스톤 + 이미지)
            };
            pet.ToggleWalkRequested += ToggleWalkMode;
            pet.Closed += p => _activePets.Remove(p);
            _activePets.Add(pet);
            pet.Show();
        }

        // 산책 중 설정을 저장하면: 크기/작업표시줄 모드 반영, 마릿수가 늘면 다음 파티 멤버가 나오고 줄면 뒤쪽부터 들어갑니다.
        private void ApplyWalkSettings()
        {
            if (!_isWalkModeActive || MyPet == null) return;

            var settings = SettingsManager.Current;
            foreach (var pet in _activePets) pet.ApplySettings(settings);

            int want = Math.Min(settings.RoamingPokemonCount, MyPet.Party.Count);
            foreach (var pet in _activePets.OrderByDescending(p => p.PartyIndex).Take(Math.Max(0, _activePets.Count - want)).ToList())
                pet.Close();

            for (int i = 0; i < MyPet.Party.Count && _activePets.Count < want; i++)
            {
                if (!_activePets.Any(p => p.PartyIndex == i)) SpawnRoamingPet(i, settings);
            }
        }

        private void StopWalk()
        {
            if (!_isWalkModeActive) return;
            _isWalkModeActive = false;
            if (_walkModeMenuItem != null) _walkModeMenuItem.Checked = false;

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
                _notifyIcon.ShowBalloonTip(3000, Loc.T("walk.found.title"), Loc.F("walk.found.message", item.Name), System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        // 공장 초기화: 산책 모드를 끄고, 데이터를 초기화한 뒤 메인 화면(알)으로 돌아옵니다.
        // 배틀/미니게임/이별 연출 중에는 진행 중인 처리와 충돌하므로 false를 반환합니다.
        public bool TryFactoryReset()
        {
            if (MyPet == null || !MyPet.CanFactoryReset) return false;

            StopWalk(); // 산책 중이면 산책 펫 창 정리 + 메인 창 복귀

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
