using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TamaPoke.Models;
using TamaPoke.Views;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace TamaPoke
{
    public partial class MainWindow : Window
    {
        public PokemonState? MyPet { get; set; }
        private DispatcherTimer? _gameTimer;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        // 🌟 창이 화면에 나타난 직후 실행되는 함수 (데이터 로드 & 타이머 시작)
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 🌟 수정됨: Load() 함수가 알아서 파일이 있으면 불러오고, 없으면 새 알을 만들어줍니다.
            // 지역 변수를 새로 만들지 않고, 뷰모델인 MyPet에 바로 연결합니다.
            MyPet = PokemonState.Load();

            // 게임 1초 타이머 설정 및 시작
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();

            // IdleView 화면을 생성하고, 로드된 MyPet 데이터를 연결(Binding)하여 화면을 전환합니다.
            IdleView startingView = new IdleView();
            startingView.DataContext = MyPet;
            NavigateTo(startingView);
        }

        // 프로그램이 종료되기 직전에 실행되는 함수입니다.
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

        public void NavigateTo(UserControl view)
        {
            MainContentFrame.Content = view;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Topmost 상태 토글
            Topmost = !Topmost;

            // 2. 버튼의 시각적 피드백 업데이트 (켜졌을 때: 진한 파란색, 꺼졌을 때: 어두운 회색)
            var button = sender as Button;
            if (button != null)
            {
                var border = button.Template.FindName("PinBorder", button) as Border;
                if (border != null)
                {
                    if (Topmost)
                    {
                        // 고정됨 (ON) - 눈에 띄는 파란색 계열
                        border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2196F3"));
                    }
                    else
                    {
                        // 고정 해제됨 (OFF) - 차분한 회색 계열
                        border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#80000000"));
                    }
                }
            }
        }
    }
}