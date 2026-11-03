using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TamaPoke.Models;
using TamaPoke.Utils;

namespace TamaPoke.Views
{
    public partial class BattleView : System.Windows.Controls.UserControl
    {
        public BattleView()
        {
            InitializeComponent();
            this.DataContextChanged += BattleView_DataContextChanged;
        }

        private void BattleView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is PokemonState oldState)
            {
                oldState.RequestCatchAnimation -= TriggerCatchAnimation;
                oldState.RequestSkillFx -= PlaySkillFxAsync;
                oldState.RequestStatFx -= PlayStatFxAsync;
            }
            if (e.NewValue is PokemonState newState)
            {
                newState.RequestCatchAnimation += TriggerCatchAnimation;
                newState.RequestSkillFx += PlaySkillFxAsync;
                newState.RequestStatFx += PlayStatFxAsync;
            }
        }

        private void TriggerCatchAnimation()
        {
            Catch_Click(this, new RoutedEventArgs());
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

        // ==========================================
        // 스프라이트 배치: 대기 모션이 들어갈 칸과 왼쪽 위 위치
        // ==========================================
        private const double ENEMY_SPRITE_BOX = 72, ENEMY_SPRITE_LEFT = 178, ENEMY_SPRITE_TOP = 54;
        private const double PLAYER_SPRITE_BOX = 108, PLAYER_SPRITE_LEFT = 26, PLAYER_SPRITE_TOP = 105;

        // 모션마다 그림 칸 크기가 다르므로, 대기 모션 기준의 배율과 중심을 그대로 유지합니다. (공격 모션에서 작아지지 않게)
        private void Sprite_TargetUpdated(object? sender, System.Windows.Data.DataTransferEventArgs e)
        {
            var pet = GetPet();
            if (pet == null || sender is not System.Windows.Controls.Image sprite || sprite.Source is not BitmapSource frame) return;

            bool enemy = sprite == EnemySprite;
            var idle = enemy ? pet.EnemyIdleFrameSize : pet.PlayerIdleFrameSize;
            if (idle.Width <= 0 || idle.Height <= 0) idle = new System.Windows.Size(frame.PixelWidth, frame.PixelHeight);

            double box = enemy ? ENEMY_SPRITE_BOX : PLAYER_SPRITE_BOX;
            double scale = box / Math.Max(idle.Width, idle.Height);
            double centerX = (enemy ? ENEMY_SPRITE_LEFT : PLAYER_SPRITE_LEFT) + idle.Width * scale / 2;
            double centerY = (enemy ? ENEMY_SPRITE_TOP : PLAYER_SPRITE_TOP) + idle.Height * scale / 2;

            sprite.Width = frame.PixelWidth * scale;
            sprite.Height = frame.PixelHeight * scale;
            sprite.Margin = new Thickness(centerX - sprite.Width / 2, centerY - sprite.Height / 2, 0, 0);
        }

        // ==========================================
        // 기술 이펙트 (Assets/UI의 fx_*.png: 가로로 이어 붙인 정사각형 프레임)
        // ==========================================
        private const int FX_FRAME_MS = 80;        // 터지는 이펙트 한 프레임
        private const int SHOT_STEP_MS = 35;       // 날아가는 이펙트 한 칸
        private const int SHOT_STEPS = 9;
        private const int SHOT_FRAME_EVERY = 2;    // 몇 칸마다 투사체 그림을 바꿀지
        private const double ENEMY_FX_SCALE = 2;   // 아트 1픽셀당 크기: 앞쪽의 내 포켓몬은 스프라이트처럼 1.5배
        private const double PLAYER_FX_SCALE = 3;
        private const double SHOT_SCALE = 2;
        private const double FX_GROUND_ROW = 14;   // 32x32 그림 중심에서 바닥 줄까지 (아트 픽셀)
        private const double FX_EMPTY_BOTTOM_ROWS = 2;
        private static readonly HashSet<PokemonType> GroundFxTypes = new() { PokemonType.Ground, PokemonType.Fire, PokemonType.Ice };

        private static readonly Dictionary<string, BitmapSource[]> FxFrames = new();

        private static BitmapSource[] GetFxFrames(string name)
        {
            if (FxFrames.TryGetValue(name, out var frames)) return frames;
            var sheet = new BitmapImage(new Uri($"pack://application:,,,/TamaPoke;component/Assets/UI/{name}.png"));
            int size = sheet.PixelHeight;
            frames = new BitmapSource[sheet.PixelWidth / size];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = new CroppedBitmap(sheet, new Int32Rect(i * size, 0, size, size));
                frames[i].Freeze();
            }
            return FxFrames[name] = frames;
        }

        // 스프라이트에서 실제로 보이는 부분의 범위 (포켓몬마다 크기와 칸 안의 위치가 다릅니다)
        private System.Windows.Rect FxBounds(bool enemy)
        {
            BattleArea.UpdateLayout(); // 방금 모션이 바뀌었으면 새 크기와 위치로 계산
            var sprite = enemy ? EnemySprite : PlayerSprite;
            var bounds = new System.Windows.Rect(0, 0, sprite.ActualWidth, sprite.ActualHeight);
            if (sprite.Source is BitmapSource source)
            {
                var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                int w = bgra.PixelWidth, h = bgra.PixelHeight;
                var pixels = new byte[w * h * 4];
                bgra.CopyPixels(pixels, w * 4, 0);

                int left = w, right = -1, top = h, bottom = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[(y * w + x) * 4 + 3] == 0) continue;
                        left = Math.Min(left, x); right = Math.Max(right, x);
                        top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                    }
                }
                if (right >= 0)
                {
                    double sx = sprite.ActualWidth / w, sy = sprite.ActualHeight / h;
                    bounds = new System.Windows.Rect(left * sx, top * sy, (right - left + 1) * sx, (bottom - top + 1) * sy);
                }
            }
            return new System.Windows.Rect(sprite.TranslatePoint(bounds.TopLeft, BattleArea), bounds.Size);
        }

        // 이펙트 중심: 보통은 몸통 중심, 땅에서 솟는 이펙트는 그림의 바닥 줄이 발밑에 오도록
        private System.Windows.Point FxCenter(bool enemy, bool onGround, double scale)
        {
            var bounds = FxBounds(enemy);
            double x = bounds.Left + bounds.Width / 2;
            return new System.Windows.Point(x, onGround ? bounds.Bottom - FX_GROUND_ROW * scale : bounds.Top + bounds.Height / 2);
        }

        // 그림이 오른쪽을 향해 그려져 있으니 내 포켓몬 쪽으로 갈 때는 뒤집습니다.
        private void ShowFx(BitmapSource frame, System.Windows.Point center, double scale, bool targetIsEnemy)
        {
            double size = frame.PixelHeight * scale;
            SkillFx.Source = frame;
            SkillFx.Width = SkillFx.Height = size;
            // 아래쪽 메시지 창에 잘리지 않도록 전투 화면 안에 맞춥니다. (그림 맨 아래 줄은 거의 비어 있어 살짝 넘어가도 됩니다)
            double top = Math.Min(center.Y - size / 2, BattleArea.ActualHeight - size + FX_EMPTY_BOTTOM_ROWS * scale);
            SkillFx.Margin = new Thickness(Math.Round(center.X - size / 2), Math.Round(top), 0, 0);
            SkillFxFlip.ScaleX = targetIsEnemy ? 1 : -1;
            SkillFx.Visibility = Visibility.Visible;
        }

        private async Task PlayBurstAsync(string name, bool targetIsEnemy, bool onGround = false)
        {
            double scale = targetIsEnemy ? ENEMY_FX_SCALE : PLAYER_FX_SCALE;
            var center = FxCenter(targetIsEnemy, onGround, scale);
            foreach (var frame in GetFxFrames(name))
            {
                ShowFx(frame, center, scale, targetIsEnemy);
                await Task.Delay(FX_FRAME_MS);
            }
            SkillFx.Visibility = Visibility.Collapsed;
        }

        // 특수 기술은 쓴 쪽에서 날아가서 터지고, 나머지는 맞는 쪽에서 바로 터집니다.
        private async Task PlaySkillFxAsync(SkillInfo skill, bool targetIsEnemy)
        {
            string type = skill.Type.ToString().ToLowerInvariant();
            if (skill.Category == SkillCategory.Special)
            {
                var frames = GetFxFrames($"fx_shot_{type}");
                System.Windows.Point from = FxCenter(!targetIsEnemy, false, SHOT_SCALE), to = FxCenter(targetIsEnemy, false, SHOT_SCALE);
                for (int i = 0; i <= SHOT_STEPS; i++)
                {
                    double t = (double)i / SHOT_STEPS;
                    var at = new System.Windows.Point(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
                    ShowFx(frames[i / SHOT_FRAME_EVERY % frames.Length], at, SHOT_SCALE, targetIsEnemy);
                    await Task.Delay(SHOT_STEP_MS);
                }
            }
            await PlayBurstAsync($"fx_skill_{type}", targetIsEnemy, GroundFxTypes.Contains(skill.Type));
        }

        private Task PlayStatFxAsync(bool rose, bool targetIsEnemy) => PlayBurstAsync(rose ? "fx_stat_up" : "fx_stat_down", targetIsEnemy);

        // ==========================================
        // 메인 메뉴 및 공격 관련 이벤트
        // ==========================================
        private void ShowAttackMenu_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsAttackMenuOpen = true;
        }

        private void CancelAttack_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsAttackMenuOpen = false;
        }

        // 메가진화: 버튼 → (폼이 여러 개면 선택 창) → 연출
        private void MegaEvolve_Click(object sender, RoutedEventArgs e) => GetPet()?.RequestMegaEvolution();

        private void MegaChoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MegaFormInfo form) GetPet()?.ChooseMegaForm(form);
        }

        private void MegaCancel_Click(object sender, RoutedEventArgs e) => GetPet()?.CancelMegaChoice();

        private async void SkillButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is SkillInfo selectedSkill)
            {
                if (this.DataContext is PokemonState pet)
                {
                    pet.IsAttackMenuOpen = false;
                    await pet.ExecuteSkillTurnAsync(selectedSkill);
                }
            }
        }

        private async void Dodge_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.Dodge);
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.Run);
        }

        // ==========================================
        // 가방(인벤토리) 메뉴 관련 이벤트
        // ==========================================
        private void OpenInventory_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsBattleInventoryOpen = true;
        }

        private void CloseInventory_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsBattleInventoryOpen = false;
        }

        // 🌟 여러 종류의 아이템을 동적으로 처리하는 통합 메서드
        private async void UseBattleItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ItemInfo clickedItem)
            {
                var pet = GetPet();
                if (pet == null) return;

                if (clickedItem.Quantity <= 0)
                {
                    pet.BattleMessage = Loc.F("battle.notEnough", clickedItem.Name);
                    return;
                }

                if (clickedItem.Type == ItemType.monsterball)
                {
                    if (pet.IsGymBattle)
                    {
                        pet.BattleMessage = Loc.T("battle.noBallInGym");
                        return;
                    }
                    pet.IsBattleInventoryOpen = false;

                    // 🌟 수동 차감 로직 삭제 (Catch_Click에서 알아서 차감합니다)
                    Catch_Click(sender, e);
                }
                else if (clickedItem.Type == ItemType.Potion)
                {
                    // 🌟 [핵심 수정] 체력이 가득 찼는지 먼저 검사합니다.
                    if (pet.PlayerHp >= pet.PlayerMaxHp)
                    {
                        // 턴을 넘기지 않고, 아이템도 소모하지 않은 채 경고 메시지만 보냅니다.
                        pet.BattleMessage = Loc.T("battle.hpFull");
                        return;
                    }

                    // 체력이 깎여 있을 때만 아래 로직(아이템 소모 및 턴 넘김)이 실행됩니다.
                    pet.IsBattleInventoryOpen = false;

                    clickedItem.Quantity--;
                    if (clickedItem.Quantity <= 0) pet.Inventory.Remove(clickedItem);

                    await pet.ExecuteItemTurnAsync(clickedItem);
                }
            }
        }

        // ==========================================
        // 전투 종료 및 포획 애니메이션 관련 이벤트
        // ==========================================
        private const double BALL_START_ABOVE_PLATFORM = 40; // 던지는 위치: 내 발판 중심에서 위로 (몸통 높이)
        private const double BALL_REST_ABOVE_PLATFORM = 8;   // 떨어진 볼이 놓이는 위치: 적 발판 중심에서 위로
        private const double BALL_ARC_HEIGHT = 12;           // 포물선 꼭대기 높이 (적 이름 패널 아래로 지나가도록)

        private async void Catch_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

            // 🌟 [시퀀스 4] 애니메이션을 시작하기 전에 몬스터볼 개수를 검사합니다!
            if (pet.MonsterBalls <= 0)
            {
                pet.IsCatchOffered = false;
                pet.BattleMessage = Loc.T("battle.noBalls");
                await Task.Delay(2000);

                // [시퀀스 5] 다시 포획/그냥가기 선택 창 띄우기
                pet.BattleMessage = Loc.T("battle.askCatch");
                pet.IsCatchOffered = true;
                return;
            }

            // 🌟 몬스터볼이 있다면 1개를 차감하고 UI 메뉴를 닫습니다.
            pet.MonsterBalls--;
            pet.Save();
            pet.IsPlayerTurn = false;
            pet.IsCatchOffered = false;

            pet.BattleMessage = Loc.T("battle.throw");

            PokeballIcon.Visibility = Visibility.Visible;

            var transformGroup = PokeballIcon.RenderTransform as TransformGroup;
            if (transformGroup == null) return;

            var rotTransform = transformGroup.Children[0] as RotateTransform;
            var transTransform = transformGroup.Children[1] as TranslateTransform;

            if (rotTransform == null || transTransform == null) return;

            // 내 포켓몬 몸통에서 출발해 포물선을 그리며 적 발판 위에 떨어집니다. (발판 위치로 계산하므로 배치가 바뀌어도 맞음)
            System.Windows.Point Center(FrameworkElement el) => el.TranslatePoint(new System.Windows.Point(el.ActualWidth / 2, el.ActualHeight / 2), BattleArea);
            double half = PokeballIcon.Width / 2;
            System.Windows.Point from = Center(PlayerPlatform), to = Center(EnemyPlatform);
            double startX = from.X - half, startY = from.Y - BALL_START_ABOVE_PLATFORM - half;
            double finalX = to.X - half, finalY = to.Y - BALL_REST_ABOVE_PLATFORM - half;
            double peakY = Math.Min(startY, finalY) - BALL_ARC_HEIGHT;

            transTransform.X = startX;
            transTransform.Y = startY;
            rotTransform.Angle = 0;

            var animX = new DoubleAnimation(startX, finalX, TimeSpan.FromMilliseconds(500));

            var animY = new DoubleAnimationUsingKeyFrames();
            animY.KeyFrames.Add(new DiscreteDoubleKeyFrame(startY, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(peakY, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(finalY, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), new QuadraticEase { EasingMode = EasingMode.EaseIn }));

            var animRot = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(500));

            transTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            transTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            rotTransform.BeginAnimation(RotateTransform.AngleProperty, animRot);

            await Task.Delay(500);
            pet.IsEnemyVisible = false;

            int shakes = new Random().Next(2, 4);
            for (int i = 1; i <= shakes; i++)
            {
                pet.BattleMessage = Loc.F("battle.wobble", i);

                var shakeAnim = new DoubleAnimation(finalX - 6, finalX + 6, TimeSpan.FromMilliseconds(100))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2)
                };

                transTransform.BeginAnimation(TranslateTransform.XProperty, shakeAnim);
                await Task.Delay(600);
            }

            transTransform.BeginAnimation(TranslateTransform.XProperty, null);
            transTransform.BeginAnimation(TranslateTransform.YProperty, null);
            rotTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            PokeballIcon.Visibility = Visibility.Collapsed;

            await pet.ExecuteCatchResultAsync();
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.LeaveWildBattle();
        }

        // ==========================================
        // 스킬 학습 및 교체 UI 버튼 이벤트들
        // ==========================================
        private void BtnLearnSkill1_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.SelectRecommendedSkill(1);
        }

        private void BtnLearnSkill2_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.SelectRecommendedSkill(2);
        }

        private void BtnSkipSkill_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.SkipSkillLearning();
        }

        private void BtnReplaceSkill0_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.ReplaceExistingSkill(0);
        }

        private void BtnReplaceSkill1_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.ReplaceExistingSkill(1);
        }

        private void BtnReplaceSkill2_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.ReplaceExistingSkill(2);
        }

        private void BtnReplaceSkill3_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.ReplaceExistingSkill(3);
        }
    }
}