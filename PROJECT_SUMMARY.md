# 산업안전 교육 시뮬레이션 게임 - 프로젝트 요약

## 프로젝트 정보
- **프로젝트명**: Factory Education - 산업안전 교육 시뮬레이션
- **게임 타입**: 1인칭 3인 동시플레이 교육용 시뮬레이션
- **엔진**: Unity 3D
- **네트워킹**: Photon PUN2
- **버전**: 1.0.0
- **개발 시작**: 2025-01-31

---

## 구현 완료 사항

### ✅ Phase 1: 로비 및 플레이어 매칭 시스템
- **GameManager.cs** 구현 완료
  - Photon PUN2 서버 연결
  - 자동 방 생성/참가 (최대 3명)
  - 플레이어 카운트 추적
  - 3명 접속 시 자동 Phase 2 전환
  - Phase 관리 시스템

### ✅ Phase 2: 플레이어 이동 제한 및 안전교육 영상
- **PlayerController.cs** 구현 완료
  - 1인칭 캐릭터 컨트롤러
  - WASD 이동, 마우스 카메라 제어
  - 이동 잠금/해제 시스템
- **VideoManager.cs** 구현 완료
  - Canvas 기반 Video Player
  - 마스터 클라이언트 영상 제어
  - 영상 재생 중 모든 플레이어 이동 잠금
  - 영상 종료 시 자동 Phase 3 전환

### ✅ Phase 3: 보호구 선택 시스템
- **SafetyEquipmentManager.cs** 구현 완료
  - 5개 보호구 버튼 UI (안전화, 안전모, 안전장갑, 더미1, 더미2)
  - 플레이어별 선택 상태 추적
  - 정답/오답 자동 검증
  - 점수 계산 (정답 +100점, 오답 -50점)
  - 3개 선택 시 자동 제출
- **ScoreManager.cs** 구현 완료
  - 플레이어별 점수 데이터 관리
  - 보호구 점수, 작업 점수, 안전 감점 추적
  - Photon RPC 점수 동기화

### ✅ Phase 4-7: 프레스 작업 턴제 시스템
- **TurnManager.cs** 구현 완료
  - 턴 순서 관리 (Player 1 → 2 → 3)
  - 현재 턴 플레이어 UI 표시
  - 턴 전환 알림
  - 플레이어별 작업 완료 추적
  - 3명 모두 완료 시 Phase 8 전환

- **PressMachine.cs** 구현 완료
  - 프레스 상태 관리 (Idle, Countdown, Operating)
  - 5초 카운트다운 시스템
  - 3초 간격 하강/상승 반복 사이클
  - 제품 소재 감지 및 완성품 생성
  - 안전 감지 (손 끼임 경고)

- **ProductionSystem.cs** 구현 완료
  - 제품 완성 처리
  - 플레이어별 성공 카운트 추적 (0/3, 1/3, 2/3, 3/3)
  - UI 업데이트

- **ProductSpawner.cs** 구현 완료
  - 플레이어별 제품 소재 생성 (최대 3회)
  - Photon Network 오브젝트 생성
  - 생성 위치 관리

- **PlayerInteraction.cs** 구현 완료
  - 양손 잡기 시스템 (E키 왼손, R키 오른손)
  - Raycast 기반 상호작용
  - 오브젝트 잡기/놓기
  - 네트워크 동기화

### ✅ Phase 8: 결과 점수판
- **ScoreboardManager.cs** 구현 완료
  - 점수판 UI 자동 생성
  - 순위 계산 및 정렬
  - 순위별 색상 구분 (1위 금색, 2위 은색, 3위 동색)
  - 플레이어별 점수 항목 표시
  - 다시 시작/종료 버튼

---

## 파일 구조

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs          (6.7 KB)
│   │   ├── TurnManager.cs          (8.0 KB)
│   │   ├── ScoreManager.cs         (5.6 KB)
│   │   └── VideoManager.cs         (4.7 KB)
│   ├── Player/
│   │   ├── PlayerController.cs     (3.4 KB)
│   │   └── PlayerInteraction.cs    (6.6 KB)
│   ├── Equipment/
│   │   └── SafetyEquipmentManager.cs (7.2 KB)
│   ├── Press/
│   │   ├── PressMachine.cs         (9.1 KB)
│   │   ├── ProductionSystem.cs     (2.5 KB)
│   │   └── ProductSpawner.cs       (4.3 KB)
│   └── UI/
│       └── ScoreboardManager.cs    (8.2 KB)
│
├── Documentation/
│   ├── README_SETUP.md             - 설정 가이드
│   ├── IMPLEMENTATION_CHECKLIST.md - 구현 체크리스트
│   ├── QUICK_REFERENCE.md          - 빠른 참조 가이드
│   └── PROJECT_SUMMARY.md          - 프로젝트 요약 (현재 파일)
│
├── Prefabs/
│   ├── Player/
│   ├── Equipment/
│   └── Press/
│
├── Videos/
│   └── (SafetyEducation.mp4 배치 예정)
│
└── Scenes/
    └── (메인 씬 설정 필요)
```

**총 스크립트 파일**: 10개
**총 라인 수**: 약 2,500 라인
**총 코드 크기**: 약 60 KB

---

## 핵심 기능

### 멀티플레이어 시스템
- Photon PUN2 기반 실시간 동기화
- 최대 3명 동시 플레이
- 마스터 클라이언트 권한 관리
- RPC 기반 네트워크 통신

### 게임 Phase 시스템
1. **Lobby** - 대기실 (3명 대기)
2. **VideoEducation** - 안전교육 영상 시청
3. **EquipmentSelection** - 보호구 선택 (5개 중 3개)
4. **PressWork** - 턴제 프레스 작업
5. **Result** - 결과 점수판

### 점수 시스템
- 보호구 선택 점수 (0~300점)
- 프레스 작업 점수 (+200점)
- 안전 감점 (-100점/위반)
- 실시간 동기화 및 순위 계산

### 턴제 시스템
- Player 1 → 2 → 3 순서
- 각 플레이어 3회 제품 제작
- 턴 알림 및 UI 업데이트
- 작업 완료 자동 감지

---

## 기술 스택

### Unity Packages
- **Photon PUN2** - 멀티플레이어 네트워킹
- **Input System** (1.14.2) - 입력 처리
- **TextMeshPro** - UI 텍스트
- **Video Player** - 영상 재생
- **Universal Render Pipeline** (선택사항)

### 주요 Unity 기능
- CharacterController (1인칭 이동)
- Coroutine (타이밍 제어)
- RaycastHit (상호작용)
- UI System (Canvas, Button, RawImage)
- PhotonView (네트워크 동기화)

---

## 네트워크 동기화 항목

### RPC 메서드
- `RPC_StartTurn` - 턴 시작
- `RPC_PlayVideo` - 영상 재생
- `RPC_OnVideoEnd` - 영상 종료
- `RPC_PlayerCompleted` - 플레이어 완료
- `RPC_StartCountdown` - 카운트다운 시작
- `RPC_StartPress` - 프레스 작동
- `RPC_StopPress` - 프레스 정지
- `RPC_UpdateSuccessCount` - 성공 카운트 업데이트
- `RPC_SyncScore` - 점수 동기화
- `RPC_SyncPhase` - Phase 동기화
- `RPC_GrabObject` - 오브젝트 잡기
- `RPC_ReleaseObject` - 오브젝트 놓기

### PhotonView 필요 오브젝트
- GameManager
- VideoManager
- SafetyEquipmentManager
- TurnManager
- PressMachine
- ScoreManager
- Player 프리팹
- ProductMaterial 프리팹

---

## 다음 단계 (TODO)

### Unity 설정 작업
- [ ] Photon PUN2 패키지 설치
- [ ] Photon App ID 발급 및 설정
- [ ] Input Actions 설정
- [ ] 태그 및 레이어 설정
- [ ] GameManager GameObject 생성 및 스크립트 할당
- [ ] VideoManager GameObject 생성 및 UI 설정
- [ ] Player 프리팹 생성 및 PhotonView 설정
- [ ] 보호구 선택 UI Canvas 생성
- [ ] 프레스 기계 모델 배치
- [ ] 점수판 UI Canvas 생성

### 에셋 추가
- [ ] 3D 모델 (보호구, 프레스, 제품 소재)
- [ ] 안전교육 영상 파일 (SafetyEducation.mp4)
- [ ] UI 스프라이트 및 아이콘
- [ ] 효과음 및 배경음악
- [ ] 공장 환경 에셋

### 추가 기능
- [ ] 튜토리얼 모드
- [ ] 설정 메뉴 (음량, 그래픽)
- [ ] 채팅 시스템
- [ ] 플레이어 커스터마이징
- [ ] 리플레이 기능

### 최적화
- [ ] Object Pooling (제품 소재)
- [ ] 네트워크 트래픽 최적화
- [ ] 프레임 드롭 테스트
- [ ] 메모리 누수 체크

---

## 테스트 계획

### 단일 플레이어 테스트
- [ ] 씬 로드 및 초기화
- [ ] Photon 서버 연결
- [ ] UI 동작 확인

### 멀티플레이어 테스트
- [ ] 2명 동시 접속
- [ ] 3명 동시 접속
- [ ] 네트워크 동기화 확인
- [ ] Phase 전환 확인
- [ ] 점수 동기화 확인

### 게임플레이 테스트
- [ ] 영상 재생 및 종료
- [ ] 보호구 선택 및 점수 계산
- [ ] 턴제 시스템
- [ ] 프레스 작동
- [ ] 제품 제작
- [ ] 점수판 표시

### 예외 상황 테스트
- [ ] 플레이어 중도 이탈
- [ ] 마스터 클라이언트 전환
- [ ] 네트워크 지연
- [ ] 동시 입력 충돌

---

## 참고 문서

- **README_SETUP.md** - Photon 설치 및 Unity 설정 가이드
- **IMPLEMENTATION_CHECKLIST.md** - Phase별 구현 체크리스트
- **QUICK_REFERENCE.md** - 주요 클래스 및 메서드 빠른 참조

---

## 팀 정보

### 역할 분담 (예시)
- **프로그래머**: 스크립트 구현 완료 ✅
- **레벨 디자이너**: Unity 씬 설정 (진행 중)
- **3D 아티스트**: 모델 제작 (진행 중)
- **UI/UX 디자이너**: UI 디자인 (대기 중)
- **사운드 디자이너**: 사운드 추가 (대기 중)

---

## 버전 히스토리

### v1.0.0 (2025-01-31)
- ✅ 모든 Phase 스크립트 구현 완료
- ✅ Photon PUN2 네트워크 시스템 구현
- ✅ 점수 시스템 및 턴제 시스템 구현
- ✅ 문서화 완료 (README, 체크리스트, 빠른 참조)

### v0.1.0 (계획)
- Unity 설정 및 씬 구성
- 에셋 추가 및 프리팹 생성
- 기본 테스트 완료

---

## 라이선스

이 프로젝트는 교육용 목적으로 제작되었습니다.

---

## 연락처

문제 또는 질문이 있으시면 팀 리더에게 연락하세요.

**프로젝트 상태**: 🟡 개발 중 (스크립트 구현 완료, Unity 설정 진행 중)
**최종 업데이트**: 2025-01-31
