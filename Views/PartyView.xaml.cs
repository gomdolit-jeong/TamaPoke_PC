using System.Windows;
using System.Windows.Controls;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class PartyView : System.Windows.Controls.UserControl
    {
        private PartyMember? _memberToSwap;

        public PartyView()
        {
            InitializeComponent();
        }

        // 🌟 카드를 클릭했을 때
        private void PartyMemberCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PartyMember clickedMember)
            {
                var state = this.DataContext as PokemonState;
                if (state == null || state.Party == null) return;

                // 🌟 핵심: 교체 모드이든 아니든, 카드를 누르면 무조건 '선택'만 되도록 통일합니다!
                // 기존에 있던 IsSwapMode일 때 ExecuteSwap을 해버리던 로직을 완전히 삭제했습니다.

                // 1. 파티의 모든 멤버의 선택 상태를 해제합니다.
                foreach (var member in state.Party)
                {
                    member.IsSelected = false;
                }

                // 2. 방금 클릭한 멤버만 '선택됨' 상태로 바꿉니다. (이때 빨간 테두리가 나타납니다!)
                clickedMember.IsSelected = true;
            }
        }

        private void ChangeMainButton_Click(object sender, RoutedEventArgs e)
        {
            var state = this.DataContext as PokemonState;
            if (state == null || state.Party == null) return;

            var selectedMember = state.Party.FirstOrDefault(p => p.IsSelected);
            if (selectedMember == null) return;

            if (state.Party.IndexOf(selectedMember) == 0) return; // 대표 포켓몬 방어

            if (state.IsSwapMode)
            {
                // 🌟 수정: 윈도우 MessageBox 대신 인게임 팝업창을 엽니다!
                _memberToSwap = selectedMember;
                ConfirmPopupText.Text = $"{selectedMember.Name} 포켓몬과\n교체하시겠습니까?";
                SwapConfirmOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                state.SwapMainWithParty(selectedMember);
            }
        }
        private void ConfirmYes_Click(object sender, RoutedEventArgs e)
        {
            var state = this.DataContext as PokemonState;
            if (state != null && _memberToSwap != null)
            {
                state.ExecuteSwap(_memberToSwap);
                state.IsPartyOpen = false; // 완료 후 파티 창 닫기
            }

            SwapConfirmOverlay.Visibility = Visibility.Collapsed; // 팝업 닫기
            _memberToSwap = null;
        }

        // 🌟 추가: 인게임 팝업에서 '아니요'를 눌렀을 때
        private void ConfirmNo_Click(object sender, RoutedEventArgs e)
        {
            SwapConfirmOverlay.Visibility = Visibility.Collapsed; // 팝업만 조용히 닫기
            _memberToSwap = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var state = this.DataContext as PokemonState;

            // 🌟 만약 포켓몬을 잡아서 교체 창이 떴는데, 안 바꾸고 그냥 닫기를 누른다면?
            if (state != null && state.IsSwapMode)
            {
                state.CancelSwap(); // 대기 중인 새 포켓몬을 포기(방생)합니다!
                System.Windows.MessageBox.Show("새로 잡은 포켓몬을 자연으로 돌려보냈습니다.", "방생");
            }

            // 창 닫기 로직 (ViewModel 바인딩을 사용 중이시라면 state.IsPartyOpen = false; 만 하셔도 됩니다)
            if (state != null) state.IsPartyOpen = false;

            // 만약 Window라면 this.Close(); 를 유지해 주세요.
            // this.Close(); 
        }
    }
}