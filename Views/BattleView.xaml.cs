using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            }
            if (e.NewValue is PokemonState newState)
            {
                newState.RequestCatchAnimation += TriggerCatchAnimation;
            }
        }

        private void TriggerCatchAnimation()
        {
            Catch_Click(this, new RoutedEventArgs());
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

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