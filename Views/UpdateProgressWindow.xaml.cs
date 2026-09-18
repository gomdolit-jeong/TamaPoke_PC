using System;
using System.Collections.Generic;
using System.Windows;
using TamaPoke.Models;
using TamaPoke.Utils;

namespace TamaPoke.Views
{
    public partial class UpdateProgressWindow : Window
    {
        private bool _requiresRestart = false;
        private bool _isUpdating = false;

        private bool _needsProgramUpdate = false;

        private int _currentTaskIndex = 0;
        private double _progressPerTask = 0;

        public UpdateProgressWindow()
        {
            InitializeComponent();

            PokemonState.IsGamePaused = true;

            this.Closing += UpdateProgressWindow_Closing;
            this.Closed += UpdateProgressWindow_Closed;
        }

        private async void btnStartUpdate_Click(object? sender, RoutedEventArgs e)
        {
            if (chkData.IsChecked != true && chkSkill.IsChecked != true && chkSprite.IsChecked != true && chkProgram.IsChecked != true)
            {
                System.Windows.MessageBox.Show("업데이트할 항목을 최소 하나 이상 선택해 주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ==========================================
            // 🌟 업데이트 중 조작 방지를 위해 모든 UI를 완벽하게 잠급니다!
            // ==========================================
            _isUpdating = true;
            btnStartUpdate.IsEnabled = false;
            btnClose.IsEnabled = false;

            chkData.IsEnabled = false;
            chkSkill.IsEnabled = false;
            chkSprite.IsEnabled = false; 
            chkProgram.IsEnabled = false;
            // ==========================================

            lbLogs.Items.Clear();
            pbStatus.Value = 0;
            _currentTaskIndex = 0;

            int totalTasks = 0;
            if (chkData.IsChecked == true) totalTasks++;
            if (chkSkill.IsChecked == true) totalTasks++;
            if (chkSprite.IsChecked == true) totalTasks++;
            if (chkProgram.IsChecked == true) totalTasks++;

            _progressPerTask = 100.0 / totalTasks;

            IProgress<string> progress = new Progress<string>(message =>
            {
                lbLogs.Items.Add(message);
                lbLogs.ScrollIntoView(lbLogs.Items[lbLogs.Items.Count - 1]);

                // 🌟 [수정됨] 하드코딩된 1025 대신 GameConstants.MAX_POKEMON_ID를 사용합니다.
                string targetString = $"/{TamaPoke.Models.GameConstants.MAX_POKEMON_ID}";
                if (message.Contains("도감 번호") && message.Contains(targetString))
                {
                    try
                    {
                        string[] parts = message.Split(new string[] { "도감 번호 ", targetString }, StringSplitOptions.None);
                        if (parts.Length >= 2 && double.TryParse(parts[1], out double currentId))
                        {
                            // 🌟 [수정됨] 진행률 100% 계산에도 상수를 적용합니다.
                            double phaseProgress = (currentId / TamaPoke.Models.GameConstants.MAX_POKEMON_ID) * _progressPerTask;
                            double baseProgress = _currentTaskIndex * _progressPerTask;
                            pbStatus.Value = baseProgress + phaseProgress;
                        }
                    }
                    catch { }
                }
            });

            try
            {
                

                if (chkData.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add("[1] 포켓몬 기본 데이터 동기화 시작...");
                    await PokemonDataUpdater.GenerateOfflineDataJsonAsync(progress, null);

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                if (chkSkill.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add("[2] 포켓몬 스킬 트리 동기화 시작...");
                    await PokemonSkillUpdater.GenerateOfflineSkillJsonAsync(progress, null);

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                if (chkSprite.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add("[3] 누락된 포켓몬 스프라이트 다운로드 및 압축 변환 시작...");
                    await PokemonSpriteUpdater.DownloadAndConvertSpritesAsync(progress);

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                if (chkProgram.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add("[4] 다마포케 프로그램 최신 버전 확인 중...");

                    string localVersion = AppVersionUpdater.GetLocalVersion();
                    progress?.Report($"- 현재 프로그램 버전: v{localVersion}");
                    progress?.Report("- 서버에서 최신 버전을 확인하고 있습니다...");

                    var updateInfo = await AppVersionUpdater.CheckForUpdatesAsync();

                    if (updateInfo != null)
                    {
                        progress?.Report($"✨ 새로운 업데이트가 발견되었습니다! (v{updateInfo.Version})");

                        // 🌟 복잡한 다운로드/압축해제 로직은 일꾼에게 전부 위임합니다!
                        bool success = await AppVersionUpdater.DownloadAndPrepareUpdateAsync(updateInfo, progress);
                        if (success)
                        {
                            _requiresRestart = true;
                            _needsProgramUpdate = true;
                        }
                    }
                    else
                    {
                        progress?.Report("✅ 현재 최신 버전을 사용 중입니다. 업데이트가 필요하지 않습니다.");
                    }

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                lbLogs.Items.Add("======================================");

                if (_requiresRestart)
                {
                    lbLogs.Items.Add("🚨 새 버전이 적용되었습니다. 창을 닫으면 프로그램이 재시작됩니다!");
                    btnClose.Content = "업데이트 적용 및 재시작";
                }
                else
                {
                    lbLogs.Items.Add("✨ 선택한 모든 업데이트가 성공적으로 완료되었습니다!");
                    btnClose.Content = "업데이트 완료 (닫기)";
                }
            }
            catch (Exception ex)
            {
                lbLogs.Items.Add($"❌ 업데이트 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                // ==========================================
                // 🌟 [수정됨] 작업이 끝나면 닫기 버튼을 열어줍니다. 
                // 재시작이 필요 없는 상황을 대비해 체크박스도 다시 켤 수 있도록 복구합니다.
                // ==========================================
                _isUpdating = false;
                btnClose.IsEnabled = true;

                if (!_requiresRestart)
                {
                    btnStartUpdate.IsEnabled = true;
                    chkData.IsEnabled = true;
                    chkSkill.IsEnabled = true;
                    chkSprite.IsEnabled = true;
                    chkProgram.IsEnabled = true;
                }
            }
        }

        private void btnClose_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();

            if (_needsProgramUpdate)
            {
                // 🌟 배치 파일 실행 및 종료 마법도 일꾼에게 시킵니다!
                AppVersionUpdater.ExecutePostUpdateBatch();
            }
            else if (_requiresRestart)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void UpdateProgressWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isUpdating)
            {
                System.Windows.MessageBox.Show("업데이트가 진행 중입니다. 완료될 때까지 기다려주세요.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Cancel = true;
            }
        }

        private void UpdateProgressWindow_Closed(object? sender, EventArgs e)
        {
            // 🌟 업데이트 창이 완전히 꺼지면 게임 일시 정지를 해제합니다.
            PokemonState.IsGamePaused = false;
        }
    }
}