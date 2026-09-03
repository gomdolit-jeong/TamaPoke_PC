# TamaPoke (다마포케 - Desktop Version)

> C# WPF와 MVVM 패턴을 기반으로 재해석한 레트로 감성 포켓몬 다마포케 데스크톱 위젯 게임입니다. 바탕화면 위에서 나만의 포켓몬을 키우고, 진화시키며, 파티 관리와 배틀·포획 시스템을 즐길 수 있습니다.

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
