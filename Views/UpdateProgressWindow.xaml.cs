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

        // 필요한 항목만 체크된 상태로 엽니다. (시작할 때 리소스 안내에서 사용)
        public UpdateProgressWindow(bool data, bool skill, bool sprite) : this()
        {
            chkData.IsChecked = data;
            chkSkill.IsChecked = skill;
            chkSprite.IsChecked = sprite;
            chkProgram.IsChecked = false;
        }

        private async void btnStartUpdate_Click(object? sender, RoutedEventArgs e)
        {
            if (chkData.IsChecked != true && chkSkill.IsChecked != true && chkSprite.IsChecked != true && chkProgram.IsChecked != true)
            {
                System.Windows.MessageBox.Show(Loc.T("update.pickOne"), Loc.T("common.notice"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // 진행 메시지 속 "123/1025" 패턴으로 진행률을 계산합니다. (메시지 언어와 무관)
                var match = System.Text.RegularExpressions.Regex.Match(message, $@"(\d+)/{TamaPoke.Models.GameConstants.MAX_POKEMON_ID}\b");
                if (match.Success)
                {
                    try
                    {
                        if (double.TryParse(match.Groups[1].Value, out double currentId))
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
                    lbLogs.Items.Add(Loc.T("update.step.data"));
                    await PokemonDataUpdater.GenerateOfflineDataJsonAsync(progress, null);

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                if (chkSkill.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add(Loc.T("update.step.skill"));
                    await PokemonSkillUpdater.GenerateOfflineSkillJsonAsync(progress, null);

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                if (chkSprite.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add(Loc.T("update.step.sprite"));
                    await PokemonSpriteUpdater.DownloadAndConvertSpritesAsync(progress);

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                if (chkProgram.IsChecked == true)
                {
                    lbLogs.Items.Add("======================================");
                    lbLogs.Items.Add(Loc.T("update.step.program"));

                    string localVersion = AppVersionUpdater.GetLocalVersion();
                    progress?.Report(Loc.F("update.currentVersion", localVersion));
                    progress?.Report(Loc.T("update.checking"));

                    var updateInfo = await AppVersionUpdater.CheckForUpdatesAsync();

                    if (updateInfo != null)
                    {
                        progress?.Report(Loc.F("update.found", updateInfo.Version));

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
                        progress?.Report(Loc.T("update.upToDate"));
                    }

                    _currentTaskIndex++;
                    pbStatus.Value = _currentTaskIndex * _progressPerTask;
                }

                lbLogs.Items.Add("======================================");

                if (_requiresRestart)
                {
                    lbLogs.Items.Add(Loc.T("update.applied"));
                    btnClose.Content = Loc.T("update.btn.restart");
                }
                else
                {
                    lbLogs.Items.Add(Loc.T("update.allDone"));
                    lbLogs.Items.Add(Loc.T("update.maybeReset"));
                    btnClose.Content = Loc.T("update.btn.done");
                }
            }
            catch (Exception ex)
            {
                lbLogs.Items.Add(Loc.F("update.error", ex.Message));
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
                System.Windows.MessageBox.Show(Loc.T("update.busy"), Loc.T("common.notice"), MessageBoxButton.OK, MessageBoxImage.Information);
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