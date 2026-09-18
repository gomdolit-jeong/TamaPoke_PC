using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel; // 🌟 상태 변화 알림을 받기 위해 추가
using TamaPoke.Models;
using TamaPoke.Utils.Service;

namespace TamaPoke.Views
{
    public partial class CleanGameView : System.Windows.Controls.UserControl
    {
        private static CleanGameView? _cachedInstance;

        public static CleanGameView GetInstance(PokemonState pet)
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = new CleanGameView();
            }
            _cachedInstance.DataContext = pet;
            return _cachedInstance;
        }

        public CleanGameView()
        {
            InitializeComponent();

            // 🌟 화면 로드/언로드 시 이벤트 연결
            this.Loaded += CleanGameView_Loaded;
            this.Unloaded += CleanGameView_Unloaded;
        }

        private void CleanGameView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet) pet.PropertyChanged += Pet_PropertyChanged;
        }

        private void CleanGameView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet) pet.PropertyChanged -= Pet_PropertyChanged;
        }

        // 🌟 청소 게임 상태 변화 감지
        private async void Pet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PokemonState.IsCleanGameOpen))
            {
                var pet = GetPet();
                if (pet != null && !pet.IsCleanGameOpen)
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
            if (pet != null && pet.IsCleanGameOpen)
            {
                System.Windows.Point pos = e.GetPosition((UIElement)sender);
                double targetX = pos.X - 160;
                double targetY = pos.Y - 160;
                pet.TargetPosX = Math.Max(-100, Math.Min(100, targetX));
                pet.TargetPosY = Math.Max(-100, Math.Min(100, targetY));
            }
        }

        private void Dirt_Click(object sender, MouseButtonEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsCleanGameOpen)
            {
                if (sender is System.Windows.Controls.Button btn && btn.Tag is int dirtId)
                {
                    pet.TapDirt(dirtId);
                    e.Handled = true;
                }
            }
        }

        private void PokemonImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsCleanGameOpen)
            {
                pet.TriggerAttackMotion();
                e.Handled = true;
            }
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                // 수동 종료 시 상태값만 변경하여 자동 전환 유도
                pet.IsCleanGameOpen = false;
            }
        }

        // 🌟 메인 화면 복귀 로직
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