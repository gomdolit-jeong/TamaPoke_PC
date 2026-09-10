using System.Windows;
using TamaPoke.ViewModels;

namespace TamaPoke.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            // 뷰모델의 저장이 완료되면 창이 닫히도록 연결
            if (DataContext is SettingsViewModel vm)
            {
                vm.RequestClose += () => this.Close();
            }
        }
    }
}