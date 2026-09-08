# 귀척사 (Gwicheoksa)

> 한국 전통 윷놀이(擲柶)를 귀신들의 대결로 재해석한 3D 턴제 보드게임
> Unity 6 · C# · 1인 개발 (기획 / 프로그래밍 / 연출)

촛불 하나만 켜진 한옥 안채, 낡은 상 위에 윷판이 놓여 있습니다.
마주 앉은 상대는 사람이 아닙니다. 네 개의 윷가락을 던져 네 망령을 모두 해방시키면 승리합니다.

---

## 게임 소개

정통 윷놀이 규칙(29칸 윷판 · 갈림길 · 업기 · 잡기 · 참먹이)을 그대로 유지한 뒤,
그 위에 **원한 → 검은 윷** 자원 순환과 **캐릭터 스킬**을 올린 대전형 보드게임입니다.

윷놀이의 가장 큰 문제는 "잡히면 그냥 손해"라는 점입니다.
이 게임은 잡힌 쪽에게 **원한**을 지급해, 불리해진 플레이어가 역전 자원을 쥐게 만듭니다.
운에 흔들리는 판을 캐릭터 선택과 스킬 타이밍으로 되받아치는 것이 핵심 재미입니다.

### 게임 모드

| 모드 | 설명 |
|---|---|
| **vs AI** | 캐릭터별 성격이 다른 AI와 1:1 대전 |
| **로컬 2인** | 한 화면에서 번갈아 플레이 |
| **튜토리얼** | 9스텝 인터랙티브 튜토리얼 (윷 읽기 → 이동 → 갈림길 → 업기 → 잡기 → 원한 → 검은 윷 → 완주 → 참먹이) |

한국어 / English 전환을 지원합니다. (CSV 기반 로컬라이제이션)

<!-- 스크린샷: 타이틀 / 캐릭터 선택 / 인게임 / 스킬 연출 4장 추가 예정 -->

---

## 핵심 시스템

### 윷 던지기

윷가락 4개를 실제 물리로 던져 뒷면 개수로 결과를 판정합니다. 뒷도 판정용 윷가락 1개를 별도로 추적합니다.

| 결과 | 효과 |
|---|---|
| 뒷도 | 1칸 후퇴 |
| 도 / 개 / 걸 | 1 / 2 / 3칸 전진 |
| 윷 / 모 | 4 / 5칸 전진 + 한 번 더 |

### 원한 (Grudge)

말이 잡히면 그 플레이어는 **원한**을 얻습니다. **완주 지점에 가까울수록** 더 많이 얻습니다.

- 일반 칸에서 잡힘: 원한 +3
- 완주 직전 칸에서 잡힘: 원한 **+5** — 참먹이(완주 지점)와 그 직전 칸인 날윷(외곽 마지막) · 안찌(지름길 마지막)
- 업힌 말이 함께 잡히면 말 개수만큼 추가

다 온 말을 잃을수록 크게 보상받는 구조라, 막판 잡기는 잡는 쪽에게도 부담이 됩니다.

원한이 **5** 쌓이면 자동으로 **검은 윷 1개**로 전환됩니다.

### 검은 윷 (Black Yut)

차례가 끝나기 전 언제든 추가로 던질 수 있는 도박수입니다.
결과는 **4/5 확률로 모, 1/5 확률로 뒷도** — 한 방에 판을 뒤집거나 스스로 무너집니다.

### 그 외 규칙

- **업기**: 자신의 말이 있는 칸에 도착하면 업어서 함께 이동 (한 번 업으면 분리 불가)
- **갈림길**: 지름길 진입 판정
- **참먹이**: 밟는 것만으로는 완주가 아니며, 반드시 넘어가야 완주 처리

---

## 캐릭터

각 캐릭터는 **상징색 · 패시브 · 액티브 · AI 성격**을 모두 다르게 가집니다.

| | 귀신 (Gwishin) | 도깨비 (Dokkaebi) | 물귀신 (Mulgwisin) |
|---|---|---|---|
| 상징색 | `#A0121B` 핏빛 크림슨 | `#E8751A` 도깨비불 주황 | `#0B7B8A` 물빛 청록 |
| 한 줄 소개 | 잡을수록 강해지는 추격자 | 잡히면 되받아치는 반격가 | 잡히는 순간 끌어내리는 방해꾼 |
| 패시브 | 잡기 성공 시 **원한 +3** — 공격이 곧 자원 | 잡히는 순간 확률로 **역잡기(씨름)** — 동료가 많을수록 성공률 상승 | 잡히는 순간 **공격자의 남은 윷 결과 전부 소멸** |
| 액티브 | **원혼 강림** (쿨 2턴) — 이동 경로 위의 적을 전부 즉시 잡기 | – | **제물** (쿨 2턴) — 자신의 말 1개를 희생해 **검은 윷 +2** |

**역잡기 확률** = `내 말 수 / (내 말 수 + 상대 말 수 + 1)`
업기로 뭉쳐 있을수록 방어가 단단해지도록 설계했습니다.

**귀신 액티브**는 도착 칸이 아닌 **경로 전체**를 판정하므로, 도깨비의 씨름 판정을 무시하고 지나가며 쓸어버립니다.
캐릭터 간 상성이 스킬 우선순위로 표현되는 지점입니다.

---

## AI 설계

AI는 별도 로직이 아니라 **가중치 스코어링 하나**로 캐릭터 성격을 표현합니다.

```
점수 = 잡기가능 × captureWeight
     + 전진량   × progressWeight
     + 완주가능 × finishWeight
     + 업기가능 × stackWeight
     + 액티브스킬 보너스
     + Random  × randomness
```

| 캐릭터 | capture | progress | finish | stack | randomness | 플레이 성향 |
|---|---|---|---|---|---|---|
| 귀신 | **2.0** | 1.0 | 1.0 | 0.5 | **0.1** | 집요하게 잡기만 노리는 정석 추격 |
| 도깨비 | 0.8 | 1.0 | 1.0 | 0.8 | **1.5** | 예측 불가능한 변칙 플레이 |
| 물귀신 | 0.5 | 0.5 | **1.5** | **2.0** | 0.2 | 뭉쳐서 안전하게 완주 우선 |

액티브 스킬 평가는 `CharacterSkill.EvaluateActiveMoveBonus()`로 각 스킬이 직접 점수를 매기게 위임했습니다.
새 캐릭터를 추가할 때 AI 코드는 건드리지 않습니다.

---

## 기술 스택

| 항목 | 내용 |
|---|---|
| 엔진 | Unity **6000.3.15f1** (URP 17.3) |
| 언어 | C# |
| 주요 패키지 | Input System, Cinemachine 3, Timeline, TextMesh Pro, DOTween |
| 데이터 | ScriptableObject (`CharacterData`, `CharacterSkill`, `BoardData`, `BoardNodeData` × 29) |
| 로컬라이제이션 | CSV 테이블 + `LocalizedText` 자동 갱신, PlayerPrefs 저장 |
| 플랫폼 | Windows (Standalone) |

---

## 프로젝트 구조

```
Assets/
├── Scenes/            TitleScene · TutorialScene · GameScene
├── Data/
│   ├── Characters/    캐릭터 3종 + 스킬 3종 (ScriptableObject)
│   ├── Board/         윷판 데이터
│   └── BoardNodes/    29개 노드 (참먹이 · 날윷 · 찌모 등 전통 명칭 그대로)
├── Scripts/
│   ├── GameSystem/
│   │   ├── Controller/    GameMaster(턴 진행) · TitleManager · TutorialManager
│   │   │                  LotDrawController(선공 제비뽑기) · OpponentCharacterController
│   │   ├── Yut/           ThrowYut(판정) · YutThrowController(던지기 연출)
│   │   ├── UI/            원한 게이지 · 스킬 툴팁 · 일시정지 · 게임 로그 · 결과창
│   │   ├── Localization/  LocalizationManager · LocalizedText
│   │   ├── Helper/         PieceMoveCalculator(이동 후보 계산) · InputBlocker
│   │   ├── GameEvents.cs  정적 이벤트 버스
│   │   └── VFXManager.cs  스킬 연출 · 배너 · 비네트
│   └── Object/
│       ├── Characters/    CharacterSkill(추상) → Gwishin / Dokkaebi / Mulgwishin
│       ├── Board/         BoardNode
│       ├── Piece/         Piece(로직) · PieceObject(뷰) · StackCountBadge
│       ├── Player.cs      말 · 윷 결과 · 원한 · 검은 윷 상태
│       └── AIController.cs
└── Editor/            BoardCircleLayout(윷판 배치 툴) · OtherworldlyAmbience(분위기 세팅 툴)
```

---

## 설계 포인트

**1. 스킬 = 훅 기반 확장**
`CharacterSkill`이 `OnCapture` / `OnCaptureAttempt` / `OnBeingCaptured` / `OnFinish` / `OnActiveMoveEffect` 등의 가상 메서드를 제공하고, `GameMaster`는 규칙 진행 중 정해진 지점에서 훅만 호출합니다. 캐릭터 추가는 ScriptableObject 하나를 만드는 일이 되고, 턴 진행 코드는 수정하지 않습니다.

**2. 로직과 뷰의 분리**
`Piece`(순수 C# 로직) ↔ `PieceObject`(3D 표현), `Player`(상태) ↔ UI를 분리했습니다. 상태 변화는 `GameEvents`로 방송하고, VFX·애니메이션·UI·상대 캐릭터 리액션은 각자 구독해서 반응합니다. 연출을 추가·제거해도 게임 규칙 코드는 그대로입니다.

**3. AI와 사람이 같은 경로를 쓴다**
이동 후보 계산은 `PieceMoveCalculator`, 실제 적용은 `GameMaster.ApplyAIMove()`로 통일했습니다. AI만 통과하는 별도 규칙 경로가 없어 규칙 불일치 버그가 생기지 않습니다.

**4. 튜토리얼을 위한 결정론 주입**
`ThrowYut.ForcedResults` 큐로 윷 결과를 강제 주입하고 `InputBlocker`로 입력을 단계별 제한해, 튜토리얼 전용 게임 로직 분기 없이 9스텝 시나리오를 구성했습니다.

**5. 접근성 · 편의**
2배속, 윷 자동 던지기, 이동 가능 말 하이라이트, 윷 결과 가이드 팝업, 스킬 툴팁을 제공합니다. 옵션은 PlayerPrefs에 영구 저장됩니다.

---

## 실행 방법

**빌드 실행**

```bash
./Build/Gwicheoksa.exe
```

**에디터에서 열기**

```bash
git clone --recurse-submodules https://github.com/Devel-Rocket-ClassRoom/minigame-project-jinag8904.git
```

Unity Hub에서 **6000.3.15f1** 버전으로 프로젝트를 열고 `Assets/Scenes/TitleScene.unity`를 실행합니다.

> 3D 모델 · 텍스처 등 외부 에셋은 `Assets/Imported` 서브모듈로 분리되어 있습니다.
> 이미 클론한 경우 `git submodule update --init --recursive`를 실행해 주세요.
