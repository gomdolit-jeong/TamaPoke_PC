using System.Windows;
using System.Windows.Controls;
// 🌟 PokemonState가 위치한 정확한 네임스페이스
using TamaPoke.Models;

namespace TamaPoke.Views
{
    // 🌟 1. UserControl 모호성 완벽 차단
    public partial class ProfileCardView : System.Windows.Controls.UserControl
    {
        public ProfileCardView()
        {
            InitializeComponent();
        }

        // null 비교 연산 에러를 방지하도록 패턴 매칭을 사용합니다.
        private PokemonState? GetPet() => DataContext as PokemonState;

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (GetPet() is PokemonState pet)
            {
                pet.IsProfileOpen = false;
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            GetPet()?.PrevProfilePage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            GetPet()?.NextProfilePage();
        }

        private void StartBattle_Click(object sender, RoutedEventArgs e)
        {
            if (GetPet() is PokemonState pet)
            {
                pet.StartWildBattle();
            }
        }

        private void GymBadge_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                if (int.TryParse(btn.Tag.ToString(), out int gymIndex))
                {
                    if (this.DataContext is PokemonState state)
                    {
                        state.PromptGymChallenge(gymIndex);
                    }
                }
            }
        }

        private void ConfirmGym_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                state.ConfirmGymChallenge();
            }
        }

        private void CancelGym_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                state.CancelGymChallenge();
            }
        }
    }
}