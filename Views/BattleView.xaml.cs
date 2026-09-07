using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TamaPoke.Models;

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

        private void UseMonsterBall_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

            if (pet.MonsterBalls <= 0)
            {
                pet.BattleMessage = "몬스터볼이 부족합니다!";
                return;
            }

            pet.IsBattleInventoryOpen = false;
            pet.MonsterBalls--;

            Catch_Click(sender, e);
        }

        private async void UsePotion_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

            if (pet.Potions <= 0)
            {
                pet.BattleMessage = "상처약이 부족합니다!";
                return;
            }

            pet.IsBattleInventoryOpen = false;
            pet.Potions--;

            var potionItem = new ItemInfo { Type = ItemType.Potion, Name = "상처약" };
            await pet.ExecuteItemTurnAsync(potionItem);
        }

        // ==========================================
        // 전투 종료 및 포획 애니메이션 관련 이벤트
        // ==========================================
        private async void Catch_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

            if (pet.MonsterBalls <= 0)
            {
                pet.BattleMessage = "몬스터볼이 부족합니다!";
                return;
            }

            pet.IsPlayerTurn = false;
            pet.IsCatchOffered = false;

            pet.BattleMessage = "가라! 몬스터볼!";

            PokeballIcon.Visibility = Visibility.Visible;

            var transformGroup = PokeballIcon.RenderTransform as TransformGroup;
            if (transformGroup == null) return;

            var rotTransform = transformGroup.Children[0] as RotateTransform;
            var transTransform = transformGroup.Children[1] as TranslateTransform;

            if (rotTransform == null || transTransform == null) return;

            transTransform.X = 0;
            transTransform.Y = 0;
            rotTransform.Angle = 0;

            double finalX = 140;
            var animX = new DoubleAnimation(0, finalX, TimeSpan.FromMilliseconds(500));

            var animY = new DoubleAnimationUsingKeyFrames();
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-90, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-65, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), new QuadraticEase { EasingMode = EasingMode.EaseIn }));

            var animRot = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(500));

            transTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            transTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            rotTransform.BeginAnimation(RotateTransform.AngleProperty, animRot);

            await Task.Delay(500);
            pet.IsEnemyVisible = false;

            int shakes = new Random().Next(2, 4);
            for (int i = 1; i <= shakes; i++)
            {
                pet.BattleMessage = $"볼이 흔들흔들... ({i})";

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
        // 🌟 스킬 학습 및 교체 UI 버튼 이벤트들
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