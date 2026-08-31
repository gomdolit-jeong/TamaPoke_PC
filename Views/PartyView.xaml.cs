using System.Windows;
using System.Windows.Controls;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class PartyView : UserControl
    {
        public PartyView()
        {
            InitializeComponent();
        }

        // 🌟 포켓몬 카드를 클릭했을 때의 스왑 처리
        private void PartyMemberCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is PartyMember clickedMember &&
                this.DataContext is PokemonState state)
            {
                if (state.IsSwapMode)
                {
                    state.ExecuteSwap(clickedMember); // 🌟 클릭한 카드와 자리를 바꿉니다!
                }
            }
        }

        // 🌟 액션 버튼 (배틀 출전 / 교체 취소) 클릭 처리
        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                if (state.IsSwapMode)
                {
                    // 교체 모드일 때는 '교체 취소' 버튼으로 작동합니다.
                    state.CancelSwap();
                    state.CloseParty();
                }
                else
                {
                    // 교체 모드가 아닐 때는 '배틀 출전' 버튼으로 작동합니다.
                    // (배틀 출전 로직은 아직 비어있습니다)
                    System.Diagnostics.Debug.WriteLine("배틀 출전 버튼이 눌렸습니다!");
                }
            }
        }

        // 돌아가기 버튼 클릭 처리
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                // 화면을 강제로 닫을 때도 안전하게 교체를 취소해 줍니다.
                if (state.IsSwapMode)
                {
                    state.CancelSwap();
                }
                state.CloseParty();
            }
        }
    }
}