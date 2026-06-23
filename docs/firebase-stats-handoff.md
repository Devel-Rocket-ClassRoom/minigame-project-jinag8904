# Firebase 전적/승률 — 작업 인수인계 노트

> 2026-06-22 노트북에서 시작. 2026-06-23 갱신(로컬 단계 완료 + 로그인 요구 추가).
> Claude에게 이 파일을 보여주며 "Firebase 전적 작업 이어서"라고 하면 맥락 복원됨.

## 목표
AI 대전 전적/승률 기능. 게임 종료 시 결과 1건 저장 → 통계 패널에서 전체·캐릭터별 승률 표시.

## 확정된 설계
- **이메일/비밀번호 인증 + Realtime Database(RTDB) + 판별 기록 + 캐릭터별 승률 + 별도 통계 패널 + UniTask**
- 핵심 전략: `IStatsRepository` 인터페이스로 저장/조회를 추상화.
  - 로컬: `LocalStatsRepository`(PlayerPrefs)로 동작·테스트 — **완료**
  - Firebase: `FirebaseStatsRepository`만 새로 작성 → `StatsService.Use()`로 구현체만 교체
  - 게임 코드(GameMaster/패널)는 인터페이스만 알아서, 백엔드 교체 시 한 곳만 수정
- **비동기 타입**: UniTask로 확정. 게임 프로젝트에 설치 완료(`com.cysharp.unitask`, manifest.json).

## 로그인 요구 (2026-06-23 추가)
- **방식**: 이메일/비밀번호 (스터디 프로젝트 `AuthManager`에 이미 구현됨)
- **범위**: 게임 플레이는 로그인 불필요. **전적 저장/조회에만** 로그인 필요.
- 영향: 전적이 기기 기준 → **계정 기준**. 로그인 상태일 때만 기록. 패널은 로그인 안 됐으면 로그인 먼저.
- UX(확정): 전적 버튼 → 로그인 안 됐으면 "로그인하면 전적을 볼 수 있어요" + 로그인 폼 → 성공 시 통계 전환.
- 익명 폴백은 계정 모델과 안 맞으므로 제거. `LocalStatsRepository`는 개발/테스트용으로만 유지.

## 전역(전체 유저) 캐릭터 승률 (2026-06-23 추가)
- **목표**: 캐릭터별 줄에 개인 승률 옆에 전체 유저 평균 승률도 표시.
- **방식**: 전 유저 matches 스캔 ❌. **전역 집계 카운터**를 원자적 증가로 유지 ✅ (확장성/보안 규칙 때문).
  - RTDB 노드: `stats/characters/{charKey}: { wins, total }` (charKey = localizationKey, 예 CHAR_GWISHIN)
  - 기록 시(`RecordMatchAsync` 안): `ServerValue.Increment(1)`로 `total` +1(항상), `wins` +1(이겼을 때만). 트랜잭션 불필요·동시성 안전.
  - 조회 시: `stats/characters` 한 번만 읽음(캐릭터 3개, 가벼움). 전체 승률 = wins/total.
- **인터페이스**: `IStatsRepository`에 `LoadGlobalStatsAsync()` → `Dictionary<string, CharacterStat>`(CharacterStat 재사용, losses = total - wins).
  - `LocalStatsRepository`: 전역 개념 없음 → 자기 기록으로 채움(로컬에선 개인=전체, 동작 확인용).
  - `FirebaseStatsRepository`: 위 카운터 읽기/쓰기 구현.
- **표시**: `StatsPanelView`가 개인 + 전역 둘 다 로드. `STATS_ROW`에 전체 승률 자리 추가, 예) `{0}승 {1}패 (내 {2:F0}% · 전체 {3:F0}%)`.
- 주의: 로그인 유저면 누구나 카운터 증가 가능(이론상 조작) — 테스트 모드 범위에선 무방.

## 완료 — 로컬 단계 (③④⑤, 로컬 검증까지 끝)
- `Assets/Scripts/GameSystem/Stats/MatchRecord.cs` — 한 판 데이터 (`won`, `character`)
- `Assets/Scripts/GameSystem/Stats/MatchStats.cs` — 순수 C# 집계 (CharacterStat + MatchStats.From)
- `Assets/Scripts/GameSystem/Stats/IStatsRepository.cs` — `RecordMatchAsync` / `LoadMatchesAsync` (UniTask)
- `Assets/Scripts/GameSystem/Stats/LocalStatsRepository.cs` — PlayerPrefs + JsonUtility(MatchRecordList 래퍼)
- `Assets/Scripts/GameSystem/Stats/StatsService.cs` — `static IStatsRepository Repo`(lazy 로컬 기본) + `Use(repo)` 주입구
- `Assets/Scripts/GameSystem/GameMaster.cs` — CoEndGame에 쓰기 훅. **`characterData.localizationKey` 저장**(언어 무관). `if (isVsAI)` 일 때만.
- `Assets/Scripts/GameSystem/UI/StatsPanelView.cs` — Show()→LoadMatchesAsync→MatchStats.From→TMP. 전체 + 캐릭터별 rows.
- `Assets/Scripts/GameSystem/Controller/TitleManager.cs` — "전적" 버튼 + ESC 닫기.
- 현지화: `STATS_TOTAL`, `STATS_ROW` 키 추가(StringTable.csv). 캐릭터 이름 라벨은 `LocalizedText`(CHAR_*).
- Unity: `Assets/Prefabs/UI/StatsPanel.prefab` + TitleScene 배선. **rows의 localizationKey = CHAR_GWISHIN/CHAR_DOKKAEBI/CHAR_MULGWISHIN** (이 값 누락 시 캐릭터별 0으로 나옴 — 실제로 한 번 겪음).

## 남은 마일스톤 — Firebase + 로그인 단계
1. **Firebase 콘솔** — 프로젝트 생성 → RTDB(테스트 모드) → **이메일/비밀번호 인증 활성화**.
2. **Unity SDK** — `google-services.json` + `FirebaseAuth`/`FirebaseDatabase` 패키지 + `Resources/FirebaseConfig.asset`(databaseUrl 등).
3. **재사용 파일 복사** (스터디 프로젝트 → 게임 프로젝트):
   - `FirebaseInitializer.cs` — 그대로. `Resources.Load<FirebaseConfig>("FirebaseConfig")` 로 설정 읽음.
   - `FirebaseConfig.cs` — 그대로. (Resources 폴더에 .asset 인스턴스 필요)
   - `AuthManager.cs` — 거의 그대로(이미 깨끗함). 메서드명 실제로는 `SignInAnonymousyAsync`(오타)·`SignInUserWithEmailAsync`·`CreateUserWithEmailAsync`.
4. **로그인 UI** — 스터디 `LoginUI.cs` 가져와 타이틀에 연결.
5. **`StatsService.InitAsync`** — 로그인 성공 후:
   `WaitForInitializationAsync()` → `uid = AuthManager.Instance.UserId`,
   `root = FirebaseInitializer.Instance.Database.RootReference` → `StatsService.Use(new FirebaseStatsRepository(uid, root))`.
6. **전적 패널 게이팅** — `AuthManager.IsLogedIn` 확인 → 안 됐으면 로그인 안내/폼, 됐으면 통계.
7. **GameMaster 훅** — 로그인 상태일 때만 `RecordMatchAsync` 호출하도록 조건 추가.
8. **전역 캐릭터 승률** — `IStatsRepository.LoadGlobalStatsAsync()` 추가, `FirebaseStatsRepository`에서 `stats/characters` 카운터 읽기/쓰기(ServerValue.Increment), `StatsPanelView`가 개인+전역 표시, `STATS_ROW` 포맷 확장. (상세: 위 "전역 캐릭터 승률" 섹션)

## 검증 (테스트 방법)
- **로컬 (완료)**: Play → AI 대전 1판 → "전적" 버튼 → 전체 + 캐릭터별 승/패·승률 표시. 여러 판 누적 확인. 언어 전환 확인. ✅
- **Firebase 교체 후**: 같은 시나리오 + 로그인 후 + Firebase 콘솔 `users/{uid}/matches` 트리에 데이터 들어오는지 확인.

## 참고
- 패널 패턴 원본: `Assets/Scripts/GameSystem/UI/SettingsPanelView.cs`
- 정적 텍스트 현지화: `Assets/Scripts/GameSystem/Localization/LocalizedText.cs` (key 넣으면 자동)
- RTDB 구조:
  - 개인: `users/{uid}/matches/{pushKey}: { won, character }` (character = localizationKey, 예 CHAR_GWISHIN)
  - 전역: `stats/characters/{charKey}: { wins, total }` (전체 유저 집계 카운터)
- 작업 방식(CLAUDE.md): Claude는 변경점·의도만 설명, 사용자가 직접 작성.
