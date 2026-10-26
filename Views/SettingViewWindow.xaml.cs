using System;
using System.Windows;
using TamaPoke.ViewModels;
using TamaPoke.Models;
using TamaPoke.Utils;

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
                Loc.T("reset.confirm.message"),
                Loc.T("reset.confirm.title"),
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
                        System.Windows.MessageBox.Show(Loc.T("reset.notFound"), Loc.T("reset.failed.title"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!mainWindow.TryFactoryReset())
                    {
                        System.Windows.MessageBox.Show(Loc.T("reset.blocked.message"), Loc.T("reset.blocked.title"), MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    System.Windows.MessageBox.Show(Loc.T("reset.done.message"), Loc.T("reset.done.title"), MessageBoxButton.OK, MessageBoxImage.Information);

                    // 초기화가 끝난 후 설정 창을 닫아 깔끔하게 게임으로 복귀하게 만듭니다.
                    this.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(Loc.F("reset.error", ex.Message), Loc.T("common.error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}