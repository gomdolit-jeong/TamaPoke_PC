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
        public PokemonState? MyPet { get; set; }
        private DispatcherTimer? _gameTimer;

        // 🌟 1. null 허용 경고 해결을 위해 '?' 추가
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            SetupSystemTray();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();

            // 프로그램의 기본 실행 파일(.exe) 아이콘을 그대로 가져와서 트레이 아이콘으로 씁니다!
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _notifyIcon.Text = "다마포케 (TamaPoke)";
            _notifyIcon.Visible = true;

            // 트레이 아이콘 더블 클릭 시 이벤트
            _notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var openMenuItem = new System.Windows.Forms.ToolStripMenuItem("화면에 띄우기");
            openMenuItem.Click += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("완전히 종료하기");
            exitMenuItem.Click += (s, e) =>
            {
                // 🌟 1. 트레이 아이콘에서 종료할 때도 똑같이 확인창을 띄워줍니다!
                var result = System.Windows.MessageBox.Show(
                    "정말로 다마포케를 종료하시겠습니까?",
                    "다마포케 종료",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _notifyIcon?.Dispose();

                    // 🌟 2. 강제 종료(Shutdown) 대신 Close()를 호출합니다.
                    // 이렇게 하면 MainWindow_Closing 이벤트가 정상적으로 실행되어 MyPet.Save()가 작동합니다!
                    this.Close();
                }
            };

            contextMenu.Items.Add(openMenuItem);
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
    }
}