# 산업안전 교육 시뮬레이션 - 구현 체크리스트

## Phase 1: 로비 및 플레이어 매칭 시스템 ✅

### 필수 구현 항목
- [x] GameManager.cs 작성
- [x] Photon PUN2 연결 시스템
- [x] 플레이어 카운터 (currentPlayers / maxPlayers)
- [x] 3명 접속 확인 시스템
- [ ] 플레이어 준비 상태 UI
- [ ] 대기 중 플레이어 수 표시 UI ("대기 중... 2/3")
- [ ] 네트워크 동기화 테스트

### Unity 설정
- [ ] GameManager GameObject 생성
- [ ] PhotonView 컴포넌트 추가
- [ ] UI 텍스트 연결 (playerCountText)
- [ ] 로비 UI Canvas 설정

---

## Phase 2: 플레이어 이동 제한 및 안전교육 영상 ✅

### 필수 구현 항목
- [x] PlayerController.cs 작성
- [x] 이동 제한 토글 기능 (isMovementLocked)
- [x] Input 차단 (WASD, 마우스룩)
- [x] VideoManager.cs 작성
- [x] Canvas 기반 Video Player
- [x] 영상 자동 재생 (3명 접속 후)
- [x] 영상 종료 이벤트 리스닝
- [x] OnVideoFinished() → Phase 3 전환

### Unity 설정
- [ ] Player 프리팹 생성
- [ ] CharacterController 추가
- [ ] PlayerController 스크립트 추가
- [ ] VideoManager GameObject 생성
- [ ] Canvas + RawImage 생성
- [ ] VideoPlayer 컴포넌트 추가
- [ ] 영상 파일 배치 (Assets/StreamingAssets/Videos/SafetyEducation.mp4)
- [ ] 마스터 클라이언트 영상 제어 테스트

---

## Phase 3: 보호구 선택 시스템 ✅

### 필수 구현 항목
- [x] SafetyEquipmentManager.cs 작성
- [x] 5개 보호구 버튼 UI
- [x] 플레이어별 선택 상태 추적
- [x] 정답 검증 로직 (안전화, 안전모, 안전장갑)
- [x] 점수 계산 (정답 +100점, 오답 -50점)
- [x] ScoreManager.cs 작성
- [x] 3개 선택 완료 시 자동 제출
- [x] 모든 플레이어 선택 완료 체크 → Phase 4 전환

### Unity 설정
- [ ] Equipment Selection UI Canvas 생성
- [ ] 5개 버튼 생성 (안전화, 안전모, 안전장갑, 더미1, 더미2)
- [ ] SafetyEquipmentManager GameObject 생성
- [ ] 버튼 배열 할당
- [ ] 선택 카운트 텍스트 연결
- [ ] ScoreManager GameObject 생성

---

## Phase 4: 프레스 작업 턴제 시스템 ✅

### 필수 구현 항목
- [x] TurnManager.cs 작성
- [x] 턴 순서 관리 (Player 1 → 2 → 3)
- [x] 현재 턴 플레이어 인덱스
- [x] 대기 구역(WaitZone) 및 작업 구역(WorkZone) 설정
- [ ] 구역 제한 Collider (비턴 플레이어 되돌림)
- [x] 턴 전환 UI 표시
- [x] 플레이어별 완료 상태 추적

### Unity 설정
- [ ] TurnManager GameObject 생성
- [ ] WaitZone Transform 설정
- [ ] WorkZone Transform 설정
- [ ] 구역 Trigger Collider 추가
- [ ] 현재 턴 UI 텍스트 연결

---

## Phase 5: 프레스 기계 작동 시스템 ✅

### 필수 구현 항목
- [x] PressMachine.cs 작성
- [x] 프레스 상태 (Idle, Countdown, Operating)
- [x] 버튼 입력 → 5초 카운트다운
- [x] 카운트다운 UI 표시
- [x] 3초 간격 하강/상승 반복
- [x] 하강 애니메이션 (Transform.position 이동)
- [x] 상승 애니메이션
- [x] 안전 감지 시스템 (손 감지 → 경고)

### Unity 설정
- [ ] 프레스 3D 모델 배치
- [ ] PressHead Transform 설정
- [ ] PressBase Transform 설정
- [ ] PressMachine 스크립트 추가
- [ ] 시작 버튼 UI 생성
- [ ] 카운트다운 텍스트 UI 연결
- [ ] 상태 텍스트 UI 연결
- [ ] BoxCollider (Trigger) 추가 (프레스 내부 감지)

---

## Phase 6: 제품 제작 로직 ✅

### 필수 구현 항목
- [x] ProductionSystem.cs 작성
- [x] ProductSpawner.cs 작성
- [x] PlayerInteraction.cs 작성
- [x] 양손 잡기 시스템 (E키 왼손, R키 오른손)
- [x] 프레스 내부 Trigger 감지
- [x] 제품 소재 → 완성품 전환
- [x] 플레이어별 성공 카운트 (0/3, 1/3, 2/3, 3/3)
- [x] 3회 성공 시 OnPlayerComplete() 호출

### Unity 설정
- [ ] ProductMaterial 프리팹 생성
  - [ ] Rigidbody 추가
  - [ ] Collider 추가
  - [ ] Tag: "ProductMaterial"
  - [ ] PhotonView 추가 (네트워크 동기화)
- [ ] LeftHand Transform (플레이어 자식)
- [ ] RightHand Transform (플레이어 자식)
- [ ] PlayerInteraction 스크립트 추가
- [ ] Input Actions 설정 (LeftHandGrab, RightHandGrab)
- [ ] ProductSpawner GameObject 생성
- [ ] 플레이어별 Spawn Position 설정
- [ ] ProductionSystem GameObject 생성
- [ ] 성공 카운트 UI 텍스트 연결

---

## Phase 7: 작업 완료 및 턴 전환 시스템 ✅

### 필수 구현 항목
- [x] OnPlayerComplete(int playerIndex) 구현
- [x] 완료 알림 UI ("작업 완료! 다음 플레이어 차례")
- [x] 다음 플레이어 이동 허용
- [x] 이전 플레이어 관전 모드 (선택사항)
- [x] 3명 모두 완료 시 Phase 8 진입
- [x] 작업 점수 추가 (+200점)

### Unity 설정
- [ ] 턴 알림 UI 패널 생성
- [ ] 알림 텍스트 연결
- [ ] 완료 후 자동 숨김 타이머 설정

---

## Phase 8: 결과 점수판 ✅

### 필수 구현 항목
- [x] ScoreboardManager.cs 작성
- [x] 플레이어별 점수 집계
  - [x] 보호구 선택 점수 (0~300점)
  - [x] 프레스 작업 점수 (완료 시 +200점)
  - [x] 안전 감점 (손 끼임 경고 시 -100점)
- [x] 순위 계산 및 정렬
- [x] UI 구성 (이름, 보호구 점수, 작업 점수, 총점, 순위)
- [x] 순위 하이라이트 (1위 금색, 2위 은색, 3위 동색)
- [x] 다시 시작 / 종료 버튼

### Unity 설정
- [ ] Scoreboard Canvas 생성
- [ ] ScoreEntry 프리팹 생성
  - [ ] 순위 텍스트
  - [ ] 플레이어 이름 텍스트
  - [ ] 보호구 점수 텍스트
  - [ ] 작업 점수 텍스트
  - [ ] 안전 감점 텍스트
  - [ ] 총점 텍스트
  - [ ] 배경 이미지
- [ ] ScoreboardManager GameObject 생성
- [ ] ScoreEntry 프리팹 할당
- [ ] 다시 시작 버튼 생성
- [ ] 종료 버튼 생성
- [ ] 모든 클라이언트 동기화 테스트

---

## 네트워크 동기화 체크리스트

### 필수 동기화 항목
- [x] 플레이어 카운트
- [x] 게임 페이즈 전환
- [x] 영상 재생 타이밍
- [x] 보호구 선택 상태
- [x] 턴 인덱스
- [x] 프레스 작동 상태
- [ ] 제품 생성/파괴
- [x] 점수 데이터

### PhotonView 설정
- [ ] GameManager: PhotonView 추가
- [ ] VideoManager: PhotonView 추가
- [ ] SafetyEquipmentManager: PhotonView 추가
- [ ] TurnManager: PhotonView 추가
- [ ] PressMachine: PhotonView 추가
- [ ] ScoreManager: PhotonView 추가
- [ ] Player 프리팹: PhotonView 추가
- [ ] ProductMaterial 프리팹: PhotonView 추가

---

## 안전성 검증

### 필수 테스트 항목
- [ ] 비정상 종료 시 게임 복구 로직
- [ ] 플레이어 중도 이탈 처리
- [ ] 동시 입력 충돌 방지
- [ ] 타이밍 동기화 (지연 보상)
- [ ] 마스터 클라이언트 전환 처리

---

## UI/UX 체크리스트

### 로비 (Phase 1)
- [ ] 플레이어 수 표시
- [ ] 준비 상태 표시
- [ ] 연결 상태 표시

### 영상 시청 (Phase 2)
- [ ] 전체 화면 비디오
- [ ] 스킵 불가 안내
- [ ] 로딩 인디케이터

### 보호구 선택 (Phase 3)
- [ ] 5개 버튼 레이아웃
- [ ] 선택 피드백 (하이라이트)
- [ ] 선택 개수 표시 (0/3, 1/3, 2/3, 3/3)
- [ ] 안내 문구

### 프레스 작업 (Phase 4-6)
- [ ] 현재 턴 표시
- [ ] 카운트다운 표시
- [ ] 프레스 상태 표시
- [ ] 성공 카운트 표시
- [ ] 안전 경고 표시

### 결과 (Phase 8)
- [ ] 점수판 레이아웃
- [ ] 순위 색상 구분
- [ ] 각 항목별 점수 표시
- [ ] 버튼 (다시 시작, 종료)

---

## 최종 테스트 시나리오

### 시나리오 1: 정상 플레이
1. [ ] 3명의 플레이어가 로비에 접속
2. [ ] 자동으로 영상 재생 시작
3. [ ] 모든 플레이어 이동 잠김 확인
4. [ ] 영상 종료 후 보호구 선택 화면 표시
5. [ ] 각 플레이어가 3개씩 선택
6. [ ] 점수 계산 확인
7. [ ] 턴제 작업 시작 (Player 1 → 2 → 3)
8. [ ] 각 플레이어가 3회 제품 제작
9. [ ] 점수판 표시 및 순위 확인

### 시나리오 2: 안전 위반
1. [ ] 프레스 작동 중 손 감지
2. [ ] 안전 경고 표시
3. [ ] 감점 (-100점) 확인

### 시나리오 3: 네트워크 오류
1. [ ] 플레이어 중도 이탈 시 처리
2. [ ] 마스터 클라이언트 전환
3. [ ] 게임 진행 가능 여부 확인

---

## 최적화 체크리스트

### 성능 최적화
- [ ] Object Pooling (제품 소재)
- [ ] 네트워크 트래픽 최소화
- [ ] RPC 호출 최적화
- [ ] 프레임 드롭 테스트

### 메모리 최적화
- [ ] Texture 압축
- [ ] 메시 최적화
- [ ] 메모리 누수 체크

---

## 문서화

- [x] README_SETUP.md 작성
- [x] IMPLEMENTATION_CHECKLIST.md 작성
- [ ] 코드 주석 추가
- [ ] API 문서 작성 (선택사항)

---

## 다음 단계

1. **3D 에셋 추가**
   - 보호구 모델 (안전화, 안전모, 안전장갑)
   - 프레스 기계 모델
   - 제품 소재 모델
   - 공장 환경 모델

2. **애니메이션**
   - 프레스 작동 애니메이션
   - 플레이어 상호작용 애니메이션

3. **사운드**
   - 배경음악
   - 효과음 (버튼 클릭, 프레스 작동, 경고음 등)

4. **추가 기능**
   - 튜토리얼 모드
   - 설정 메뉴 (음량, 그래픽 등)
   - 다국어 지원

---

**체크리스트 버전**: 1.0.0
**최종 업데이트**: 2025-01-31
