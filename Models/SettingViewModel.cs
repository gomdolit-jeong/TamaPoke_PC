using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TamaPoke.Models;
using TamaPoke.Utils;

namespace TamaPoke.ViewModels
{
    public class GenerationOption : INotifyPropertyChanged
    {
        public int GenNumber { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SettingsViewModel : INotifyPropertyChanged
    {
        // 🌟 외부 바인딩이 가능하도록 퍼블릭 프로퍼티로 변경합니다.
        private GameSettings _settings = new GameSettings();

        public GameSettings Settings
        {
            get => _settings;
            set { _settings = value; OnPropertyChanged(); }
        }

        public bool UseTrayNotifications
        {
            get => Settings.UseTrayNotifications;
            set { Settings.UseTrayNotifications = value; OnPropertyChanged(); }
        }

        // 🌟 세대 선택 체크박스용 컬렉션
        public ObservableCollection<GenerationOption> Generations { get; set; }

        public ICommand SaveCommand { get; }
        public event Action? RequestClose;

        public SettingsViewModel()
        {
            Settings = SettingsManager.Load();

            // 🌟 저장된 파일에 이별 기준일 값이 없거나 0 이하일 경우 기본값 3으로 보정
            if (Settings.FarewellAgeDays <= 0)
            {
                Settings.FarewellAgeDays = 3;
            }

            // 1~9세대 지방명 매핑 초기화
            var genNames = new Dictionary<int, string>
            {
                { 1, "1세대 (관동 지방)" },
                { 2, "2세대 (성도 지방)" },
                { 3, "3세대 (호연 지방)" },
                { 4, "4세대 (신오 지방)" },
                { 5, "5세대 (하나 지방)" },
                { 6, "6세대 (칼로스 지방)" },
                { 7, "7세대 (알로라 지방)" },
                { 8, "8세대 (가라르 지방)" },
                { 9, "9세대 (팔데아 지방)" }
            };

            Generations = new ObservableCollection<GenerationOption>();
            foreach (var kvp in genNames)
            {
                Generations.Add(new GenerationOption
                {
                    GenNumber = kvp.Key,
                    DisplayText = kvp.Value,
                    IsSelected = Settings.SelectedGenerations.Contains(kvp.Key)
                });
            }

            SaveCommand = new RelayCommand(SaveSettings);
        }

        private void SaveSettings()
        {
            // 체크된 세대들만 뽑아서 저장 데이터에 반영
            Settings.SelectedGenerations = Generations
                .Where(g => g.IsSelected)
                .Select(g => g.GenNumber)
                .ToList();

            // 최소 1개 이상은 선택되어 있어야 하므로 모두 체크 해제했다면 1세대 강제 선택
            if (Settings.SelectedGenerations.Count == 0)
            {
                Settings.SelectedGenerations.Add(1);
            }

            SettingsManager.Save(Settings);
            RequestClose?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}