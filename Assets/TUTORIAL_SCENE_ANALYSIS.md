# Tutorial 씬 현재 상황 분석

## 📊 씬 파일 정보
- **파일 경로**: `Assets/Scenes/Scenes/Tutorial.unity`
- **파일 크기**: 2,390 라인
- **마지막 수정**: 최근 (git status에서 modified 상태)

---

## 🎯 현재 Hierarchy 구조 (분석 결과)

### 씬 구성 오브젝트:
```
Tutorial Scene
├── Directional Light (1740495923)
│   └── UniversalAdditionalLightData
│
├── Factory1Wall01 (1) (1579834339)
│   └── 3D 모델 (공장 벽)
│
├── [BuildingBlock] Camera Rig (Prefab Instance: 1234372208)
│   ├── Main Camera (1234372213)
│   ├── LeftHand Controller (1234372217)
│   ├── RightHand Controller (1234372216)
│   ├── VRHandInteraction (Script 추가됨)
│   ├── XRRayInteractor (왼손)
│   └── XRRayInteractor (오른손)
│   └── LobbyCanvas (952965440) - World Space Canvas
│       ├── PlayerCountText (1543457556) - TextMeshPro
│       ├── StartGameButton (177106099) - Button
│       │   ├── StartGameButtonHandler (Script)
│       │   └── Text (TMP) (2100914398)
│       └── Tutorial (1733328947) - Image
│
└── (Prefab: Factory 모델 - 1557075005)
```

---

## 📋 주요 컴포넌트 분석

### 1. **Directional Light**
- Position: (0, 3, 0)
- Rotation: (50, -30, 0)
- Type: Directional
- Color: Warm white (1, 0.957, 0.839)
- Intensity: 1

### 2. **Factory1Wall01**
- Position: (6.04, -1.48, 34.45)
- Scale: (0.977, 1, 1)
- Component: MeshFilter, MeshRenderer, BoxCollider
- 공장 벽 3D 모델

### 3. **[BuildingBlock] Camera Rig** (VR 플레이어)
- **Prefab 인스턴스**: XR Interaction Toolkit
- **추가된 스크립트**:
  - `VRHandInteraction.cs` (손 상호작용)
  - 왼손/오른손 XRRayInteractor

#### 3-1. Main Camera
- VR HMD 카메라
- Canvas RenderMode: World Space의 타겟 카메라

#### 3-2. VRHandInteraction (추가됨)
```csharp
VRHandInteraction:
  leftHandControllerObj: {fileID: 1234372217}
  rightHandControllerObj: {fileID: 1234372216}
  leftHandTransform: {fileID: 1234372215}
  rightHandTransform: {fileID: 1234372210}
  grabRange: 2
```

### 4. **LobbyCanvas** (World Space Canvas)
- **Render Mode**: World Space
- **Position**: (0, 0, 2.87) (Camera Rig 하위)
- **Scale**: (0.01, 0.01, 0.01)
- **Camera**: Main Camera (VR 카메라)

#### 4-1. PlayerCountText (TextMeshPro)
- **Text**: (현재 비어있음 - 기본값)
- **Position**: (0, -50, -35)
- **Anchor**: Center
- **Color**: White

#### 4-2. StartGameButton (Button)
- **Position**: (0, -57.2, 0)
- **Size**: (160, 30)
- **Anchor**: Center
- **스크립트**:
  - `StartGameButtonHandler.cs` (startButton: null)
  - Unity UI Button

##### StartGameButton > Text (TMP)
- **Text**: (현재 비어있음 - 기본값)
- **Parent**: StartGameButton

#### 4-3. Tutorial (Image)
- **Position**: (-1, -140, 54)
- **Anchor**: Center
- **Component**: UI Image

---

## ⚠️ 발견된 문제점

### 1. **매니저 오브젝트 없음** ❌
```
❌ GameManager 없음
❌ TurnManager 없음
❌ VideoManager 없음
❌ ScoreManager 없음
❌ QuizManager 없음
```

### 2. **UI 구조 문제** ⚠️
- Canvas가 **World Space**로 설정됨 (Screen Space - Overlay여야 함)
- Canvas가 Camera Rig 하위에 있음 (Scene Root에 있어야 함)
- PlayerCountText와 StartGameButton의 텍스트가 비어있음

### 3. **네트워크 설정 없음** ❌
- PhotonView 컴포넌트 없음
- 멀티플레이어 기능 미구현

### 4. **스크립트 연결 문제** ⚠️
- StartGameButtonHandler의 `startButton` 필드가 null
- PlayerCountText가 어떤 매니저와도 연결되지 않음

### 5. **환경 오브젝트 부족** ⚠️
- 공장 벽 1개만 있음
- 바닥, 천장 없음
- 프레스 기계 없음
- 작업 공간 없음

---

## 🎯 Tutorial 씬의 목적 (추정)

현재 구조를 보면 **튜토리얼/로비 씬**으로 보입니다:
- VR Camera Rig (플레이어)
- World Space Canvas (UI)
- PlayerCountText (플레이어 수 표시)
- StartGameButton (게임 시작 버튼)

**하지만 매니저 시스템이 없어서 작동하지 않습니다.**

---

## ✅ 해야 할 작업 (우선순위)

### 1단계: 기본 매니저 시스템 구축
- [ ] GameManager 추가
- [ ] PhotonView 추가
- [ ] UI Canvas를 Screen Space - Overlay로 변경

### 2단계: UI 재구성
- [ ] LobbyCanvas를 Scene Root로 이동
- [ ] Canvas 설정 변경 (World Space → Screen Space)
- [ ] PlayerCountText 텍스트 설정 ("Waiting... 0/3")
- [ ] StartGameButton > Text 설정 ("Start Game")

### 3단계: 스크립트 연결
- [ ] GameManager > PlayerCountText 연결
- [ ] StartGameButton > GameManager.StartGame() 연결
- [ ] VRPlayerController 추가 (Camera Rig에)

### 4단계: 환경 구성
- [ ] 바닥 추가
- [ ] 공장 모델 배치
- [ ] 조명 추가

---

## 🔧 빠른 수정 가이드

### A. Canvas를 Screen Space로 변경

#### 현재 구조 (잘못됨):
```
[BuildingBlock] Camera Rig
└── LobbyCanvas (World Space)
    ├── PlayerCountText
    ├── StartGameButton
    └── Tutorial
```

#### 수정 후 구조 (올바름):
```
Scene Root
├── [BuildingBlock] Camera Rig
├── Canvas_LobbyUI (Screen Space - Overlay)
│   ├── PlayerCountText
│   ├── StartGameButton
│   └── Tutorial
└── GameManager
```

### B. GameManager 추가
```
Hierarchy 우클릭 > Create Empty
이름: GameManager
Add Component > GameManager.cs
Add Component > PhotonView
```

### C. UI 텍스트 설정
```
PlayerCountText:
  Text: "Waiting... 0/3"

StartGameButton > Text (TMP):
  Text: "Start Game"
```

---

## 📊 Onegiog vs Tutorial 씬 비교

### Onegiog 씬 (메인 게임)
- 매니저 시스템 완비 (GameManager, TurnManager 등)
- 작업 공간 (프레스 기계, 작업 구역)
- 턴제 시스템
- 퀴즈 시스템

### Tutorial 씬 (현재 상태)
- VR Camera Rig만 있음
- UI 구조가 잘못됨 (World Space)
- 매니저 없음
- 네트워크 기능 없음

---

## 🎮 Tutorial 씬의 역할 (제안)

### 옵션 1: 단순 로비 씬
- Photon 연결
- 플레이어 대기
- 게임 시작 버튼
- Onegiog 씬으로 이동

### 옵션 2: 튜토리얼 씬
- VR 조작 연습
- UI 상호작용 연습
- 이동/회전 연습
- 손 잡기 연습
- Onegiog 씬으로 이동

### 옵션 3: 통합 (권장)
- 로비 + 간단한 튜토리얼
- Onegiog와 동일한 매니저 구조
- Scene 전환 기능

---

## 🚀 추천 작업 순서

### 1. UI 재구성 (30분)
1. LobbyCanvas를 Scene Root로 드래그
2. Canvas > Render Mode: Screen Space - Overlay
3. PlayerCountText 텍스트: "Waiting... 0/3"
4. StartGameButton > Text: "Start Game"

### 2. GameManager 추가 (10분)
1. Create Empty > GameManager
2. Add Component > GameManager.cs
3. Add Component > PhotonView
4. PlayerCountText 연결

### 3. VRPlayerController 추가 (10분)
1. Camera Rig 선택
2. Add Component > VRPlayerController.cs
3. Camera Transform 연결

### 4. 테스트 (10분)
1. Play 버튼
2. Photon 연결 확인
3. "Waiting... 1/3" 표시 확인

**총 소요 시간: 약 1시간**

---

## 📝 다음 단계

Tutorial 씬을 어떻게 구성하시겠습니까?

1. **단순 로비** (가장 빠름)
2. **튜토리얼 연습** (시간 필요)
3. **Onegiog 복사** (가장 확실함)

선택하시면 상세 가이드를 드리겠습니다!
