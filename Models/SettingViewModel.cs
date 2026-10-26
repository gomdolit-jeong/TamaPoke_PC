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

    // 언어 선택 콤보박스 항목 (WPF 바인딩은 튜플 필드를 읽지 못하므로 속성을 가진 레코드로)
    public record LanguageOption(string Code, string Name);

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

        // 언어 선택 (저장하면 즉시 적용)
        public IReadOnlyList<LanguageOption> Languages { get; } = Loc.Supported.Select(l => new LanguageOption(l.Code, l.Name)).ToList();
        private string _selectedLanguage = Loc.Korean;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); }
        }
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

            Generations = new ObservableCollection<GenerationOption>();
            for (int gen = 1; gen <= 9; gen++)
            {
                Generations.Add(new GenerationOption
                {
                    GenNumber = gen,
                    DisplayText = Loc.T($"gen.{gen}"),
                    IsSelected = Settings.SelectedGenerations.Contains(gen)
                });
            }

            _selectedLanguage = Loc.Instance.Language;

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

            Settings.Language = SelectedLanguage;
            SettingsManager.Save(Settings);
            if (Loc.Instance.Language != SelectedLanguage) Loc.Instance.SetLanguage(SelectedLanguage);
            RequestClose?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}