using System;
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
    // 🌟 에러의 원인이었던 GenerationOption 클래스를 다시 포함했습니다!
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

        // 바탕화면 포켓몬 수 프로퍼티
        public int RoamingPokemonCount
        {
            get => Settings.RoamingPokemonCount;
            set { Settings.RoamingPokemonCount = value; OnPropertyChanged(); }
        }

        public bool IsTaskbarMode
        {
            get => Settings.IsTaskbarMode;
            set { Settings.IsTaskbarMode = value; OnPropertyChanged(); }
        }

        public ObservableCollection<GenerationOption> Generations { get; set; }
        public ICommand SaveCommand { get; }
        public event Action? RequestClose;

        public SettingsViewModel()
        {
            Settings = SettingsManager.Load();

            // 이별 기준일 보정
            if (Settings.FarewellAgeDays <= 0)
            {
                Settings.FarewellAgeDays = 3;
            }

            // 포켓몬 수가 1~6 범위를 벗어나면 기본값 1로 강제 설정
            if (Settings.RoamingPokemonCount < 1 || Settings.RoamingPokemonCount > 6)
            {
                Settings.RoamingPokemonCount = 1;
            }

            // 1~9세대 지방명 매핑 초기화
            var genNames = new Dictionary<int, string>
            {
                { 1, "1세대 (관동 지방)" }, { 2, "2세대 (성도 지방)" }, { 3, "3세대 (호연 지방)" },
                { 4, "4세대 (신오 지방)" }, { 5, "5세대 (하나 지방)" }, { 6, "6세대 (칼로스 지방)" },
                { 7, "7세대 (알로라 지방)" }, { 8, "8세대 (가라르 지방)" }, { 9, "9세대 (팔데아 지방)" }
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
            Settings.SelectedGenerations = Generations
                .Where(g => g.IsSelected)
                .Select(g => g.GenNumber)
                .ToList();

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