# Firebase 전적/승률 — 작업 인수인계 노트

> 2026-06-22 노트북에서 작성. 다른 컴퓨터(학원)에서 이어가기 위한 메모.
> Claude에게 이 파일을 보여주며 "Firebase 전적 작업 이어서"라고 하면 맥락 복원됨.

## 목표
AI 대전 전적/승률 기능. 게임 종료 시 결과 1건 저장 → 통계 패널에서 전체·캐릭터별 승률 표시.

## 확정된 설계 (사용자 선택)
- **익명 인증 + Realtime Database(RTDB) + 판별 기록 + 캐릭터별 승률 + 별도 통계 패널 + UniTask(팀 표준)**
- 핵심 전략: `IStatsRepository` 인터페이스로 저장/조회를 추상화.
  - 오늘: `LocalStatsRepository`(PlayerPrefs)로 동작·테스트
  - 내일: `FirebaseStatsRepository`만 새로 작성 → `StatsService`에서 구현체만 교체
  - 게임 코드(GameMaster/패널)는 인터페이스만 알아서, 백엔드 교체 시 한 곳만 수정

## ⚠️ 미해결 결정 — UniTask 미설치
이 게임 프로젝트엔 **UniTask가 설치 안 돼 있음** (manifest.json·Assets·Packages 어디에도 없음. 스터디 프로젝트에만 존재).
→ 비동기 타입을 어떻게 할지 선택 필요:
- **(A) .NET 기본 `Task`로 진행** → 설치 불필요, 즉시 컴파일. 나중에 UniTask로 swap은 find-replace 수준(`Task.CompletedTask`↔`UniTask.CompletedTask`, `Task.FromResult`↔`UniTask.FromResult`). Firebase SDK가 원래 `Task` 반환이라 `.AsUniTask()`도 불필요.
- **(B) UniTask 지금 설치** (Package Manager git URL) → 팀 표준과 즉시 일치.
- 학원 컴퓨터/Firebase 환경에 UniTask가 깔려 있으면 (B)가 자연스러움. 먼저 학원 프로젝트의 UniTask 설치 여부 확인할 것.

## 완료
- 개념 이해 완료 (BaaS / 익명인증=uid 이름표 / RTDB=JSON 트리 / Push=고유키 누적 vs SetValue=덮어쓰기 / DatabaseReference=위치 손잡이 / DataSnapshot=얼린 사진)
- `Assets/Scripts/GameSystem/Stats/MatchRecord.cs` — 한 판 데이터 (필드 2개: `won`, `character`. vsAI·timestamp는 불필요해 제거)
- `Assets/Scripts/GameSystem/Stats/MatchStats.cs` — 순수 C# 집계 (CharacterStat + MatchStats.From). 승률 `(float)wins/Total`, 표시 `*100f:F0`

## 남은 마일스톤
3. `IStatsRepository`(`RecordMatchAsync`, `LoadMatchesAsync`) + `LocalStatsRepository` + `StatsService`
   - LocalStatsRepository: PlayerPrefs + JsonUtility. **JsonUtility는 top-level List 직렬화 불가** → `[Serializable] class MatchRecordList { public List<MatchRecord> items; }` 래퍼 필요.
   - StatsService: `static IStatsRepository Repo`. 오늘 `Init()`에서 `new LocalStatsRepository()`. 내일 `InitAsync()`로 바꿔 Firebase 주입.
   - ← UniTask vs Task 결정 후 시작
4. **GameMaster 쓰기 훅** — `Assets/Scripts/GameSystem/Controller/GameMaster.cs:911-912`
   - 현재: `var winner = ...First(p => p.AllFinished); gameOverUI.Show(winner.playerId, isVsAI);`
   - 추가: `if (isVsAI) StatsService.Repo.RecordMatchAsync(new MatchRecord(winner.playerId == 0, players[0].characterData.name)).Forget();`
5. **StatsPanelView + 타이틀 버튼** — `SettingsPanelView` 패턴 복제(Show/Hide/IsOpen=activeSelf). `Show()`에서 LoadMatchesAsync → MatchStats.From → TMP 텍스트. TitleManager에 "전적" 버튼 + ESC 닫기. Unity에서 패널 UI 구성.

## 재사용 자산 (스터디 프로젝트 → 게임 프로젝트로 복사)
- `FirebaseInitializer.cs` — 초기화 싱글톤 (CheckAndFixDependenciesAsync, App/Database/Auth, `WaitForInitializationAsync`). 그대로 사용.
- `AuthManager.cs` — 익명 로그인 이미 구현 (`SignInAnonymouslyAsync` 튜플 반환, `UserId` 프로퍼티).
  - **복사 시 정리: `Update()`의 Space키 SignOut 제거, `SignOut()` 중복 `auth.SignOut()` 한 번만.**
- 내일 `StatsService.InitAsync` 흐름:
  `WaitForInitializationAsync()` → `AuthManager.SignInAnonymouslyAsync()` → `uid = AuthManager.Instance.UserId`,
  `root = FirebaseInitializer.Instance.Database.RootReference` → `new FirebaseStatsRepository(uid, root)`. 실패 시 Local 폴백.
- 내일 Firebase 콘솔: 프로젝트 생성 → RTDB(테스트 모드) → Anonymous 인증 활성화. Unity: `google-services.json` + `FirebaseAuth`/`FirebaseDatabase` 패키지 + `FirebaseConfig`(databaseUrl).

## 참고
- 패널 패턴 원본: `Assets/Scripts/GameSystem/UI/SettingsPanelView.cs`
- 타이틀 버튼 배선: `Assets/Scripts/GameSystem/Controller/TitleManager.cs:22`
- TMP 패널 예시: `Assets/Scripts/GameSystem/UI/GameOverPanelView.cs`
- RTDB 구조: `users/{uid}/matches/{pushKey}: { won, character }`
- 작업 방식(CLAUDE.md): Claude는 변경점·의도만 설명, 사용자가 직접 작성.
