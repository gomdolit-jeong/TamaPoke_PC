using System;
using System.Windows;
using System.Windows.Controls;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class MemoGameView : UserControl
    {
        private static MemoGameView? _cachedInstance;

        public static MemoGameView GetInstance(PokemonState pet)
        {
            if (_cachedInstance == null)
            {
                _cachedInstance = new MemoGameView();
            }
            _cachedInstance.DataContext = pet;
            return _cachedInstance;
        }

        public MemoGameView()
        {
            InitializeComponent();
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        // 클릭된 인덱스를 모델의 로직으로 전송
        private void Btn0_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(0);
        private void Btn1_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(1);
        private void Btn2_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(2);
        private void Btn3_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(3);

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                pet.IsMemoGameOpen = false;
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