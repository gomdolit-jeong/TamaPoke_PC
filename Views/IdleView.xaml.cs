using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TamaPoke.Models;
using TamaPoke.Utils.Service;

namespace TamaPoke.Views
{
    public partial class IdleView : UserControl
    {
        public IdleView()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                Window window = Window.GetWindow(this);
                if (window != null)
                {
                    window.KeyDown += Window_KeyDown;
                }
            };
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is PokemonState pet)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "모든 데이터(도감, 출석 기록 등)를 완전히 초기화하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                        "전체 데이터 공장 초기화",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (!pet.IsMuted) SoundManager.Play(SoundManager.N_DENY);
                        pet.FactoryReset();
                    }
                }
            }
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        private void GameArea_MouseMove(object sender, MouseEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsBallGameOpen)
            {
                Point pos = e.GetPosition((UIElement)sender);
                double targetX = pos.X - 160;
                pet.TargetPosX = Math.Max(-100, Math.Min(100, targetX));
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e) => GetPet()?.ToggleMute();

        private void ToggleFeedMenu_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.IsPlayMenuOpen = false;
                pet.IsFeedMenuOpen = !pet.IsFeedMenuOpen;
            }
            else if (pet != null && !pet.IsMuted) SoundManager.Play(SoundManager.N_DENY);
        }

        private void FeedItem_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int foodType))
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_EAT);
                pet.Feed(foodType);
            }
        }

        private void TogglePlayMenu_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = !pet.IsPlayMenuOpen;
            }
            else if (pet != null && !pet.IsMuted) SoundManager.Play(SoundManager.N_DENY);
        }

        // ==========================================
        // 🌟 5종 미니게임 선택 메뉴 버튼 이벤트
        // ==========================================
        private void PlayBall_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_PLAY);
                pet.StartBallGame();

                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    BallGameView ballView = new BallGameView();
                    ballView.DataContext = pet;
                    mainWindow.NavigateTo(ballView); // 🌟 메인 프레임에 공 튕기기 뷰 로드!
                }
            }
        }

        private void PlayCatch_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_PLAY);
                pet.StartCatchGame(); // 게임 시작!

                // 캐싱된 뷰를 사용하여 부드럽게 전환
                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    CatchGameView catchView = CatchGameView.GetInstance(pet);
                    mainWindow.NavigateTo(catchView);
                }
            }
        }

        private void PlayMemo_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_PLAY);
                pet.StartMemoGame(); // 🌟 게임 시작

                // 뷰 통째로 전환
                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    MemoGameView memoView = MemoGameView.GetInstance(pet);
                    mainWindow.NavigateTo(memoView);
                }
            }
        }

        private void PlayClean_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_PLAY);
                pet.StartCleanGame(); // 게임 시작

                Window window = Window.GetWindow(this);
                if (window is MainWindow mainWindow)
                {
                    // 최적화된 캐싱 뷰 사용
                    CleanGameView cleanView = CleanGameView.GetInstance(pet);
                    mainWindow.NavigateTo(cleanView);
                }
            }
        }

        private void TapBall_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsBallGameOpen)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.TapBall();
            }
        }
        
        private void SleepButton_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg)
            {
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = false;
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.ToggleSleep();
            }
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && pet.IsAlive && !pet.IsEgg)
            {
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = false;
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_MEDAL);
                pet.Clean();
            }
            else if (pet != null && !pet.IsMuted) SoundManager.Play(SoundManager.N_DENY);
        }

        private void SkipTimeButton_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null && !pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
            pet?.SkipTimeForTest();
        }

        private void Evolve_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_MEDAL);
                pet.StartEvolutionCeremony();
            }
        }

        private void PostponeEvolve_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.PostponeEvolve();
            }
        }

        private void StartFarewell_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) { if (!pet.IsMuted) SoundManager.Play(SoundManager.N_MEDAL); pet.StartFarewell(); }
        }

        private void PostponeFarewell_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.PostponeFarewell();
            }
        }

        private void StartRunaway_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) { if (!pet.IsMuted) SoundManager.Play(SoundManager.N_DENY); pet.StartRunaway(); }
        }

        private void DexButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.IsDexOpen = !pet.IsDexOpen;
            }
        }

        private void PetImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Screen_MouseLeftButtonDown(sender, e);

        private void Screen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not PokemonState pet) return;

            if (pet.IsEgg)
            {
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_TAP);
                pet.TapEgg();
            }
            else if (pet.IsAlive && !pet.IsSleeping && !pet.IsCeremony)
            {
                pet.IsFeedMenuOpen = false;
                pet.IsPlayMenuOpen = false;
                if (!pet.IsMuted) SoundManager.Play(SoundManager.N_HEART);
                pet.Pet();
            }
        }

        private void MenuDex_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet)
            {
                pet.IsDexOpen = true;
                if (pet.IsDexOpen) pet.IsProfileOpen = false;
            }
        }

        private void MenuProfile_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                pet.IsProfileOpen = true;
                pet.ProfilePage = 0;
                if (pet.IsProfileOpen) pet.IsDexOpen = false;
            }
        }
        private void MenuInventory_Click(object sender, RoutedEventArgs e)
        {
            // 현재 화면의 데이터를 담당하는 PokemonState를 가져옵니다.
            if (this.DataContext is Models.PokemonState state && state.IsAlive)
            {
                // 가방이 열려있으면 닫고, 닫혀있으면 엽니다.
                state.IsInventoryOpen = !state.IsInventoryOpen;

                // UI가 겹쳐서 지저분해지는 것을 막기 위해, 가방을 열 때는 다른 하위 메뉴들을 닫아줍니다.
                if (state.IsInventoryOpen)
                {
                    state.IsFeedMenuOpen = false;
                    state.IsPlayMenuOpen = false;
                }
            }
        }

        private void MenuRelease_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PokemonState pet && pet.IsAlive && !pet.IsEgg && !pet.IsSleeping)
            {
                MessageBoxResult result = MessageBox.Show(
                    "정말로 포켓몬을 자연으로 놔주시겠습니까?\n이 결정은 되돌릴 수 없습니다.",
                    "자연으로 놔주기",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (!pet.IsMuted) SoundManager.Play(SoundManager.N_DENY);
                    pet.IsDexOpen = false;
                    pet.IsProfileOpen = false;
                    pet.StartRelease();
                }
            }
            else if (DataContext is PokemonState pet2 && !pet2.IsMuted)
            {
                SoundManager.Play(SoundManager.N_DENY);
            }
        }
        private void MenuItem_OpenParty_Click(object sender, RoutedEventArgs e)
        {
            // DataContext가 PokemonState인지 확인하고 상태를 변경합니다.
            if (this.DataContext is PokemonState state)
            {
                state.OpenParty();
            }
        }
    }
}
