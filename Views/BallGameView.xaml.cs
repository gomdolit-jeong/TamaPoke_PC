using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TamaPoke.Models;
using TamaPoke.Utils.Service;

namespace TamaPoke.Views
{
    public partial class BallGameView : System.Windows.Controls.UserControl
    {
        // 🌟 뷰를 매번 새로 만들지 않고 캐싱하여 재사용하기 위한 정적 인스턴스
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
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        // 마우스 이동 시 포켓몬 이동 처리
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

        // 🌟 즉발형 터치 이벤트 처리
        private void TapBall_Click(object sender, MouseButtonEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsBallGameOpen)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.TapBall();

                // 마우스 클릭 이벤트가 부모 UI로 전달되지 않고 여기서 완료되도록 처리
                e.Handled = true;
            }
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.IsBallGameOpen = false;
                ReturnToIdle(pet);
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