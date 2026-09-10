# TamaPoke (다마포케 - Desktop Version)

> C# WPF와 MVVM 패턴을 기반으로 재해석한 레트로 감성 포켓몬 다마포케 데스크톱 위젯 게임입니다. 바탕화면 위에서 나만의 포켓몬을 키우고, 진화시키며, 파티 관리와 배틀·포획 시스템을 즐길 수 있습니다.

---

## 🎮 사용 설명서 (User Manual)

TamaPoke는 바탕화면 위에 둥둥 떠 있는(Floating) 반투명 위젯 형태로 동작합니다. 포켓몬을 마우스 좌클릭하여 바탕화면 어디로든 자유롭게 이동시킬 수 있습니다.

### 🔘 하단 액션 버튼 (Action Buttons)
메인 화면 하단의 4가지 버튼을 통해 포켓몬의 핵심 상태(컨디션)를 관리합니다.
* 🍎 **먹이 주기 (Feed)**: 맛있는 열매를 주어 '포만감'을 채워줍니다.
* 🎮 **놀아주기 (Play)**: 포켓몬과 놀아주며 '행복도'를 높이거나 야생 배틀을 진행합니다.
* 🌙 **재우기 (Sleep)**: 쿨쿨 재워서 '기력'을 회복시킵니다.
* 🫧 **씻겨주기 (Clean)**: 깨끗하게 목욕시켜 '청결도'를 유지합니다.

### 🖱️ 우클릭 메뉴 (Context Menu)
포켓몬을 마우스 오른쪽 버튼으로 클릭하면, 다음과 같은 다양한 시스템에 접근할 수 있습니다.
* 📖 **포켓몬 도감 열기**: 지금까지 만났거나 포획한 포켓몬의 수집 기록을 확인합니다.
* 📋 **상세 프로필 열기**: 포켓몬의 레벨과 전투 능력치, 장착 중인 스킬, 획득한 체육관 뱃지 및 메달을 확인합니다.
* 📁 **파티 보기**: 최대 6마리로 구성된 파티 멤버를 관리하고 메인 화면에 나올 포켓몬을 교체합니다.
* 🎒 **가방 열기**: 야생 포켓몬을 잡을 수 있는 몬스터볼과 체력을 회복하는 상처약 등 아이템 인벤토리를 확인합니다.
* 🍃 **자연으로 놔주기**: 정든 포켓몬을 자연으로 방생하여 이별합니다.

---

## 🌟 주요 기능 (Key Features)

* **데스크톱 플로팅 위젯**: 윈도우 바탕화면에서 항상 귀여운 포켓몬과 함께하며 실시간으로 상태를 관리할 수 있습니다. 반투명 창 및 마우스 오버 투명도 조절 기능을 지원합니다.
* **생애 주기 및 육성 시스템**: 
  * 알에서 부화하여 밥 주기, 놀아주기, 재우기, 씻기 등을 통해 4가지 핵심 상태(포만감, 행복, 기력, 청결)를 관리합니다.
  * 시간에 따른 성장과 다중 분기 진화 시스템을 지원합니다. (컨디션 조건 및 진화 보류 기능 포함)
  * 수명(나이) 도달 시 감동적인 작별(이별), 방생, 그리고 가출 메커니즘이 안전하게 작동합니다.
* **파티 및 배틀/포획 시스템**: 
  * 최대 6마리의 포켓몬을 파티에 보관하고 대표(메인) 포켓몬과 자유롭게 교체할 수 있습니다.
  * 파티가 꽉 찬 상태에서 새로운 포켓몬을 포획할 경우, 직관적인 인게임 오버레이 팝업을 통해 기존 멤버를 교체하거나 방생할 수 있습니다.
  * 1마리만 남은 상태에서의 방생·가출 시 데이터 꼬임(복제 버그)을 방지하는 안전한 동기화 로직을 탑재했습니다.
* **JSON 기반 데이터 영속성**: 진행 상황, 도감, 파티원 정보가 세이브 파일로 안전하게 저장 및 로드됩니다.

---

## 🛠️ 기술 스택 (Tech Stack)

* **Language**: C#, C++
* **Framework**: WPF (Windows Presentation Foundation), .NET
* **Architecture**: MVVM 패턴
* **Data Storage**: JSON

---

## 🔗 참고 오픈소스 및 원작 리포지토리 (References)

본 데스크톱 프로젝트는 아래의 임베디드/웹 기반 원작 `TamaPoke` 프로젝트들의 게임 루프, 기획 및 아이디어를 참고하여 데스크톱 환경에 맞게 재구성되었습니다:

* [socquique/TamaPoke](https://github.com/socquique/TamaPoke) — 원작 펌웨어 및 기본 다마고치 메커니즘
* [ShadowEnemyx/TamaPoke (Expanded Update)](https://github.com/ShadowEnemyx/TamaPoke/tree/tamapoke-expanded-update) — 확장 기능 및 콘텐츠 레퍼런스
* [DylanPDao/TamaPoke](https://github.com/DylanPDao/TamaPoke) — 배틀 및 추가 시스템 확장 아이디어

---

## 📝 Credits & Acknowledgements

* **Sprite Assets**: 게임 내 사용된 모든 포켓몬 도트 스프라이트(Sprites)는 포켓몬 불가사의 던전 커뮤니티의 오픈소스 프로젝트인 [PMDCollab/SpriteCollab](https://github.com/PMDCollab/SpriteCollab)의 원본 리소스를 활용하였습니다. 고품질의 스프라이트를 제공해 주신 커뮤니티 기여자분들께 깊은 감사를 드립니다.
  * 해당 리소스는 **Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)** 라이선스에 따라 비상업적 목적으로만 사용되었습니다.
  * 라이선스 전문 확인: [https://creativecommons.org/licenses/by-nc/4.0/](https://creativecommons.org/licenses/by-nc/4.0/)
* **Badge Images**: 본 프로젝트에 사용된 체육관 뱃지 이미지는 [Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Main_Page)의 자료를 참고 및 인용하였습니다[cite: 9]. 
* **Disclaimer**: 이 프로젝트는 비상업적 팬 프로젝트(Fan Project)이며, 포켓몬스터(Pokémon)와 관련된 모든 저작권 및 상표권은 Nintendo, Creatures Inc., GAME FREAK inc. 에 귀속되어 있습니다[cite: 9].