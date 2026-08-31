using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class CatchGameView : UserControl
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
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        private void GameArea_MouseMove(object sender, MouseEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsCatchGameOpen)
            {
                // 포켓몬이 마우스를 따라 부드럽게 이동합니다.
                Point pos = e.GetPosition((UIElement)sender);
                double targetX = pos.X - 160;
                pet.TargetPosX = Math.Max(-100, Math.Min(100, targetX));
            }
        }

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.IsCatchGameOpen = false;
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