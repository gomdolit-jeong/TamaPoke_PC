using System.Windows;
using System.Windows.Controls;
using TamaPoke.Models; // PokemonState 등의 데이터 모델을 사용하기 위해 꼭 필요합니다.

namespace TamaPoke.Views
{
    public partial class ProfileCardView : System.Windows.Controls.UserControl
    {
        public ProfileCardView()
        {
            InitializeComponent();
        }

        // ==========================================
        // 🌟 1. 체육관 뱃지 클릭 이벤트
        // ==========================================
        private void GymBadge_Click(object sender, RoutedEventArgs e)
        {
            // 클릭된 버튼과 현재 뷰모델(PokemonState)을 가져옵니다.
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null && this.DataContext is PokemonState state)
            {
                // UI의 뱃지 번호(Tag)는 1번부터 64번까지입니다.
                int globalIndex = (int)btn.Tag;

                // GymLeaders 배열과 진행도 체크 로직은 0부터 시작하므로 -1을 해줍니다!
                int zeroBasedIndex = globalIndex - 1;

                // PokemonState 내부에 작성해두신 검증 및 팝업 띄우기 로직을 실행합니다.
                state.PromptGymChallenge(zeroBasedIndex);
            }
        }

        // ==========================================
        // 🌟 2. 오버레이 확인창 - '도전하기' 클릭
        // ==========================================
        private void ConfirmGym_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                // PokemonState 내부에 작성해두신 실제 배틀 진입 로직을 실행합니다.
                state.ConfirmGymChallenge();
            }
        }

        // ==========================================
        // 🌟 3. 오버레이 확인창 - '취소' 클릭
        // ==========================================
        private void CancelGym_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state)
            {
                state.CancelGymChallenge();
            }
        }

        // ==========================================
        // (기존) 기본 프로필 창 조작용 버튼 이벤트들
        // ==========================================
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state) state.IsProfileOpen = false;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state) state.PrevProfilePage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState state) state.NextProfilePage();
        }

        private void StartBattle_Click(object sender, RoutedEventArgs e)
        {
            // 야생 배틀 시작 버튼용 이벤트
            if (this.DataContext is PokemonState state) state.StartWildBattle();
        }
    }
}