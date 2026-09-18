using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel; // 🌟 상태 변화 알림을 받기 위해 필수적인 네임스페이스입니다.
using TamaPoke.Models;
using TamaPoke.Utils.Service;

namespace TamaPoke.Views
{
    public partial class BallGameView : System.Windows.Controls.UserControl
    {
        private static BallGameView? _cachedInstance;

        public static BallGameView GetInstance(PokemonState pet)
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = new BallGameView();
            }
            _cachedInstance.DataContext = pet;
            return _cachedInstance;
        }

        public BallGameView()
        {
            InitializeComponent();

            // 🌟 화면 로드 및 언로드 시 이벤트를 연결/해제합니다.
            this.Loaded += BallGameView_Loaded;
            this.Unloaded += BallGameView_Unloaded;
        }

        private void BallGameView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.PropertyChanged += Pet_PropertyChanged;
            }
        }

        private void BallGameView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.PropertyChanged -= Pet_PropertyChanged;
            }
        }

        // 🌟 포켓몬의 상태 변화를 실시간으로 감지합니다.
        private async void Pet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PokemonState.IsBallGameOpen))
            {
                var pet = GetPet();
                // 3번 연속으로 공을 떨어뜨려 내부 로직에서 IsBallGameOpen이 false가 되면 자동 실행됩니다.
                if (pet != null && !pet.IsBallGameOpen)
                {
                    await System.Threading.Tasks.Task.Delay(2000);
                    ReturnToIdle(pet);
                }
            }
        }

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

        private void TapBall_Click(object sender, MouseButtonEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsBallGameOpen)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.TapBall();
                e.Handled = true;
            }
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                // 사용자가 수동으로 그만두기를 누를 때에도 상태값만 변경하여 자동 전환 이벤트를 유도합니다.
                pet.IsBallGameOpen = false;
            }
        }

        public void ReturnToIdle(PokemonState pet)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow is MainWindow mainWindow)
            {
                IdleView idleView = new IdleView();
                idleView.DataContext = pet;
                mainWindow.NavigateTo(idleView);
            }
        }
    }
}