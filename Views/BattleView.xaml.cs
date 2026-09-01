using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TamaPoke.Models;

namespace TamaPoke.Views
{
    public partial class BattleView : UserControl
    {
        public BattleView()
        {
            InitializeComponent();
            this.DataContextChanged += BattleView_DataContextChanged;
        }

        private void BattleView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 이전 데이터의 연결 해제
            if (e.OldValue is PokemonState oldState)
            {
                oldState.RequestCatchAnimation -= TriggerCatchAnimation;
            }
            // 새로운 데이터(PokemonState)의 신호탄에 반응하도록 연결
            if (e.NewValue is PokemonState newState)
            {
                newState.RequestCatchAnimation += TriggerCatchAnimation;
            }
        }

        // 인벤토리 등 외부에서 신호탄이 날아오면 포획 애니메이션을 실행하는 함수
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
            if (pet != null) pet.IsAttackMenuOpen = true; // 이거 하나만 쓰면 끝! (IsMainMenuVisible 자동 false)
        }

        private void CancelAttack_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsAttackMenuOpen = false;
        }

        private async void SkillButton_Click(object sender, RoutedEventArgs e)
        {
            // 클릭된 버튼과 그 버튼에 바인딩된 스킬(SkillInfo) 정보를 가져옵니다.
            if (sender is Button btn && btn.DataContext is SkillInfo selectedSkill)
            {
                if (this.DataContext is PokemonState pet)
                {
                    // 스킬 메뉴를 닫고 선택한 스킬로 턴 진행
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
            if (pet != null) pet.IsBattleInventoryOpen = true; // 수정됨
        }

        private void CloseInventory_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsBattleInventoryOpen = false; // 수정됨
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

            pet.IsBattleInventoryOpen = false; // 수정됨
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

            pet.IsBattleInventoryOpen = false; // 수정됨
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
            pet.IsCatchOffered = false; // 승리 선택창 숨김

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

            // 몬스터볼 애니메이션 궤적 설정
            double finalX = 140;
            var animX = new DoubleAnimation(0, finalX, TimeSpan.FromMilliseconds(500));

            var animY = new DoubleAnimationUsingKeyFrames();
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-90, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-65, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), new QuadraticEase { EasingMode = EasingMode.EaseIn }));

            var animRot = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(500));

            transTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            transTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            rotTransform.BeginAnimation(RotateTransform.AngleProperty, animRot);

            // 0.5초 대기 후 볼에 들어간 것처럼 적을 숨김
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
    }
}