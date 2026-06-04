# Claude Code 프로젝트 가이드

## 작업 스타일

- **언어**: 한국어 존댓말로만 답변. 코드/기술 용어는 영어 그대로 사용.
  - 종결어미: "~습니다", "~예요", "~합니다" / 반말("~야", "~지", "~네") ❌
- **진행 방식**: 마일스톤별로 쪼개서 각 단계마다 테스트하며 진행. 한꺼번에 많이 구현하지 않음.
- **코드 작성 방식**: 파일 생성/수정 전 변경점과 의도를 먼저 설명. 사용자가 확인 후 직접 작성. Claude가 직접 파일을 작성하지 않음. 사용자가 명시적으로 요청할 때만 파일 생성/수정.

---

## 프로젝트 개요

윷놀이 기반 보드게임. 3개 캐릭터(귀신/도깨비/물귀신)별 스킬과 AI를 가진 Unity 프로젝트.

---

## 캐릭터 상징색

| 캐릭터 | Hex |
|---|---|
| 귀신 (Gwishin) | `#A0121B` (핏빛 크림슨, 구 #FF0000) |
| 도깨비 (Dokkaebi) | `#E8751A` |
| 물귀신 (Mulgwisin) | `#0B7B8A` |

`CharacterData.themeColor` 필드에 저장됨. 파티클/UI 테마에 이 색상 기준 사용.

---

## 캐릭터 스킬 요약

### 귀신 (Gwishin)
- AI: `captureWeight 2.0`, `randomness 0.1`
- 패시브: 잡기 시 원한 +3 (`OnCapture`) — 원한 5 누적 시 검은 윷 전환
- 액티브: 경로상 적 즉시 잡기 (`OnActiveMoveEffect`) — 도깨비 씨름 우선순위 무시

### 도깨비 (Dokkaebi)
- AI: `randomness 1.5`
- 패시브: 잡힐 때 확률로 역잡기 + 검은 윷 +1 (`OnCaptureAttempt`)

### 물귀신 (Mulgwisin)
- AI: `finishWeight 1.5`, `stackWeight 2.0`
- 패시브: 잡힐 때 공격자 남은 윷 결과 전부 소모 (`OnBeingCaptured`)
- 액티브: 자신의 말 1개 희생 → 검은 윷 +2

---

## 완료된 이슈 (PR 머지 기준)

| 이슈 | 내용 |
|---|---|
| #56 | 테이블 맞은편 3D 캐릭터 (도깨비 오크 모델 + AnimatorController + GameEvents) |
| #55 | 검은 윷 던지기 연출 |
| #54 | 선공 결정 연출 (제비뽑기) |
| #37 | 캐릭터별 스킬 연출 (VFX) |
| #39 #29 | AI 말 이동 + 카메라 움직임 |
| #3 | 윷 오브젝트 던지기 물리 |
| #18 | 윷 던지기 버튼 |
| #47 | 영어/한국어 전환 |
| #24 | 일시 정지 메뉴 |
| #16 | 비주얼 강화 초기 (말, 판 등) |
| #41 | 액티브 스킬 시스템 |
| #25 | 이동 가능 말 하이라이트 |
| AI 시스템 | 캐릭터별 AI 성격 + 스킬 |
| 튜토리얼 씬 | 9스텝 인터랙티브 튜토리얼 (`TutorialScene.unity`, `TutorialManager.cs`, `InputBlocker`, `ThrowYut.ForcedResults`) |

---

## #56 핵심 구조 (참고)

- `Assets/Scripts/GameSystem/GameEvents.cs` — 정적 이벤트 클래스
  - `OnYutThrown(int)` / `OnCaptureSuccess(int)` / `OnCaptureFailed(int)` / `OnPieceFinished(int)`
- `Assets/Scripts/GameSystem/OpponentCharacterController.cs` — 이벤트 구독 + `Animator.SetTrigger` 분기
- `OpponentCharacter` AnimatorController — 상태 5개 (Idle + 반응 4종)
- 캐릭터 위치: `pos (0, -0.5, 17)`, `rot (0, 180, 0)`

---

## 다음 후보

1. **#51 UI 이벤트 연출** — 이펙트/카메라 분위기로 게임 상황 표현
