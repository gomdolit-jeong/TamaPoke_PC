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

        // 🌟 카드를 클릭했을 때
        private void PartyMemberCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is PartyMember clickedMember &&
                this.DataContext is PokemonState state)
            {
                if (state.IsSwapMode)
                {
                    state.ExecuteSwap(clickedMember); // 교체 모드일 때는 자리를 바꿉니다.
                }
                else
                {
                    // 🌟 교체 모드가 아닐 때는 포켓몬을 '선택'합니다.
                    // 1. 모든 멤버의 선택 상태를 해제합니다.
                    foreach (var member in state.Party)
                    {
                        member.IsSelected = false;
                    }
                    // 2. 지금 클릭한 포켓몬만 선택 상태로 만듭니다.
                    clickedMember.IsSelected = true;
                }
            }
        }

        // 🌟 액션(배틀 출전/방생) 버튼을 클릭했을 때
        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                if (state.IsSwapMode)
                {
                    state.CancelSwap();
                    state.CloseParty();
                }
                else
                {
                    // 🌟 파티에서 현재 선택된(IsSelected == true) 포켓몬을 찾습니다.
                    PartyMember? selectedMember = null;
                    foreach (var member in state.Party)
                    {
                        if (member.IsSelected) { selectedMember = member; break; }
                    }

                    if (selectedMember != null)
                    {
                        // TODO: 이 멤버를 출전시키는 배틀 로직을 여기에 구현합니다!
                        System.Diagnostics.Debug.WriteLine($"{selectedMember.Name} 출전 준비 완료!");
                        state.CloseParty();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("출전할 포켓몬을 먼저 선택해 주세요.");
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                if (state.IsSwapMode) state.CancelSwap();

                // 창을 닫을 때 선택 상태를 초기화해 줍니다.
                foreach (var member in state.Party) member.IsSelected = false;

                state.CloseParty();
            }
        }
    }
}