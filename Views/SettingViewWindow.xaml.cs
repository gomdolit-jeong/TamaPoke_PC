using System;
using System.Windows;
using System.Windows.Media; // 🌟 추가: WPF UI 계층을 탐색하기 위해 꼭 필요한 네임스페이스입니다.
using TamaPoke.ViewModels;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            if (DataContext is SettingsViewModel vm)
            {
                vm.RequestClose += () => this.Close();
            }
        }

        // 🌟 XAML에서 연결한 버튼 클릭 이벤트
        private void FactoryReset_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(
                "정말로 모든 데이터를 초기화하시겠습니까?\n이 작업은 되돌릴 수 없으며, 모든 포켓몬과 도감 데이터가 삭제됩니다.",
                "공장 초기화 경고",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 1. 새롭게 추가한 '탐색기' 기능으로 PokemonState를 찾아옵니다.
                    PokemonState? targetPet = FindPokemonStateFromWindows();

                    if (targetPet != null)
                    {
                        // 2. 데이터를 찾았다면 재시작 없이 부드럽게 초기화 진행!
                        targetPet.FactoryReset();
                        System.Windows.MessageBox.Show("초기화가 완료되었습니다. 새로운 알에서부터 다시 시작합니다!", "초기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);

                        // 초기화가 끝난 후 설정 창을 닫아 깔끔하게 게임으로 복귀하게 만듭니다.
                        this.Close();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("오류: 현재 실행 중인 포켓몬 데이터를 찾을 수 없습니다.\n(DataContext 연결 경로 문제)", "초기화 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==========================================
        // 🌟 WPF 화면 트리 내부를 깊숙이 탐색하여 PokemonState를 찾아내는 핵심 메서드
        // ==========================================
        private PokemonState? FindPokemonStateFromWindows()
        {
            foreach (Window win in System.Windows.Application.Current.Windows)
            {
                // 최상단 창에 있는지 확인
                if (win.DataContext is PokemonState pet) return pet;

                // 창 내부에 겹겹이 쌓인 UI 자식 요소들을 재귀적으로 샅샅이 탐색
                PokemonState? foundInTree = SearchVisualTree(win);
                if (foundInTree != null) return foundInTree;
            }
            return null;
        }

        private PokemonState? SearchVisualTree(DependencyObject parent)
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                // 해당 UI 요소가 PokemonState를 들고 있는지 확인
                if (child is FrameworkElement fe && fe.DataContext is PokemonState pet)
                {
                    return pet; // 드디어 찾았습니다!
                }

                // 찾지 못했다면 더 깊은 안쪽(자식의 자식)까지 계속해서 파고듭니다.
                PokemonState? result = SearchVisualTree(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}