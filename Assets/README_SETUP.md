# 산업안전 교육 시뮬레이션 게임 - 설정 가이드

## 프로젝트 개요
- **게임 타입**: 1인칭 3인 동시플레이 산업안전 교육 시뮬레이션
- **엔진**: Unity 3D
- **네트워킹**: Photon PUN2
- **Unity 버전**: 2022.3 이상 권장

---

## 1. Photon PUN2 설치

### 1.1 Asset Store에서 설치
1. Unity 에디터에서 **Window > Asset Store** 열기
2. "Photon PUN 2" 검색
3. **Photon PUN 2 - FREE** 다운로드 및 Import
4. Import 창에서 모든 파일 체크하고 **Import** 클릭

### 1.2 Photon 계정 생성 및 App ID 발급
1. [Photon Engine 사이트](https://www.photonengine.com/) 접속
2. 무료 계정 생성
3. Dashboard에서 **Create a New App** 클릭
4. **Photon Type**: Photon PUN
5. **Name**: FactoryEducation (또는 원하는 이름)
6. **Create** 클릭 후 **App ID** 복사

### 1.3 Unity에서 Photon 설정
1. Unity 에디터에서 **Window > Photon Unity Networking > PUN Wizard** 열기
2. **Setup Project** 클릭
3. 복사한 **App ID** 붙여넣기
4. **Setup Project** 클릭

---

## 2. 필수 패키지 설치

프로젝트에 다음 패키지가 설치되어 있는지 확인:

- **Input System** (1.14.2 이상)
- **TextMeshPro** (Unity 기본 포함)
- **Video Player** (Unity 기본 포함)
- **Universal Render Pipeline** (선택사항, 그래픽 향상)

### 설치 방법
1. **Window > Package Manager** 열기
2. 왼쪽 상단 **Packages: Unity Registry** 선택
3. 필요한 패키지 검색 후 **Install** 클릭

---

## 3. 폴더 구조

프로젝트의 폴더 구조는 다음과 같습니다:

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs
│   │   ├── TurnManager.cs
│   │   ├── ScoreManager.cs
│   │   └── VideoManager.cs
│   ├── Player/
│   │   ├── PlayerController.cs
│   │   └── PlayerInteraction.cs
│   ├── Equipment/
│   │   └── SafetyEquipmentManager.cs
│   ├── Press/
│   │   ├── PressMachine.cs
│   │   ├── ProductionSystem.cs
│   │   └── ProductSpawner.cs
│   └── UI/
│       └── ScoreboardManager.cs
├── Prefabs/
│   ├── Player/
│   ├── Equipment/
│   └── Press/
├── Videos/
│   └── SafetyEducation.mp4
├── Models/
└── UI/
```

---

## 4. 필수 설정

### 4.1 Tag 설정
**Edit > Project Settings > Tags and Layers**에서 다음 태그 추가:

- `ProductMaterial` - 제품 소재
- `Interactable` - 상호작용 가능한 오브젝트
- `PlayerHand` - 플레이어 손
- `PressZone` - 프레스 작업 구역

### 4.2 Layer 설정 (선택사항)
- `Interactable` - 상호작용 레이어 (Layer 8)

### 4.3 Input Actions 설정
1. **Assets/InputSystem_Actions.inputactions** 파일 열기 (또는 새로 생성)
2. 다음 액션 추가:
   - **Move** (WASD)
   - **Look** (마우스 이동)
   - **LeftHandGrab** (E키)
   - **RightHandGrab** (R키)

### 4.4 비디오 파일 배치
1. **Assets/StreamingAssets/Videos/** 폴더 생성
2. 안전교육 영상 파일 **SafetyEducation.mp4**를 해당 폴더에 배치

---

## 5. 씬 설정

### 5.1 GameManager 설정
1. Hierarchy에 빈 GameObject 생성: **GameManager**
2. **GameManager.cs** 스크립트 추가
3. Inspector에서 다음 설정:
   - **Player Count Text**: TextMeshProUGUI 컴포넌트 할당
   - **Lobby UI**: 로비 UI Canvas 할당
   - **Game UI**: 게임 UI Canvas 할당

### 5.2 VideoManager 설정
1. Hierarchy에 빈 GameObject 생성: **VideoManager**
2. **VideoManager.cs** 스크립트 추가
3. Canvas 생성 및 RawImage 추가
4. Inspector에서:
   - **Video Player**: VideoPlayer 컴포넌트 (자동 생성)
   - **Video Display**: RawImage 컴포넌트 할당
   - **Video File Name**: "SafetyEducation.mp4"

### 5.3 플레이어 프리팹 생성
1. Hierarchy에 빈 GameObject 생성: **Player**
2. 다음 컴포넌트 추가:
   - **CharacterController**
   - **PhotonView**
   - **PlayerController.cs**
   - **PlayerInteraction.cs**
3. 자식으로 **Main Camera** 추가
4. 손 오브젝트 생성:
   - **LeftHand** (Transform)
   - **RightHand** (Transform)
5. **Prefabs/Player/** 폴더로 드래그하여 프리팹 생성
6. PhotonView 설정:
   - **Observed Components**: Transform
   - **Synchronization**: Unreliable On Change

### 5.4 보호구 선택 UI 설정
1. Canvas 생성: **EquipmentSelectionUI**
2. 5개의 Button 생성 (안전화, 안전모, 안전장갑, 더미1, 더미2)
3. **SafetyEquipmentManager.cs** 스크립트 추가
4. Inspector에서 버튼 할당

### 5.5 프레스 기계 설정
1. Hierarchy에 프레스 모델 배치
2. **PressMachine.cs** 스크립트 추가
3. 프레스 상부/하부 Transform 할당
4. BoxCollider (Trigger) 추가하여 프레스 감지 영역 설정

### 5.6 점수판 UI 설정
1. Canvas 생성: **ScoreboardUI**
2. **ScoreboardManager.cs** 스크립트 추가
3. 점수 항목 프리팹 생성 및 할당

---

## 6. Photon 설정 확인

### 6.1 Photon Server Settings
1. **Window > Photon Unity Networking > Highlight Server Settings**
2. 다음 설정 확인:
   - **App Id PUN**: 발급받은 App ID
   - **Fixed Region**: "kr" (한국) 또는 "asia" (아시아)
   - **Protocol**: UDP

### 6.2 Room Settings
- **Max Players**: 3 (GameManager에서 설정됨)
- **Room Name**: "SafetyTrainingRoom" (GameManager에서 설정됨)

---

## 7. 빌드 설정

### 7.1 씬 등록
1. **File > Build Settings** 열기
2. 현재 씬을 **Scenes in Build**에 추가

### 7.2 플랫폼 설정
- **Target Platform**: PC, Mac & Linux Standalone
- **Architecture**: x86_64

### 7.3 Player Settings
1. **Edit > Project Settings > Player**
2. **Company Name** 및 **Product Name** 설정
3. **Scripting Backend**: IL2CPP (선택사항, 성능 향상)

---

## 8. 테스트

### 8.1 로컬 테스트
1. Unity 에디터에서 **Play** 버튼 클릭
2. Photon 서버 연결 확인
3. 로비 입장 확인

### 8.2 멀티플레이어 테스트
1. Unity 에디터에서 빌드 (**File > Build and Run**)
2. 빌드된 게임 실행
3. Unity 에디터에서도 Play 버튼 클릭
4. 2개의 클라이언트가 같은 방에 입장하는지 확인
5. 추가 클라이언트 실행하여 3명 접속 테스트

---

## 9. 주요 스크립트 설명

### 9.1 GameManager.cs
- 게임 전체 흐름 관리
- Photon 서버 연결 및 방 생성/참가
- Phase 전환 (Lobby → Video → Equipment → PressWork → Result)

### 9.2 VideoManager.cs
- 안전교육 영상 재생 및 제어
- 플레이어 이동 잠금/해제
- 영상 종료 시 다음 Phase로 전환

### 9.3 SafetyEquipmentManager.cs
- 보호구 선택 시스템
- 정답/오답 검증 및 점수 계산

### 9.4 TurnManager.cs
- 턴제 시스템 관리
- 플레이어별 작업 순서 제어

### 9.5 PressMachine.cs
- 프레스 기계 작동 시스템
- 카운트다운 및 반복 사이클
- 안전 감지

### 9.6 ScoreManager.cs
- 플레이어별 점수 관리
- 보호구 점수, 작업 점수, 안전 감점

### 9.7 ScoreboardManager.cs
- 결과 점수판 표시
- 순위 계산 및 UI 생성

---

## 10. 문제 해결

### 10.1 Photon 연결 실패
- App ID가 올바른지 확인
- 인터넷 연결 확인
- 방화벽 설정 확인

### 10.2 플레이어가 생성되지 않음
- Player 프리팹이 Resources 폴더에 있는지 확인
- PhotonView 컴포넌트가 추가되어 있는지 확인

### 10.3 영상이 재생되지 않음
- 비디오 파일 경로 확인 (StreamingAssets/Videos/)
- VideoPlayer 컴포넌트 설정 확인
- 비디오 코덱 확인 (H.264 권장)

### 10.4 점수가 동기화되지 않음
- PhotonView와 RPC 설정 확인
- 마스터 클라이언트 권한 확인

---

## 11. 다음 단계

1. **3D 모델 추가**: 보호구, 프레스 기계, 제품 소재 모델 Import
2. **애니메이션 추가**: 프레스 작동 애니메이션
3. **UI 디자인**: 더 나은 UI/UX 디자인
4. **사운드 추가**: 효과음 및 배경음악
5. **최적화**: 네트워크 동기화 최적화
6. **추가 기능**: 튜토리얼, 설정 메뉴 등

---

## 12. 참고 자료

- [Photon PUN 2 Documentation](https://doc.photonengine.com/pun/v2/getting-started/pun-intro)
- [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html)
- [Unity Video Player](https://docs.unity3d.com/Manual/class-VideoPlayer.html)

---

## 연락처

문제가 발생하거나 질문이 있으시면 팀 리더에게 연락하세요.

**프로젝트 버전**: 1.0.0
**최종 수정일**: 2025-01-31
