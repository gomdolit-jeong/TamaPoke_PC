using System.Windows;
using System.Windows.Controls;
// 🌟 PokemonState가 위치한 정확한 네임스페이스를 추가합니다.
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class ProfileCardView : UserControl
    {
        public ProfileCardView()
        {
            InitializeComponent();
        }

        // 🌟 null 비교 연산 에러를 방지하도록 패턴 매칭을 사용합니다.
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
    }
}