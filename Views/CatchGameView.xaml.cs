using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel; // 🌟 상태 변화 알림을 받기 위해 추가된 네임스페이스
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class CatchGameView : System.Windows.Controls.UserControl
    {
        private static CatchGameView? _cachedInstance;

        public static CatchGameView GetInstance(PokemonState pet)
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = new CatchGameView();
            }
            _cachedInstance.DataContext = pet;
            return _cachedInstance;
        }

        public CatchGameView()
        {
            InitializeComponent();

            // 🌟 화면이 켜지고 꺼질 때 이벤트를 연결하고 해제합니다.
            this.Loaded += CatchGameView_Loaded;
            this.Unloaded += CatchGameView_Unloaded;
        }

        private void CatchGameView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.PropertyChanged += Pet_PropertyChanged;
            }
        }

        private void CatchGameView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.PropertyChanged -= Pet_PropertyChanged;
            }
        }

        // 🌟 포켓몬의 상태가 변할 때마다 호출되는 메서드입니다.
        private async void Pet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 게임 상태(IsCatchGameOpen)가 변했는지 확인합니다.
            if (e.PropertyName == nameof(PokemonState.IsCatchGameOpen))
            {
                var pet = GetPet();
                // 게임이 종료(false)되었다면 메인 화면으로 돌아갑니다.
                if (pet != null && !pet.IsCatchGameOpen)
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
            if (pet != null && pet.IsCatchGameOpen)
            {
                System.Windows.Point pos = e.GetPosition((UIElement)sender);
                double targetX = pos.X - 160;
                pet.TargetPosX = Math.Max(-100, Math.Min(100, targetX));
            }
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                // 상태를 false로 바꾸면, 위에서 만든 Pet_PropertyChanged가 이를 감지하여 자동으로 ReturnToIdle을 실행합니다.
                pet.IsCatchGameOpen = false;
            }
        }

        // 🌟 메인 화면으로 돌아가는 로직을 하나의 메서드로 깔끔하게 묶어 재사용합니다.
        private void ReturnToIdle(PokemonState pet)
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