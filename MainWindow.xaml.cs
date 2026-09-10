using System;
using System.Security.Policy;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TamaPoke.Models;
using TamaPoke.Views;

namespace TamaPoke
{
    public partial class MainWindow : Window
    {
        public PokemonState? MyPet { get; set; }
        private DispatcherTimer? _gameTimer;

        // 🌟 1. null 허용 경고 해결을 위해 '?' 추가
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        public MainWindow()
        {
            try
            {
                // 1. 화면(XAML)을 그려냅니다. (이곳에서 에러가 나면 UI 요소나 리소스 이미지 문제입니다)
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // 진짜 에러 원인을 팝업으로 띄워줍니다!
                System.Windows.MessageBox.Show($"XAML 로드 에러: {ex.InnerException?.Message ?? ex.Message}", "에러 추적기");
            }

            try
            {
                // 2. 시스템 트레이를 설정합니다. (이곳에서 에러가 나면 아이콘 추출이나 권한 문제입니다)
                SetupSystemTray();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"트레이 설정 에러: {ex.Message}", "에러 추적기");
            }

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();

            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "monsterball.ico");

            if (System.IO.File.Exists(iconPath))
            {
                _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }

            _notifyIcon.Text = "다마포케 (TamaPoke)";
            _notifyIcon.Visible = true;

            // 트레이 아이콘 더블 클릭 시 창 띄우기
            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openMenuItem = new System.Windows.Forms.ToolStripMenuItem("다마포케 화면에 띄우기");
            openMenuItem.Click += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            // 🌟 1. 새로 추가할 '설정' 메뉴 아이템
            var settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem("설정");
            settingsMenuItem.Click += (s, e) =>
            {
                // UI 스레드 안전하게 설정 팝업창 띄우기
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var settingsWindow = new SettingsWindow();

                    // 메인 창이 열려있다면 오너로 지정하여 중앙에 예쁘게 띄우기
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
                var result = System.Windows.MessageBox.Show(
                    "정말로 다마포케를 종료하시겠습니까?",
                    "다마포케 종료",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _notifyIcon?.Dispose();
                    this.Close();
                }
            };

            // 🌟 2. 컨텍스트 메뉴에 순서대로 아이템 추가하기
            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(settingsMenuItem); // 설정 메뉴 장착!
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MyPet = PokemonState.Load();

            if (MyPet != null)
            {
                MyPet.TrayNotificationRequested += ShowTrayNotification;
            }

            _gameTimer = new DispatcherTimer();

            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();

            IdleView startingView = new IdleView();
            startingView.DataContext = MyPet;
            NavigateTo(startingView);
        }

        private void ShowTrayNotification(string title, string message)
        {
            if (_notifyIcon != null && _notifyIcon.Visible)
            {
                // 3000은 3초 동안 띄운다는 뜻입니다.
                _notifyIcon.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MyPet != null)
            {
                MyPet.Save();
            }
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (MyPet != null)
            {
                MyPet.Tick();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // 🌟 3. UserControl 모호성 해결 (WPF의 UserControl임을 명확히 지정)
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
            // 🌟 1. 사용자에게 종료 여부를 묻는 팝업창을 띄웁니다.
            var result = System.Windows.MessageBox.Show(
                "정말로 다마포케를 종료하시겠습니까?",
                "다마포케 종료",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            // 🌟 2. 사용자가 '예(Yes)'를 선택했을 때만 프로그램을 완전히 종료합니다.
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // 트레이 아이콘이 남아있지 않도록 메모리에서 비워줍니다.
                _notifyIcon?.Dispose();

                // 창을 닫습니다. (MainWindow_Closing이 호출되며 자동 저장됩니다!)
                this.Close();
            }
            // '아니요(No)'를 누르면 아무 일도 일어나지 않고 게임으로 돌아갑니다.
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;

            // 🌟 4. Button 및 Border 모호성 해결
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                var border = button.Template.FindName("PinBorder", button) as System.Windows.Controls.Border;
                if (border != null)
                {
                    if (Topmost)
                    {
                        // 🌟 5. Color 및 Brush 모호성 해결
                        border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF2196F3"));
                    }
                    else
                    {
                        border.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#80000000"));
                    }
                }
            }
        }

        // 🌟 개발자용 임시 에셋 변환 클릭 이벤트 (작업이 끝나면 지워주세요!)
        private void DevConvertButton_Click(object sender, RoutedEventArgs e)
        {
            // 주의: 따옴표 안의 폴더 경로는 실제 PC에 있는 경로로 꼭 바꿔주세요!
            string sourceFolder = @"D:\sprite";
            string targetFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Resource", "PokemonSprites_ex");
            string trackerJsonPath = @"D:\sprite\tracker.json";
            System.Windows.MessageBox.Show("변환을 시작합니다. 콘솔 창이나 출력 창을 확인해 주세요!", "개발자 도구");

            // 변환 실행!
            TamaPoke.Utils.SpriteConverter.BatchConvertAll(sourceFolder, targetFolder, trackerJsonPath);

            System.Windows.MessageBox.Show("변환이 완료되었습니다! 폴더를 확인해 보세요.", "개발자 도구");
        }
    }
}