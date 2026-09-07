using System.Windows;
using System.Windows.Controls;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class PokemonDexView : System.Windows.Controls.UserControl
    {
        public PokemonDexView()
        {
            InitializeComponent();
        }

        private void RegionTab_Checked(object sender, RoutedEventArgs e)
        {
            // 1. 클릭된 라디오 버튼을 가져옵니다.
            var radioButton = sender as System.Windows.Controls.RadioButton;
            if (radioButton == null) return;

            // 2. 현재 데이터 컨텍스트(PokemonState)를 가져옵니다.
            var state = this.DataContext as PokemonState;
            if (state == null) return;

            // 3. 라디오 버튼의 Tag 값(0, 1, 2, 3)을 읽어와서 도감의 지역 인덱스를 변경합니다.
            if (int.TryParse(radioButton.Tag?.ToString(), out int regionIndex))
            {
                state.CurrentRegionIndex = regionIndex;
            }

            // 🌟 핵심: 탭이 바뀔 때마다 스크롤바를 맨 위로 확실하게 끌어올려 줍니다!
            if (DexScrollViewer != null)
            {
                DexScrollViewer.ScrollToTop();
            }
        }

        private void DexButton_Click(object sender, RoutedEventArgs e)
        {
            var state = this.DataContext as PokemonState;
            if (state != null)
            {
                state.IsDexOpen = false;
            }
        }
    }
}