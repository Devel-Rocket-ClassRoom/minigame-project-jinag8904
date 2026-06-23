# Firebase 전적/승률 — 작업 인수인계 노트

> 2026-06-22 노트북에서 시작. 2026-06-23 로컬 단계 완료. **2026-06-23 Firebase 단계 전체 완료.**
> Claude에게 이 파일을 보여주며 "Firebase 전적 작업 이어서"라고 하면 맥락 복원됨.

## 목표
AI 대전 전적/승률 기능. 게임 종료 시 결과 1건 저장 → 통계 패널에서 전체·캐릭터별 승률 표시.
로그인(이메일/비밀번호)한 계정 기준으로 개인 기록을 남기고, 캐릭터별 **전역(전체 게임) 승률**도 함께 표시.

## 현재 상태: ✅ 핵심 기능 완성
개인 기록 저장/조회 + 로그인/로그아웃 + 전역 캐릭터 승률까지 동작 확인됨. 남은 건 선택 항목(아래 "추후/선택")뿐.

---

## 확정된 설계 (구현 완료)
- **이메일/비밀번호 인증 + Realtime Database(RTDB) + 판별 기록 + 캐릭터별 승률 + 별도 통계 패널 + UniTask**
- `IStatsRepository` 인터페이스로 개인 기록 저장/조회를 추상화.
  - 로컬: `LocalStatsRepository`(PlayerPrefs) — 개발/테스트용
  - Firebase: `FirebaseStatsRepository` — 로그인 시 `StatsService.Use()`로 주입
  - 게임 코드(GameMaster/패널)는 `StatsService.Repo` 인터페이스만 앎
- 비동기: UniTask (`com.cysharp.unitask`, manifest.json)

## 로그인
- **이메일/비밀번호** (`AuthManager`). 게임 플레이는 로그인 불필요, **전적 저장/조회에만** 로그인 필요.
- UX: 전적 버튼 → 로그인 안 됐으면 로그인 폼(`LoginPanelView`) → 성공 시 통계로 전환.
- 타이틀에 로그인 시 uid 표시 + 로그아웃 버튼(`TitleManager`).
- 저장소 교체는 `TitleManager`가 로그인 시 `StatsService.InitAsync()` 호출로 수행. 로그아웃 후엔 `GameMaster`가 `IsLogedIn`일 때만 개인 기록하므로 안전(잘못된 계정에 안 씀).

## 전역(전체 게임) 캐릭터 승률
- 캐릭터별 줄에 개인 승률 + **전역 승률** 표시.
- 전 유저 matches 스캔 ❌ → **전역 집계 카운터**를 RTDB에 유지 ✅
  - 노드: `stats/characters/{charKey}: { wins, total }` (charKey = localizationKey, 예 CHAR_GWISHIN)
  - 기록 시: `total` +1(항상), `wins` +1(이겼을 때만)
  - 조회 시: `stats/characters` 한 번 읽음. 전역 승률 = wins/total, losses = total - wins.
- 전역 집계는 개인 기록과 **분리**된 독립 모듈(`GlobalStats`). **로그인 유저의 게임만 집계**(공개 repo 보안 규칙상 인증 필요 — 아래 보안 섹션). `GlobalStats.RecordAsync`는 비로그인 시 early-return.
- 표시: 개인/전역 텍스트 오브젝트 분리. 전역은 승률만, 키 `STATS_GLOBAL`(`전체 {0:F0}%` / `Global {0:F0}%`).

---

## ⚠️ 원안 대비 설계 변경점 (중요)
구현하면서 인수인계 원안과 달라진 부분:

1. **전역 집계를 `IStatsRepository`에서 분리 → 독립 `GlobalStats` (static)**
   - 이유: 전역은 개인 기록(per-uid)과 성격이 다른 집계 데이터. 저장소 구현(로컬/Firebase)과 무관하게 별도로 다루는 게 깔끔.
   - `GlobalStats.RecordAsync(charKey, won)` / `GlobalStats.LoadAsync()`. Firebase 준비 + **로그인 시에만** 기록(보안 규칙상 인증 필요, 비로그인 early-return).
   - 원안의 `IStatsRepository.LoadGlobalStatsAsync()`는 **추가하지 않음.**

2. **`ServerValue.Increment` 미지원** → **`RunTransaction`** 으로 카운터 증가
   - 이 SDK(Firebase Unity 13.12.0)의 `Firebase.Database.dll`에 `Increment` 없음(확인됨). 트랜잭션으로 원자적 +1, 동시성 안전.

3. **익명 로그인 제거** — 계정 모델과 안 맞음. (단, `AuthManager.SignInAnonymousyAsync`는 아직 파일에 남아있음 — 호출처 없음, 정리 권장)

4. **인증 에러 메시지 현지화** — `AuthManager.ParseFirebaseError`가 한국어 문장 대신 **현지화 키**(`AUTH_ERR_*`) 반환 → UI가 `LocalizationManager.Get()`으로 번역.

5. **AuthManager 싱글톤 버그 수정** — 중복 인스턴스는 `Destroy(gameObject)`로 자기 자신을 파괴(기존 유지). `OnDestroy`는 `if (instance == this)`일 때만 정리. (씬 전환 시 인증 상태 꼬임 해결.)

6. **로딩 오버레이** — 통계 패널이 네트워크 읽기 동안 `loadingView`로 placeholder 가림.

7. **비동기 수명 가드** — `await` 뒤 파괴된 오브젝트 접근 방지: `TitleManager`는 `GetCancellationTokenOnDestroy()`, 패널들은 `if (this == null) return;`.

---

## 파일 목록

### 순수 C# / 데이터
- `Assets/Scripts/GameSystem/Stats/MatchRecord.cs` — 한 판 데이터 (`won`, `character`)
- `Assets/Scripts/GameSystem/Stats/MatchStats.cs` — 순수 집계 (CharacterStat + MatchStats.From)
- `Assets/Scripts/GameSystem/Stats/IStatsRepository.cs` — `RecordMatchAsync` / `LoadMatchesAsync` (UniTask)
- `Assets/Scripts/GameSystem/Stats/LocalStatsRepository.cs` — PlayerPrefs + JsonUtility
- `Assets/Scripts/GameSystem/Stats/FirebaseStatsRepository.cs` — `users/{uid}/matches` push/read
- `Assets/Scripts/GameSystem/Stats/GlobalStats.cs` — `stats/characters` 트랜잭션 증가/조회 (전역, 로그인 무관)
- `Assets/Scripts/GameSystem/Stats/StatsService.cs` — `Repo`(lazy 로컬) + `Use()` + `InitAsync()`

### Firebase (스터디 프로젝트에서 가져옴)
- `Assets/Scripts/GameSystem/Firebase/FirebaseInitializer.cs` — 초기화 + `Resources.Load<FirebaseConfig>("FirebaseConfig")`
- `Assets/Scripts/GameSystem/Firebase/FirebaseConfig.cs` — ScriptableObject (databaseUrl 등)
- `Assets/Scripts/GameSystem/Firebase/AuthManager.cs` — 이메일/비번 인증, `LoginStateChanged` 이벤트
- `Assets/google-services.json` — 프로젝트 gwicheoksa, 패키지 com.develrocket.gwicheoksa
- `Assets/Resources/FirebaseConfig.asset` — databaseUrl: https://gwicheoksa-default-rtdb.asia-southeast1.firebasedatabase.app

### UI / 통합
- `Assets/Scripts/GameSystem/UI/StatsPanelView.cs` — 개인+전역 표시, 로딩 오버레이
- `Assets/Scripts/GameSystem/UI/LoginPanelView.cs` — 로그인/회원가입, `OnLoginSuccess` 이벤트
- `Assets/Scripts/GameSystem/Controller/TitleManager.cs` — 전적 버튼 게이팅, uid/로그아웃, ESC 닫기
- `Assets/Scripts/GameSystem/GameMaster.cs` — CoEndGame 훅: 전역 무조건 + 개인 로그인 시
- 현지화(`StringTable.csv`): `STATS_TOTAL`, `STATS_ROW`, `STATS_GLOBAL`, `STATS_LOADING`,
  `LOGIN_INFO`, `LOGIN_NEED_INPUT`, `LOGIN_BTN`, `LOGIN_SIGNUP`, `LOGIN_EMAIL_PH`, `LOGIN_PW_PH`,
  `LOGOUT_BTN`, `AUTH_ERR_EMAIL_IN_USE`, `AUTH_ERR_WEAK_PW`, `AUTH_ERR_INVALID_EMAIL`, `AUTH_ERR_NETWORK`, `AUTH_ERR_GENERIC`

### Unity 배선
- TitleScene: `FirebaseInitializer`/`AuthManager` 오브젝트(DontDestroyOnLoad), `LoginPanel`, `StatsPanel`(rows의 localizationKey = CHAR_GWISHIN/CHAR_DOKKAEBI/CHAR_MULGWISHIN + text/globalText 연결, loadingView 연결), TitleManager에 uidText/logoutBtn/loginPanel 연결

---

## RTDB 구조
- 개인: `users/{uid}/matches/{pushKey}: { won, character }` (character = localizationKey)
- 전역: `stats/characters/{charKey}: { wins, total }` (로그인 유저 게임 집계)
- 콘솔: https://console.firebase.google.com/project/gwicheoksa/database/gwicheoksa-default-rtdb/data

## 보안 규칙 (테스트 모드 아님 — 공개 repo라 잠금)
```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "auth != null && auth.uid === $uid",
        ".write": "auth != null && auth.uid === $uid"
      }
    },
    "stats": {
      ".read": "auth != null",
      ".write": "auth != null"
    }
  }
}
```
- `users/{uid}`: 본인만 읽기/쓰기. `stats`: 로그인 유저만. 그 외 경로 기본 거부.
- 이 규칙 때문에 **비로그인 전역 집계는 불가** → `GlobalStats.RecordAsync`가 비로그인 시 early-return(에러 로그 방지).

---

## 추후/선택
- **`StatsBootstrap` (시작 시 자동 InitAsync)** — 현재 저장소 교체는 로그인 시점(`TitleManager`)에만 일어남. 로그인된 채 앱을 재시작하고 전적 패널을 안 연 채로 플레이하면 그 판이 **로컬에 저장**되는 엣지가 있음. 시작 시/로그인 상태 변화 시 `StatsService.InitAsync()`를 자동 호출하는 부트스트랩 컴포넌트로 보완 가능(설계만 됨, 미구현).
- **익명 로그인 메서드 제거** — `AuthManager.SignInAnonymousyAsync` 죽은 코드, 삭제 권장.
- **데이터 초기화** (우선순위 낮음) — 내 전적 기록 삭제.
  - `IStatsRepository.ResetAsync()` 추가. Local: `PlayerPrefs.DeleteKey("match_records")`. Firebase: `users/{uid}/matches` 노드 `RemoveValueAsync`.
  - **전역(`stats/characters`)은 건드리지 않음**(누적 통계, 개인 초기화로 안 깎음).
  - UI: 전적 패널에 "기록 초기화" 버튼 + 확인 다이얼로그 → 초기화 후 `Refresh()` 재호출.
- **오프라인 전적 패널 가드** — 오프라인에서 패널 열면 `LoadMatchesAsync`/`GlobalStats.LoadAsync`의 await가 멈춘 듯 보일 수 있음. 타임아웃/실패 메시지 처리. (게임 진행·기록은 fire-and-forget라 영향 없음.)

## 검증 (완료된 시나리오)
- 로컬: AI 1판 → 전적 → 전체/캐릭터별 표시, 여러 판 누적, 언어 전환 ✅
- Firebase: 로그인 → AI 1판 → `users/{uid}/matches` 저장 + 패널 반영 ✅
- 전역: 로그인 1판 → 개인+전역 동시 기록/표시. 비로그인은 개인·전역 모두 미기록 ✅
- 보안: 다른 계정의 `users/{uid}` 접근 거부, 비로그인 read/write 거부 ✅
- 로그인/로그아웃: 타이틀 uid 표시, 로그아웃 동작, 씬 전환 후 상태 유지 ✅

## 참고
- 패널 패턴 원본: `Assets/Scripts/GameSystem/UI/SettingsPanelView.cs`
- 정적 텍스트 현지화: `Assets/Scripts/GameSystem/Localization/LocalizedText.cs` (key 넣으면 자동)
- 작업 방식(CLAUDE.md): Claude는 변경점·의도만 설명, 사용자가 직접 작성.
