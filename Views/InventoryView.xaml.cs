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

        // 🌟 모든 아이템 클릭을 통합하여 처리하는 마법의 메서드입니다.
        private void UseItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Models.ItemInfo clickedItem)
            {
                if (this.DataContext is Models.PokemonState pet)
                {
                    if (clickedItem.Quantity <= 0)
                    {
                        pet.BattleMessage = $"{clickedItem.Name}이(가) 부족합니다!";
                        return;
                    }

                    // 1. 몬스터볼 처리 로직
                    if (clickedItem.Type == Models.ItemType.monsterball)
                    {
                        if (!pet.IsBattleOpen || pet.IsGymBattle)
                        {
                            pet.BattleMessage = "야생 배틀 중에만 사용할 수 있습니다!";
                            return;
                        }
                        pet.IsInventoryOpen = false;
                        clickedItem.Quantity--;

                        if (clickedItem.Quantity <= 0) pet.Inventory.Remove(clickedItem);
                        pet.OnPropertyChanged(nameof(pet.MonsterBalls));

                        pet.RequestCatchAnimation?.Invoke();
                    }
                    // 2. 상처약(Potion) 처리 로직
                    else if (clickedItem.Type == Models.ItemType.Potion)
                    {
                        if (!pet.IsBattleOpen || !pet.IsPlayerTurn)
                        {
                            pet.BattleMessage = "자신의 턴에만 사용할 수 있습니다!";
                            return;
                        }

                        // 🌟 [핵심 수정] 가방 화면에서도 체력이 가득 찼는지 먼저 검사합니다.
                        if (pet.PlayerHp >= pet.PlayerMaxHp)
                        {
                            // 턴 소모 방지 및 경고 메시지 출력
                            pet.BattleMessage = "체력이 이미 가득 차 있습니다!";
                            return;
                        }

                        // 체력이 깎여 있을 때만 정상 소모 및 턴 넘김 실행
                        pet.IsInventoryOpen = false;

                        clickedItem.Quantity--;
                        if (clickedItem.Quantity <= 0) pet.Inventory.Remove(clickedItem);
                        pet.OnPropertyChanged(nameof(pet.Potions));

                        _ = pet.ExecuteItemTurnAsync(clickedItem);
                    }
                }
            }
        }
    }
}