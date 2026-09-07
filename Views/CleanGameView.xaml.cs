using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        // 마우스를 따라 포켓몬 이동
        private void GameArea_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsCleanGameOpen)
            {
                System.Windows.Point pos = e.GetPosition((UIElement)sender);

                // 마우스의 X, Y 좌표를 모두 구해서 모델에 전달합니다.
                double targetX = pos.X - 160;
                double targetY = pos.Y - 160;

                pet.TargetPosX = Math.Max(-100, Math.Min(100, targetX));
                pet.TargetPosY = Math.Max(-100, Math.Min(100, targetY)); // 🌟 Y좌표 전달 추가
            }
        }

        // 🌟 세균을 클릭(터치)했을 때 처리
        private void Dirt_Click(object sender, MouseButtonEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsCleanGameOpen)
            {
                // 클릭된 버튼의 Tag 속성에서 세균의 Id를 가져옵니다.
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

                // 이벤트 핸들됨 처리 (포켓몬을 눌렀는데 그 뒤에 있는 세균까지 같이 눌리는 현상 방지)
                e.Handled = true;
            }
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.IsCleanGameOpen = false;
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
}