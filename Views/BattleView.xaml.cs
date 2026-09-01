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

        // 🌟 인벤토리에서 신호탄이 날아오면 실행되는 함수
        private void TriggerCatchAnimation()
        {
            Catch_Click(this, new RoutedEventArgs());
        }

        private PokemonState? GetPet() => DataContext as PokemonState;

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

        private async void QuickAttack_Click(object sender, RoutedEventArgs e)
        {
            CancelAttack_Click(sender, e);
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.QuickAttack);
        }

        private async void HeavyAttack_Click(object sender, RoutedEventArgs e)
        {
            CancelAttack_Click(sender, e);
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.HeavyAttack);
        }

        private async void Dodge_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.Dodge);
        }

        private async void Rest_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.Rest);
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) await pet.ExecuteTurnAsync(BattleAction.Run);
        }

        private async void Catch_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

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

            // 🌟 몬스터볼이 상대 포켓몬 정중앙에 떨어지도록 거리(140)와 높이(-160)를 정밀 조정
            double finalX = 140;
            var animX = new DoubleAnimation(0, finalX, TimeSpan.FromMilliseconds(500));

            var animY = new DoubleAnimationUsingKeyFrames();
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-90, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), new QuadraticEase { EasingMode = EasingMode.EaseOut })); // 크게 솟구침
            animY.KeyFrames.Add(new EasingDoubleKeyFrame(-65, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), new QuadraticEase { EasingMode = EasingMode.EaseIn })); // 적 중앙으로 낙하

            var animRot = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(500));

            transTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            transTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            rotTransform.BeginAnimation(RotateTransform.AngleProperty, animRot);

            // 🌟 0.5초 대기 (몬스터볼이 적에게 닿는 정확한 순간)
            await Task.Delay(500);

            // 🌟 적 포켓몬을 화면에서 숨겨서 볼 안에 들어간 것처럼 연출!
            pet.IsEnemyVisible = false;

            int shakes = new Random().Next(2, 4);
            for (int i = 1; i <= shakes; i++)
            {
                pet.BattleMessage = $"볼이 흔들흔들... ({i})";

                // 🌟 흔들림의 범위를 최종 좌표(finalX)를 기준으로 좌우 6만큼 움직이도록 수정
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

        private async void SkillButton_Click(object sender, RoutedEventArgs e)
        {
            // 클릭된 버튼과 그 버튼에 바인딩된 스킬(SkillInfo) 정보를 가져옵니다.
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is SkillInfo selectedSkill)
            {
                if (this.DataContext is PokemonState pet)
                {
                    // 🌟 기존 ExecuteTurnAsync 함수를 수정하여 선택한 스킬을 전달하게 됩니다.
                    await pet.ExecuteSkillTurnAsync(selectedSkill);
                }
            }
        }

        // 🌟 가방 열기/닫기 이벤트
        private void OpenInventory_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsInventoryOpen = true;
        }

        private void CloseInventory_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet != null) pet.IsInventoryOpen = false;
        }

        // 🌟 가방에서 아이템 클릭 시 실행되는 이벤트
        private async void UseItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ItemInfo selectedItem)
            {
                var pet = GetPet();
                if (pet == null) return;

                if (selectedItem.Quantity <= 0)
                {
                    pet.BattleMessage = "아이템이 부족합니다!";
                    return;
                }

                // 가방을 닫습니다.
                pet.IsInventoryOpen = false;

                // 🌟 아이템 종류에 따라 알맞은 로직 실행
                if (selectedItem.Type == ItemType.monsterball)
                {
                    selectedItem.Quantity--; // 몬스터볼 개수 차감
                    Catch_Click(sender, e);  // 기존 포획 로직 실행
                }
                else if (selectedItem.Type == ItemType.Potion)
                {
                    selectedItem.Quantity--; // 상처약 개수 차감

                    // 🌟 수정됨: 방금 만든 아이템 턴 처리 비동기 함수를 호출합니다!
                    await pet.ExecuteItemTurnAsync(selectedItem);
                }
            }
        }

        // 🌟 몬스터볼 버튼 클릭 이벤트 (배틀 중 연동)
        private void UseMonsterBall_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

            if (pet.MonsterBalls <= 0)
            {
                pet.BattleMessage = "몬스터볼이 부족합니다!";
                return;
            }

            pet.IsInventoryOpen = false;
            pet.MonsterBalls--;

            // 기존에 작성해두신 완벽한 포획 애니메이션 실행!
            Catch_Click(sender, e);
        }

        // 🌟 상처약 버튼 클릭 이벤트 (배틀 중 연동)
        private void UsePotion_Click(object sender, RoutedEventArgs e)
        {
            var pet = GetPet();
            if (pet == null) return;

            if (pet.Potions <= 0)
            {
                pet.BattleMessage = "상처약이 부족합니다!";
                return;
            }

            pet.IsInventoryOpen = false;
            pet.Potions--;
            pet.PlayerHp = Math.Min(pet.PlayerMaxHp, pet.PlayerHp + 30);
            pet.BattleMessage = "상처약을 사용하여 포켓몬의 체력을 회복했다!";
        }
    }
}