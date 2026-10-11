using System;
using System.Windows;
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
                    // 메인 창이 가진 '진짜' 포켓몬 데이터를 직접 사용합니다.
                    // (화면 트리를 뒤지면 산책 모드의 펫 창 데이터를 잡을 수 있어 위험합니다)
                    if (System.Windows.Application.Current.MainWindow is not MainWindow mainWindow || mainWindow.MyPet == null)
                    {
                        System.Windows.MessageBox.Show("오류: 현재 실행 중인 포켓몬 데이터를 찾을 수 없습니다.", "초기화 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!mainWindow.TryFactoryReset())
                    {
                        System.Windows.MessageBox.Show("배틀, 미니게임, 이별 연출이 진행 중일 때는 초기화할 수 없습니다.\n끝난 뒤에 다시 시도해 주세요.", "초기화 불가", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    System.Windows.MessageBox.Show("초기화가 완료되었습니다. 새로운 알에서부터 다시 시작합니다!", "초기화 완료", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 초기화가 끝난 후 설정 창을 닫아 깔끔하게 게임으로 복귀하게 만듭니다.
                    this.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}