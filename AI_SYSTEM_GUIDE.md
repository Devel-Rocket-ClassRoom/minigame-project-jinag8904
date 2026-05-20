# AI 캐릭터 시스템 구현 가이드

## 전체 구조

```
CharacterData.cs   (신규) ← 캐릭터 데이터 정의
AIController.cs    (신규) ← AI 턴 처리
Player.cs          (수정) ← isAI, characterData 추가
GameMaster.cs      (수정) ← AI 분기 + 능력 훅 추가
```

---

## 신규 스크립트

### `CharacterData.cs`

**역할**: 캐릭터별 데이터(이름, 아이콘, AI 성격)를 담는 ScriptableObject.
에디터에서 GhostData.asset / GoblinData.asset / WaterGhostData.asset 3개 생성해서 사용.

```
[Serializable] AIPersonality
  필드
    float captureWeight   = 1f   // 잡기 선호도
    float progressWeight  = 1f   // 전진 선호도
    float finishWeight    = 1f   // 완주 선호도
    float stackWeight     = 1f   // 업기 선호도
    float randomness      = 0.3f // 무작위성 (높을수록 예측 불가)

CharacterData : ScriptableObject
  필드
    string        characterName
    Sprite        icon
    AIPersonality aiPersonality
    // 나중에 추가: CharacterAbility ability
```

---

### `AIController.cs`

**역할**: AI 플레이어의 턴을 처리. GameMaster와 같은 GameObject에 컴포넌트로 추가.
GameMaster가 AI 턴일 때 DecideTurn()을 호출하고 기다림.

```
AIController : MonoBehaviour

  필드
    GameMaster gameMaster   // Awake에서 GetComponent

  public 메서드
    IEnumerator DecideTurn(Player player)
      // AI 턴 전체 흐름 처리
      // 1. player.Throw() 호출
      // 2. yutResults가 남아있는 동안 반복:
      //      PickBestMove()로 말·목적지 결정
      //      gameMaster.ApplyAIMove()에 결과 전달
      // 3. 검은 윷 처리 (HasBlackYut이면 사용 여부 결정)

  private 메서드
    (Piece piece, BoardNodeData target, YutResult used) PickBestMove(Player player)
      // 이동 가능한 말 전체를 순회하며
      // PieceMoveCalculator.ComputeMoves()로 후보 구해서
      // ScoreMove()로 점수 매긴 뒤 최고점 반환

    float ScoreMove(Piece piece, BoardNodeData target, YutResult used, AIPersonality p)
      // 점수 계산 항목:
      //   잡기 가능 여부      * p.captureWeight
      //   이동 거리(전진량)   * p.progressWeight
      //   완주 가능 여부      * p.finishWeight
      //   업기 가능 여부      * p.stackWeight
      //   Random.value        * p.randomness

    bool ShouldUseBlackYut(Player player)
      // 나중에 personality 기반으로 판단
      // 지금은 일단 true 반환
```

---

## 수정할 스크립트

### `Player.cs`

추가할 필드 2개:

```
bool          isAI
CharacterData characterData
// 나중에 추가: CharacterAbility ability
```

---

### `GameMaster.cs`

**추가할 필드:**

```
AIController aiController   // Awake에서 GetComponent
```

**`Awake()` 수정:**

```
aiController = GetComponent<AIController>();
```

**`CoHandlePlayerTurn()` 수정 — AI 분기 추가 (맨 앞에):**

```
if (player.isAI)
{
    yield return StartCoroutine(aiController.DecideTurn(player));
    yield break;
}
// 이하 기존 사람 입력 로직 그대로
```

**`CoHandlePlayerTurn()` 수정 — 능력 훅 추가 (잡기 처리 직후 line 266 근처):**

```
// 기존 잡기 처리 끝난 뒤
player.ability?.OnCapture(piece, capturedPieces);
foreach (var leader in capturedPieces.Where(p => p.stackLeader == null))
    leader.owner.ability?.OnBeingCaptured(leader);
```

**`HandleFinish()` 수정 — 능력 훅 추가:**

```
// 기존 완주 처리 끝난 뒤
player.ability?.OnFinish(piece);
```

**`ApplyAIMove()` 신규 추가 — AIController가 결정한 이동을 실제 적용:**

```
public IEnumerator ApplyAIMove(Player player, Piece piece, BoardNodeData target, YutResult used)
  // CoHandlePlayerTurn의 이동 적용 + 잡기 처리 + 업기 처리 로직을 그대로 옮겨옴
  // AI / 사람 모두 이 메서드를 재사용하도록 나중에 리팩터링 가능
```

---

## 나중에 추가될 것 (지금은 만들지 않음)

```
CharacterAbility.cs (추상 base)
  virtual void OnCapture(Piece piece, List<Piece> captured)    {}
  virtual void OnBeingCaptured(Piece piece)                    {}
  virtual void OnFinish(Piece piece)                           {}
  virtual bool CanActivate()                                   => false
  virtual IEnumerator Activate()                               { yield break; }

GhostAbility.cs      : CharacterAbility
GoblinAbility.cs     : CharacterAbility
WaterGhostAbility.cs : CharacterAbility
```

---

## 캐릭터 수치 예시 (임시)

| 캐릭터 | captureWeight | progressWeight | finishWeight | stackWeight | randomness |
|--------|--------------|----------------|--------------|-------------|------------|
| 귀신   | 2.0          | 1.0            | 1.0          | 0.5         | 0.1        |
| 도깨비  | 0.8          | 1.0            | 1.0          | 0.8         | 1.5        |
| 물귀신  | 0.5          | 0.5            | 1.5          | 2.0         | 0.2        |
