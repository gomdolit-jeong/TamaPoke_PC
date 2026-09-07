using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TamaPoke.Views
{
    public partial class InventoryView : System.Windows.Controls.UserControl
    {
        public InventoryView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Models.PokemonState state)
            {
                state.IsInventoryOpen = false;
            }
        }

        private void UseMonsterBall_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Models.PokemonState pet)
            {
                if (pet.MonsterBalls <= 0)
                {
                    pet.BattleMessage = "몬스터볼이 부족합니다!";
                    return;
                }

                if (!pet.IsBattleOpen)
                {
                    pet.BattleMessage = "배틀 중에만 사용할 수 있습니다!";
                    return;
                }

                pet.IsInventoryOpen = false;
                pet.MonsterBalls--;
                pet.RequestCatchAnimation?.Invoke();
            }
        }

        // 🌟 상처약 버튼 클릭 이벤트 (MouseButtonEventArgs -> RoutedEventArgs로 변경)
        private void UsePotion_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Models.PokemonState pet)
            {
                if (pet.Potions <= 0)
                {
                    pet.BattleMessage = "상처약이 부족합니다!";
                    return;
                }

                pet.IsInventoryOpen = false;
                pet.Potions--;
                pet.Energy = Math.Min(100, pet.Energy + 30);
                pet.BattleMessage = "상처약을 사용하여 체력을 30 회복했다!";
            }
        }
    }
}