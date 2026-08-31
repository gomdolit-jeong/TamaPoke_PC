using System;
using System.Windows;
using System.Windows.Controls;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class PokemonDexView : UserControl
    {
        public PokemonDexView()
        {
            InitializeComponent();
        }

        private void RegionTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int regionIndex))
            {
                if (this.DataContext is PokemonState pet)
                {
                    // 1. 도감 리스트 갱신
                    pet.CurrentRegionIndex = regionIndex;

                    // 🌟 2. 화면 배치가 완전히 끝난 후(ContextIdle) 스크롤을 최상단으로 올림!
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        DexScrollViewer.UpdateLayout(); // 레이아웃 확정
                        DexScrollViewer.ScrollToTop();  // 스크롤 이동
                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
            }
        }

        private void DexButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is PokemonState pet)
            {
                pet.IsDexOpen = false;
            }
        }
    }
}