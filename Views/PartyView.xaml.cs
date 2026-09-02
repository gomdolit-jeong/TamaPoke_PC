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
            // 💡 디버깅을 위해 이 줄에 중단점(Break point, F9)을 걸어보세요!
            if (sender is Border border && border.DataContext is PartyMember clickedMember)
            {
                var state = this.DataContext as PokemonState;
                if (state == null || state.Party == null) return;

                if (state.IsSwapMode)
                {
                    state.ExecuteSwap(clickedMember);
                }
                else
                {
                    foreach (var member in state.Party)
                    {
                        member.IsSelected = false;
                    }
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

        private void ChangeMainButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // 1. 현재 데이터 컨텍스트(PokemonState)를 가져옵니다.
            var state = this.DataContext as PokemonState;
            if (state == null || state.Party == null) return;

            // 2. 파티에서 IsSelected가 true인 멤버를 찾습니다. (단일 선택이므로 1개만 나옵니다)
            var selectedMember = state.Party.FirstOrDefault(p => p.IsSelected);

            if (selectedMember == null)
            {
                // 선택된 포켓몬이 없다면 그냥 함수를 종료하거나 메시지를 띄웁니다.
                System.Windows.MessageBox.Show("교체할 파티 멤버를 먼저 선택해 주세요!", "알림");
                return;
            }

            // 3. 현재 IdleView에 있는 '메인 포켓몬'의 스탯을 백업하여 새로운 파티 멤버 카드로 만듭니다.
            state.SwapMainWithParty(selectedMember);
        }
    }
}