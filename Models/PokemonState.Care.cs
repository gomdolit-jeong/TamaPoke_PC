using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TamaPoke.Utils;
using TamaPoke.Utils.Service;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 돌보기와 시간 흐름 (Care & Tick)
        private bool _isFeedMenuOpen = false;
        [JsonIgnore] public bool IsFeedMenuOpen { get => _isFeedMenuOpen; set => SetProperty(ref _isFeedMenuOpen, value); }
        private bool _isPlayMenuOpen = false;
        [JsonIgnore] public bool IsPlayMenuOpen { get => _isPlayMenuOpen; set => SetProperty(ref _isPlayMenuOpen, value); }

        private string _currentFoodIcon = "🍎";
        [JsonIgnore] public string CurrentFoodIcon { get => _currentFoodIcon; set => SetProperty(ref _currentFoodIcon, value); }
        private string _currentFoodColor = "#E53935";
        [JsonIgnore] public string CurrentFoodColor { get => _currentFoodColor; set => SetProperty(ref _currentFoodColor, value); }
        private bool _isBathing = false;
        [JsonIgnore] public bool IsBathing { get => _isBathing; set => SetProperty(ref _isBathing, value); }

        private bool _isPetting = false;
        [JsonIgnore] public bool IsPetting { get => _isPetting; set => SetProperty(ref _isPetting, value); }

        public void Tick()
        {
            if (IsGamePaused) return;

            // 산책 중에는 산책 전용 규칙으로 시간이 흐릅니다. (PokemonState.Roaming.cs)
            if (IsFreeRoaming) { WalkTick(); return; }

            if (IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;

            _ageSeconds++;
            UpdateDayNightCycle();

            if (_ageSeconds % 60 == 0) Save();
            if (DateTime.Now.Date > LastPlayedDate.Date) CheckDailyStreak();

            if (IsEgg)
            {
                if (_ageSeconds >= 180) Hatch();
                return;
            }

            if (_ageSeconds % 60 == 0)
            {
                UpdateVitalsAndNeeds();
            }

            HandleStatusInterrupts();
            ProcessIdleBehaviors();

            CheckStateAndAnimate();
        }

        private void UpdateVitalsAndNeeds()
        {
            if (AgeMinutes % MINUTES_PER_LEVEL == 0)
            {
                IsEvolutionPostponed = false;
                IsFarewellPostponed = false;
            }

            AgeMinutes++;
            if (AgeMinutes % MINUTES_PER_LEVEL == 0) IsEvolutionPostponed = false;
            if (LowestStat >= 40) { _goodTicks++; if (_goodTicks >= 720) { _goodTicks = 0; if (TrDef < 100) TrDef++; } } else { _goodTicks = 0; }

            // 몸무게는 자는 중에도 3분마다 1씩 자연스럽게 빠집니다. (원작과 동일)
            if (Weight > 0 && AgeMinutes % WEIGHT_BURN_EVERY_MINUTES == 0) Weight--;

            // 자는 동안에는 케어 실수가 생기지 않습니다. (원작과 동일)
            if (IsSleeping)
            {
                Energy = Clamp100(Energy + 6);
                if (AgeMinutes % 30 == 0) Hygiene = Math.Max(0, Hygiene - 1);
                return;
            }

            if (AgeMinutes % 20 == 0) { Fullness = Math.Max(0, Fullness - 2); Energy = Math.Max(0, Energy - 1); }

            Random rand = new Random();

            if (Fullness > 40 && Poops < MAX_POOPS && rand.Next(100) < POOP_CHANCE)
            {
                Poops++;
                Hygiene = Clamp100(Hygiene - (10 * Poops));

                if (Settings.UseTrayNotifications)
                    TrayNotificationRequested?.Invoke("화장실 알림", "💩");
            }

            if (AgeMinutes % 10 == 0) { int dJoy = 0; if (Fullness < 30) dJoy -= 2; if (Hygiene < 30) dJoy -= 3; Joy = Clamp100(Joy + dJoy); }

            CheckCareMistake();

            if (Fullness == 0 && Joy == 0 && Energy == 0 && Hygiene == 0)
            {
                if (NeglectTicks < RUNAWAY_TICKS)
                    NeglectTicks++;

                if (CanRunawayNow)
                {
                    StartRunaway();
                    return;
                }
            }
            else
            {
                NeglectTicks = 0;
            }

            if (Fullness < 30 && !_notifiedHunger)
            {
                if (Settings.UseTrayNotifications) TrayNotificationRequested?.Invoke("배고픔 알림", "🍚");
                _notifiedHunger = true;
            }
            else if (Fullness >= 30)
            {
                _notifiedHunger = false;
            }

            if (Joy < 30 && !_notifiedSadness)
            {
                if (Settings.UseTrayNotifications) TrayNotificationRequested?.Invoke("우울함 알림", "🥹");
                _notifiedSadness = true;
            }
            else if (Joy >= 30)
            {
                _notifiedSadness = false;
            }

            CheckMedals();
        }

        // 케어 실수: 스탯 하나라도 10 이하로 방치되면 1회 (같은 방치를 매분 세지 않도록 60분 쿨다운)
        // 실수 1회마다 진화가 1레벨 늦어지고 유대감이 1 줄어듭니다. 퍼펙트 바디 메달은 실수 0회일 때만. (원작과 동일)
        private void CheckCareMistake()
        {
            if (_mistakeCooldown > 0) _mistakeCooldown--;
            if (LowestStat > CARE_MISTAKE_THRESHOLD || _mistakeCooldown > 0) return;

            CareMistakes++;
            _mistakeCooldown = CARE_MISTAKE_COOLDOWN_MINUTES;
            if (Bond > 1) Bond--;
        }

        private void HandleStatusInterrupts()
        {
            if (Poops > 0 || Hygiene < 30)
            {
                if (_tempActionTimer > 0 && _isAutoAction)
                {
                    _tempActionTimer = 0;
                    _tempActionId = ANIM_IDLE;
                }
            }
        }

        private void ProcessIdleBehaviors()
        {
            if (!IsSleeping && !IsCeremony && !IsBattleOpen && _tempActionTimer <= 0)
            {
                if (_currentActionId == ANIM_IDLE && Poops == 0 && Hygiene >= 30)
                {
                    Random moveRand = new Random(); int r = moveRand.Next(100);
                    if (r < 35)
                    {
                        if (moveRand.Next(10000) < 3)
                        {
                            StartWildBattle();
                        }
                        else
                        {
                            FlipX = moveRand.Next(0, 2) == 0 ? -1 : 1;
                            PlayAutoAction(ANIM_WALK, moveRand.Next(120, 240));
                        }
                    }
                    else if (r < 60) { int[] flair = { ANIM_POSE, ANIM_NOD, ANIM_DEEPBREATH }; PlayAutoAction(flair[moveRand.Next(flair.Length)], 90); }
                    else { PlayAutoAction(ANIM_IDLE, moveRand.Next(60, 150)); }
                }
            }
        }

        public async void Feed(int foodType)
        {
            if (IsEgg || IsSleeping || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;

            if (IsEating)
            {
                _tempActionId = ANIM_IDLE;
                OnPropertyChanged(nameof(IsEating));
                await Task.Delay(50);
            }

            switch (foodType)
            {
                case 0: CurrentFoodIcon = "🍒"; CurrentFoodColor = "#E53935"; break;
                case 1: CurrentFoodIcon = "🫐"; CurrentFoodColor = "#1E88E5"; break;
                case 2: CurrentFoodIcon = "🍏"; CurrentFoodColor = "#43A047"; break;
                case 3: CurrentFoodIcon = "🍬"; CurrentFoodColor = "#F06292"; break;
            }

            if (foodType == 3)
            {
                Fullness = Clamp100(Fullness + 10); Joy = Clamp100(Joy + 12); Weight = Clamp100(Weight + 12);
            }
            else
            {
                if (foodType == FavoriteBerry)
                {
                    Fullness = Clamp100(Fullness + 35); Joy = Clamp100(Joy + 10); Bond = Clamp100(Bond + 2); BerryKnown = true;
                }
                else
                {
                    Fullness = Clamp100(Fullness + 25);
                }
            }

            NeglectTicks = 0; IsFeedMenuOpen = false;

            ResetPosition();

            PlayTempAction(ANIM_EAT, 65);

            OnPropertyChanged(nameof(CurrentFoodIcon));
            OnPropertyChanged(nameof(CurrentFoodColor));
            OnPropertyChanged(nameof(IsEating));
            CheckStateAndAnimate();

            // 🌟 수정됨: 열매가 사라지는 시간과 동일하게 2.1초(2100ms)만 대기합니다.
            await Task.Delay(2100);

            if (!IsAnyMiniGameOpen && !IsBattleOpen)
            {
                PlayTempAction(ANIM_HOP, 30);
                CheckStateAndAnimate();
            }
        }

        public async void Clean()
        {
            if (IsEgg || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen || IsSleeping) return;

            Poops = 0; Hygiene = 100; Bond = Clamp100(Bond + 1); NeglectTicks = 0;
            IsBathing = true;

            ResetPosition();

            PlayTempAction(ANIM_POSE, 60);
            CheckStateAndAnimate();

            await Task.Delay(2000);
            IsBathing = false;

            if (!IsAnyMiniGameOpen && !IsBattleOpen)
            {
                PlayTempAction(ANIM_SHAKE, 30);
                CheckStateAndAnimate();
            }
        }
        public void ToggleSleep()
        {
            // 미니게임 중이거나 알 상태일 때는 수면 불가능
            if (IsEgg || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsBattleOpen) return;

            // 수면 상태 반전 (자고 있으면 깨우고, 깨어있으면 재웁니다)
            IsSleeping = !IsSleeping;

            if (!IsSleeping)
            {
                // 🌟 잠에서 막 깼을 때: 14번(ANIM_WAKE) 모션을 60틱(약 2초) 동안 재생합니다!
                PlayTempAction(ANIM_WAKE, 60);
            }
            else
            {
                // 🌟 잠들었을 때: 진행 중이던 다른 임시 모션을 즉시 취소합니다.
                _tempActionTimer = 0;
            }

            // 상태를 즉시 화면에 반영합니다.
            CheckStateAndAnimate();
        }

        public void ToggleMute() => IsMuted = !IsMuted;

        // 음소거가 아닐 때만 효과음을 재생합니다.
        public void PlaySound((int f, int ms)[] notes)
        {
            if (!IsMuted) SoundManager.Play(notes);
        }

        public async void Pet()
        {
            if (IsEgg || IsCeremony || IsAnyMiniGameOpen || IsProfileOpen || IsDexOpen || IsBattleOpen) return;
            Joy = Clamp100(Joy + 5); Bond = Clamp100(Bond + 1);

            ResetPosition();
            PlayTempAction(ANIM_HOP, 30);
            CheckStateAndAnimate();

            IsPetting = true;
            await Task.Delay(1000);
            IsPetting = false;
        }

        public void TapEgg() { if (!IsEgg) return; EggTaps++; if (EggTaps >= 3) Hatch(); }
        public void PostponeEvolve() => IsEvolutionPostponed = true;
        public void PostponeFarewell() => IsFarewellPostponed = true;
        #endregion
    }
}
