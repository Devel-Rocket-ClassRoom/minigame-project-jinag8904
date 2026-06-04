---
name: project-candle-objects-todo
description: M3 소품·조명 강화 — 양초 배치 완료, 등롱 발광 작업 진행 중
metadata: 
  node_type: memory
  type: project
  originSessionId: 4b80cfec-ff2e-4597-9821-68ba5fe19f3d
---

M3(소품+조명 분위기 강화) 작업. 모든 환경 라이트/소품은 `Assets/Prefabs/EnvironmentVisual.prefab` 안에 있고, 이 프리팹을 GameScene·TutorialScene·TitleScene 세 씬이 공유하므로 프리팹 수정 한 번이면 전부 반영됨. (씬에서 직접 수정하면 오버라이드로 동기화 깨짐 — 반드시 프리팹 모드/스테이지에서 수정)

**완료:**
- 임포트 에셋: `Assets/Imported/Vefects/Candle VFX URP`(양초), `Assets/Imported/KTinteractiveProp`(등롱 등 한국전통), `Assets/Imported/Korean_Traditional_Prop`(석탑/항아리/악기 등)
- 양초 배치: `Vefects Candle 0x` 메시(불꽃·연기 파티클 내장)를 코너 라이트 위치에 배치, scale ≈ 50 (WorldRoot 0.18배 보정). 프리팹 내 `CandleLight_Corner_*` 6개가 광원.
- `CandleFlicker.cs`(`Assets/Scripts/GameSystem/`): Perlin noise로 Light intensity/range 흔듦, Awake에서 현재값 기준 캡처. 코너 라이트에 부착.
- `CandleLight_Center` 삭제됨(사용자 판단, 차이 미미). 단 `Assets/Editor/OtherworldlyAmbience.cs`의 `Apply Otherworldly Lighting` 메뉴 실행 시 `EnsureCenterCandleLight()`가 씬레벨에 부활시킴 — 실행 금지. M3 마무리 때 이 스크립트 Center생성 제거+좌표 갱신 필요(구 좌표 상수 남음).
- **등롱 발광 (이번 세션):** 등롱 메시 `SM_Lantern_1/2`는 머티리얼 slot0=`M_R_Lantern`(나무), slot1=`M_Lantern_EM`(창호지/발광). Point Light만으론 등롱 몸체가 안 빛나서, `M_Lantern_EM` 복제 → `Assets/Materials/M_Lantern_EM_Glow.mat` 생성, `_EmissionColor`=(3, 2.07, 1.13) HDR 호박색. 프리팹 내 등롱 5개(Lantern 02 + Lantern 01 ×4) 메시 slot1에 교체 할당.
- 등롱 Point Light 5개: range 10→18(주의: 등롱이 scale 50배라 메시가 ~50유닛 높이인데 Light range는 스케일 영향 안 받음. range 5로 줄였더니 밑동만 비춰 어두웠음 → 18로 키움), colorTemperature 3565→2700K(촛불색), intensity 50 유지. LightProp(E키 토글) 컴포넌트 없음 — 플레이 없이 항상 켜짐.

**남은 작업:**
- 사용자 시각 확인 후 밝기 밸런스 미세조정 (양초 range/intensity, 등롱 intensity)
- M3-4 추가 소품(석탑/항아리 등) 코너·벽가 배치
- M3-5 공간 분위기: fog 활성화 + 떠다니는 먼지·혼불 파티클
- 양초 자체 라이트(Vefects `Candle Point Light`)는 실측스케일이라 비활성, 기존 CandleLight 사용 중

원본 임포트 머티리얼 직접수정 금지 → 복제본 사용 원칙 지킴. 관련: [[project-visual-enhancement-progress]], [[project-scene-downscale]], [[feedback-explain-before-code]]
