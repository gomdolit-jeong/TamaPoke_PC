using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TamaPoke.Models
{
    // 🌟 partial 키워드를 사용하여 기존 PokemonState 클래스와 연결합니다!
    public partial class PokemonState
    {
        [JsonIgnore] public bool IsFreeRoaming { get; set; } = false;

        // 🌟 방향을 조절할 수 있는 속성 (0:앞, 2:옆, 4:뒤)
        private int _direction = 0;
        [JsonIgnore] public int Direction
        {
            get => _direction;
            set
            {
                if (SetProperty(ref _direction, value))
                {
                    // 방향이 바뀌면 애니메이션 프레임을 초기화하여 새로 자르도록 유도합니다.
                    _animationFrames = null;
                    CheckStateAndAnimate();
                }
            }
        }

        // 🌟 외부(MainWindow)에서 애니메이션을 강제로 실행하는 메서드
        public void SetPlayModeAction(int actionId, int timer = 9999)
        {
            // 기존 파일에 선언되어 있던 비공개 변수들을 자유롭게 사용할 수 있습니다!
            PlayTempAction(actionId, timer);
            CheckStateAndAnimate();
        }

        // 산책용 펫 창이 닫힐 때 애니메이션 타이머를 멈춰 타이머가 계속 쌓이지 않게 합니다.
        public void StopAnimationLoop()
        {
            _mainLoopTimer?.Stop();
            _animationFrames = null;
        }
    }
}