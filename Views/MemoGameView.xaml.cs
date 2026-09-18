using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel; // 🌟 상태 변화 알림을 받기 위해 추가
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class MemoGameView : System.Windows.Controls.UserControl
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

            // 🌟 화면 로드/언로드 시 이벤트 연결
            this.Loaded += MemoGameView_Loaded;
            this.Unloaded += MemoGameView_Unloaded;
        }

        private void MemoGameView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet) pet.PropertyChanged += Pet_PropertyChanged;
        }

        private void MemoGameView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet) pet.PropertyChanged -= Pet_PropertyChanged;
        }

        // 🌟 메모리 게임 상태 변화 감지
        private async void Pet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PokemonState.IsMemoGameOpen))
            {
                var pet = GetPet();
                if (pet != null && !pet.IsMemoGameOpen)
                {
                    await System.Threading.Tasks.Task.Delay(2000);
                    ReturnToIdle(pet);
                }
            }
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        private void Btn0_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(0);
        private void Btn1_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(1);
        private void Btn2_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(2);
        private void Btn3_Click(object sender, RoutedEventArgs e) => GetPet()?.SubmitMemoInput(3);

        private void QuitGame_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                // 수동 종료 시 상태값만 변경하여 자동 전환 유도
                pet.IsMemoGameOpen = false;
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