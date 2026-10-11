using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TamaPoke.Models;
using TamaPoke.Utils;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace TamaPoke.Views
{
    // 산책 모드에서 바탕화면을 돌아다니는 펫 한 마리 (투명 창 + 이동/행동 루프)
    public sealed class RoamingPet
    {
        // ==========================================
        // 🌟 알트탭/작업표시줄에 나타나지 않게 하는 Win32 스타일
        // ==========================================
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        private const double BASE_ROAMING_SPEED = 3.0;
        private const double SPRITE_ZOOM = 2.0;    // 스프라이트 원본 대비 배율 (IdleView와 동일)
        private const int RUMMAGE_TICKS = 130;     // 풀숲 뒤적이기 연출 길이 (33ms 틱, 약 4.3초)

        // 목적지에 도착하거나 이벤트가 생겼을 때 잠시 하는 행동
        private enum Behavior { Walking, Sleeping, Sinking, Interacting, Rummaging }

        public PokemonState State { get; }
        public Window Window { get; }

        // 산책 중 이 펫이 줍게 된 아이템 (풀숲 뒤적이기 연출이 끝나면 머리 위에 표시)
        public ItemDefinition? PendingFoundItem { get; set; }

        public event Action? ToggleWalkRequested;   // 펫 창에서 Ctrl+W
        public event Action<RoamingPet>? Closed;

        private readonly IReadOnlyList<RoamingPet> _others; // 서로 부딪히면 놀기 위한 다른 펫 목록
        private readonly DispatcherTimer _moveTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
        private readonly Random _rnd = new(Guid.NewGuid().GetHashCode());
        private readonly bool _isTaskbarMode;
        private readonly double _scale;
        private readonly double _width, _height;
        private readonly double _speed;

        private double _targetX, _targetY;
        private int _restCounter = 0;
        private Behavior _behavior = Behavior.Walking;
        private int _interactionCooldown = 0;

        public RoamingPet(PartyMember member, IReadOnlyList<RoamingPet> others, GameSettings settings)
        {
            _others = others;

            State = new PokemonState
            {
                IsFreeRoaming = true,
                SpeciesId = member.SpeciesId,
                SpriteFileName = member.SpriteFileName,
                IsShiny = member.IsShiny
            };
            State.StartSubPetAnimationOnly();

            double rawScale = settings.RoamingPetScale > 0 ? settings.RoamingPetScale : 2.0;
            _scale = Math.Min(rawScale, 3.0);
            _isTaskbarMode = settings.IsTaskbarMode;
            _speed = BASE_ROAMING_SPEED * (0.8 + _rnd.NextDouble() * 0.4);

            // 가장 큰 모션 프레임이 잘리지 않는 크기로 창을 만듭니다.
            var maxFrame = BinSpriteReader.GetMaxFrameSize(AppPaths.SpritePath(State.SpriteFileName));
            double baseWidth = Math.Max(50.0, maxFrame.Width) * SPRITE_ZOOM;
            double baseHeight = Math.Max(50.0, maxFrame.Height) * SPRITE_ZOOM;
            _width = baseWidth * _scale;
            _height = baseHeight * _scale;

            Window = CreateWindow(baseWidth, baseHeight);
        }

        private Window CreateWindow(double baseWidth, double baseHeight)
        {
            var window = new Window
            {
                Width = _width,
                Height = _height,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowActivated = false,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            window.SourceInitialized += (s, e) =>
            {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW & ~WS_EX_APPWINDOW);
            };

            window.KeyDown += (s, e) =>
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W) ToggleWalkRequested?.Invoke();
            };

            var roamingView = new RoamingView
            {
                DataContext = State,
                Width = baseWidth / SPRITE_ZOOM,
                Height = baseHeight / SPRITE_ZOOM,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                LayoutTransform = new System.Windows.Media.ScaleTransform(_scale * SPRITE_ZOOM, _scale * SPRITE_ZOOM)
            };

            var containerGrid = new System.Windows.Controls.Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(containerGrid, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
            System.Windows.Media.RenderOptions.SetEdgeMode(containerGrid, System.Windows.Media.EdgeMode.Aliased);
            containerGrid.Children.Add(roamingView);
            window.Content = containerGrid;

            window.Closed += (s, e) =>
            {
                _moveTimer.Stop();
                State.StopAnimationLoop();
                Closed?.Invoke(this);
            };
            window.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _moveTimer.Tick += (s, e) => Step();

            return window;
        }

        // 주 화면의 임의 위치에 나타나 걷기 시작합니다.
        public void Show()
        {
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                Window.Left = _rnd.Next(primaryScreen.Bounds.Left, primaryScreen.Bounds.Right - (int)_width);
                Window.Top = _isTaskbarMode ? primaryScreen.WorkingArea.Bottom - _height : _rnd.Next(primaryScreen.Bounds.Top, primaryScreen.Bounds.Bottom - (int)_height);
            }
            Window.Show();

            (_targetX, _targetY) = PickTargetOnCurrentScreen();
            SetAction(PokemonState.ANIM_WALK);
            _moveTimer.Start();
        }

        public void Close() => Window.Close();

        private System.Windows.Forms.Screen CurrentScreen(double x, double y) =>
            System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)x, (int)y));

        private (double x, double y) PickTargetOnCurrentScreen()
        {
            var screen = CurrentScreen(Window.Left + _width / 2, Window.Top + _height / 2);
            var b = screen.Bounds;
            double tX = _rnd.Next(b.Left, (int)Math.Max(b.Left + 10, b.Right - _width));
            double tY = _isTaskbarMode ? screen.WorkingArea.Bottom - _height : _rnd.Next(b.Top, (int)Math.Max(b.Top + 10, b.Bottom - _height));
            return (tX, tY);
        }

        // 스프라이트에 해당 모션이 없으면 UpdateAnimation이 알아서 IDLE 스트립으로 대체합니다.
        private void SetAction(int animId, int duration = 9999) => State.SetPlayModeAction(animId, duration > 0 ? duration : 9999);

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            _moveTimer.Stop();
            _behavior = Behavior.Walking;
            _restCounter = 0;
            State.IsRummaging = false; // 뒤적이던 중이면 중단 (아이템은 남아 있다가 다시 연출)
            SetAction(PokemonState.ANIM_HURT);

            try { Window.DragMove(); } catch { }

            (_targetX, _targetY) = PickTargetOnCurrentScreen();
            SetAction(PokemonState.ANIM_WALK);
            _moveTimer.Start();
        }

        // 33ms마다: 쉬는 중이면 남은 시간 처리, 아니면 이벤트 확인 후 목적지로 이동
        private void Step()
        {
            if (_interactionCooldown > 0) _interactionCooldown--;

            if (_restCounter > 0)
            {
                StepRest();
                return;
            }

            if (TryStartRummage() || TryStartInteraction()) return;

            MoveTowardTarget();
        }

        private void StepRest()
        {
            _restCounter--;

            switch (_behavior)
            {
                case Behavior.Sleeping:
                    if (_restCounter == 30) SetAction(PokemonState.ANIM_WAKE, 30);
                    break;
                case Behavior.Sinking:
                    if (_restCounter == 60) SetAction(PokemonState.ANIM_LEAPFORTH, 60);
                    else if (_restCounter == 30) SetAction(PokemonState.ANIM_HITGROUND, 30);
                    break;
                case Behavior.Rummaging:
                    // 풀숲 뒤적이기: 뛰어들기 → 뒤적뒤적 → 아이템 들고 깡총
                    if (_restCounter == RUMMAGE_TICKS - 30) SetAction(PokemonState.ANIM_PULL, 60);
                    else if (_restCounter == 40)
                    {
                        State.IsRummaging = false;
                        SetAction(PokemonState.ANIM_HOP, 40);
                        if (PendingFoundItem != null)
                        {
                            State.ShowRoamingBubble(PendingFoundItem.Icon);
                            PendingFoundItem = null;
                        }
                    }
                    break;
            }

            if (_restCounter > 0) return;

            // 쉬는 시간이 끝나면 새 목적지로 출발 (다른 펫과 놀았다면 살짝 떨어진 곳으로)
            (_targetX, _targetY) = _behavior == Behavior.Interacting ? PickEscapeTarget() : PickTargetOnCurrentScreen();
            _behavior = Behavior.Walking;
            SetAction(PokemonState.ANIM_WALK);
        }

        private (double x, double y) PickEscapeTarget()
        {
            const double escapeDistance = 120.0;
            double angle = _rnd.NextDouble() * Math.PI * 2;
            var screen = CurrentScreen(Window.Left + _width / 2, Window.Top + _height / 2);
            var b = screen.Bounds;

            double x = Math.Clamp(Window.Left + Math.Cos(angle) * escapeDistance, b.Left, b.Right - _width);
            double y = _isTaskbarMode ? screen.WorkingArea.Bottom - _height
                                      : Math.Clamp(Window.Top + Math.Sin(angle) * escapeDistance, b.Top, b.Bottom - _height);
            return (x, y);
        }

        // 아이템을 줍게 된 펫: 걷다가 그 자리의 풀숲으로 뛰어듭니다.
        private bool TryStartRummage()
        {
            if (_behavior != Behavior.Walking || PendingFoundItem == null) return false;

            _behavior = Behavior.Rummaging;
            _restCounter = RUMMAGE_TICKS;
            State.IsRummaging = true;
            SetAction(PokemonState.ANIM_LEAPFORTH, 30);
            return true;
        }

        // 다른 펫과 가까워지면 서로 바라보며 깡총 뛰거나 툭 칩니다.
        private bool TryStartInteraction()
        {
            if (_interactionCooldown > 0 || _behavior != Behavior.Walking) return false;

            foreach (var other in _others)
            {
                if (other == this) continue;

                double diffX = other.Window.Left - Window.Left;
                double diffY = other.Window.Top - Window.Top;
                if (Math.Sqrt(diffX * diffX + diffY * diffY) >= 50.0 * _scale) continue;

                _behavior = Behavior.Interacting;
                _restCounter = 45;
                _interactionCooldown = 150;
                State.Direction = diffX > 0 ? 2 : 6;
                SetAction(_rnd.Next(0, 2) == 0 ? PokemonState.ANIM_HOP : PokemonState.ANIM_STRIKE, 45);
                return true;
            }
            return false;
        }

        private void MoveTowardTarget()
        {
            double dx = _targetX - Window.Left;
            double dy = _targetY - Window.Top;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < 5.0)
            {
                StartArrivalBehavior();
                return;
            }

            Window.Left += dx / distance * _speed;
            Window.Top += dy / distance * _speed;

            if (_isTaskbarMode)
            {
                Window.Top = CurrentScreen(Window.Left, Window.Top).WorkingArea.Bottom - _height;
                State.Direction = dx > 0 ? 2 : 6;
            }
            else
            {
                State.FlipX = 1;
                State.Direction = DirectionFromAngle(Math.Atan2(dy, dx) * 180 / Math.PI);
            }
        }

        // PMD 스프라이트 방향 행: 0=아래, 1=오른아래, 2=오른쪽, 3=오른위, 4=위, 5=왼위, 6=왼쪽, 7=왼아래
        private static int DirectionFromAngle(double angle)
        {
            if (angle < 0) angle += 360;
            if (angle >= 337.5 || angle < 22.5) return 2;
            if (angle < 67.5) return 1;
            if (angle < 112.5) return 0;
            if (angle < 157.5) return 7;
            if (angle < 202.5) return 6;
            if (angle < 247.5) return 5;
            if (angle < 292.5) return 4;
            return 3;
        }

        // 목적지에 도착하면 정면을 보고 잠시 행동 (대기/포즈/심호흡/끄덕/낮잠/털썩)
        private void StartArrivalBehavior()
        {
            State.Direction = 0;
            State.FlipX = 1;

            int roll = _rnd.Next(0, 100);
            if (roll < 18) Rest(PokemonState.ANIM_IDLE, 60);
            else if (roll < 36) Rest(PokemonState.ANIM_POSE, 40);
            else if (roll < 52) Rest(PokemonState.ANIM_DEEPBREATH, 60);
            else if (roll < 68) Rest(PokemonState.ANIM_NOD, 60);
            else if (roll < 84) { _behavior = Behavior.Sleeping; Rest(PokemonState.ANIM_SLEEP, 300); }
            else { _behavior = Behavior.Sinking; Rest(PokemonState.ANIM_SINK, 90); }
        }

        private void Rest(int animId, int ticks)
        {
            SetAction(animId, ticks);
            _restCounter = ticks;
        }
    }
}
