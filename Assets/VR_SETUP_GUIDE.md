# VR 산업안전 교육 시뮬레이션 - 설정 가이드

## 🥽 VR 게임 구성

이 게임은 **VR 전용**으로 제작되었습니다.
- **지원 플랫폼**: Meta Quest 2/3/Pro, PC VR (SteamVR)
- **조작**: VR 컨트롤러
- **시점**: VR 1인칭

---

## 📦 1단계: 필수 패키지 설치

### Unity XR Plugin Management 설치

1. **Edit > Project Settings** 열기
2. **XR Plug-in Management** 선택
3. **Install XR Plug-in Management** 클릭
4. 플랫폼별 설정:
   - **PC/Mac/Linux**: **OpenXR** 체크
   - **Android (Quest)**: **Oculus** 체크

### XR Interaction Toolkit 설치

1. **Window > Package Manager** 열기
2. 왼쪽 상단 **Packages: Unity Registry** 선택
3. **XR Interaction Toolkit** 검색
4. **Install** 클릭
5. Samples에서 다음 Import:
   - **Starter Assets**
   - **Hands Interaction Demo** (선택사항)

---

## 🎮 2단계: VR 플레이어 프리팹 생성

### XR Origin (XR Rig) 생성

1. **Hierarchy 우클릭 > XR > XR Origin (VR)**
2. 자동 생성된 구조:
```
XR Origin (XR Rig)
├─ Camera Offset
│  ├─ Main Camera
│  ├─ LeftHand Controller
│  └─ RightHand Controller
└─ Locomotion System
```

### Character Controller 추가

1. **XR Origin** 선택
2. **Add Component > Character Controller**
3. **Inspector**에서:
   - **Center**: `X: 0, Y: 1, Z: 0`
   - **Radius**: `0.3`
   - **Height**: `1.8`

### VRPlayerController 스크립트 추가

1. **XR Origin** 선택
2. **Add Component > VRPlayerController**
3. **Inspector**에서:
   - **Move Speed**: `3`
   - **Camera Transform**: `Main Camera` 드래그 앤 드롭

### PhotonView 추가 (멀티플레이어용)

1. **XR Origin** 선택
2. **Add Component > Photon View**
3. **Inspector**에서:
   - **Observed Components**: Transform 추가
   - **Synchronization**: Unreliable On Change

---

## 🖐️ 3단계: VR 손 상호작용 설정

### VRHandInteraction 스크립트 추가

1. **XR Origin** 선택
2. **Add Component > VRHandInteraction**
3. **Inspector**에서:
   - **Left Hand Controller**: `LeftHand Controller` 드래그
   - **Right Hand Controller**: `RightHand Controller` 드래그
   - **Left Hand Transform**: `LeftHand Controller` 드래그
   - **Right Hand Transform**: `RightHand Controller` 드래그
   - **Grab Range**: `2`
   - **Interactable Layer**: Interactable 선택

### XR Direct Interactor 추가 (옵션)

**더 나은 잡기 경험을 원한다면:**

1. **LeftHand Controller** 선택
2. **Add Component > XR Direct Interactor**
3. **RightHand Controller**에도 동일하게 추가

---

## 🎨 4단계: VR UI 설정 (World Space)

**중요: VR에서는 모든 UI가 World Space여야 합니다!**

### Lobby UI를 World Space로 변경

1. **LobbyCanvas** 선택
2. **Inspector > Canvas**:
   - **Render Mode**: `World Space`
   - **Position**: `X: 0, Y: 2, Z: 3` (플레이어 앞 3m)
   - **Rotation**: `X: 0, Y: 0, Z: 0`
   - **Scale**: `X: 0.01, Y: 0.01, Z: 0.01`

### Video UI를 World Space로 변경

1. **VideoCanvas** 선택
2. **Inspector > Canvas**:
   - **Render Mode**: `World Space`
   - **Position**: `X: 0, Y: 2, Z: 2` (플레이어 가까이)
   - **Rotation**: `X: 0, Y: 0, Z: 0`
   - **Scale**: `X: 0.01, Y: 0.01, Z: 0.01`
   - **Width**: `1920`
   - **Height**: `1080`

### Equipment Selection UI를 World Space로 변경

1. **EquipmentSelectionCanvas** 선택
2. **Inspector > Canvas**:
   - **Render Mode**: `World Space`
   - **Position**: `X: 0, Y: 1.5, Z: 2`
   - **Scale**: `X: 0.01, Y: 0.01, Z: 0.01`

### Scoreboard UI를 World Space로 변경

1. **ScoreboardCanvas** 선택
2. **Inspector > Canvas**:
   - **Render Mode**: `World Space`
   - **Position**: `X: 0, Y: 2, Z: 3`
   - **Scale**: `X: 0.01, Y: 0.01, Z: 0.01`

### VR UI 인터랙션 활성화

**모든 Canvas에 추가:**

1. Canvas 선택
2. **Add Component > Canvas Group** (선택사항, 투명도 제어용)
3. **Add Component > Graphic Raycaster**
4. Graphic Raycaster 설정:
   - **Blocking Objects**: Three D
   - **Blocking Mask**: Everything

---

## 🎯 5단계: VR 컨트롤러 입력 설정

### XR Controller (Action-based) 확인

1. **LeftHand Controller** 선택
2. **XR Controller (Action-based)** 컴포넌트 확인
3. 주요 입력:
   - **Position Action**: `{LeftHand}/pointerPosition`
   - **Rotation Action**: `{LeftHand}/pointerRotation`
   - **Select Action**: `{LeftHand}/gripPressed` (잡기)

### 입력 액션 에셋 생성 (필요시)

1. **Assets 우클릭 > Create > XR > Input Actions**
2. 이름: `XRInputActions`
3. 더블클릭하여 열기
4. 기본 액션 확인:
   - Move (Left Stick)
   - Turn (Right Stick)
   - Grip (Grab)

---

## 🏭 6단계: 하이라키 최종 구조

```
Hierarchy
├─ XR Origin (XR Rig) [VR Player Prefab]
│  ├─ Camera Offset
│  │  ├─ Main Camera
│  │  ├─ LeftHand Controller
│  │  │  ├─ XR Controller (Action-based)
│  │  │  └─ XR Direct Interactor
│  │  └─ RightHand Controller
│  │     ├─ XR Controller (Action-based)
│  │     └─ XR Direct Interactor
│  ├─ Locomotion System
│  ├─ Character Controller
│  ├─ VRPlayerController
│  ├─ VRHandInteraction
│  └─ PhotonView
│
├─ === Managers ===
├─ GameManager
├─ VideoManager
├─ TurnManager
├─ ScoreManager
├─ SafetyEquipmentManager
├─ ProductSpawner
├─ ProductionSystem
├─ ScoreboardManager
│
├─ === UI (World Space) ===
├─ LobbyCanvas (World Space, Pos: 0,2,3)
├─ VideoCanvas (World Space, Pos: 0,2,2)
├─ EquipmentSelectionCanvas (World Space, Pos: 0,1.5,2)
├─ ScoreboardCanvas (World Space, Pos: 0,2,3)
│
└─ === Game Objects ===
   └─ PressMachine
```

---

## 🎮 7단계: VR 컨트롤러 조작법

### 이동
- **왼쪽 조이스틱**: 전후좌우 이동
- **오른쪽 조이스틱**: 좌우 회전 (스냅 턴 30도)

### 상호작용
- **그립 버튼 (왼손)**: 왼손으로 오브젝트 잡기
- **그립 버튼 (오른손)**: 오른손으로 오브젝트 잡기
- **그립 버튼 놓기**: 오브젝트 놓기/던지기

### UI 상호작용
- **VR 레이 포인터**: 컨트롤러에서 레이저 빔 나감
- **트리거 버튼**: UI 버튼 클릭

---

## ⚙️ 8단계: Photon PUN2 설치 (멀티플레이어)

### 설치 방법

1. **Window > Asset Store**
2. `Photon PUN 2` 검색
3. **PUN 2 - FREE** Import
4. Photon App ID 설정:
   - [photonengine.com](https://www.photonengine.com) 회원가입
   - App ID 발급
   - **Window > Photon Unity Networking > PUN Wizard**
   - App ID 입력

---

## 🔧 9단계: VR Player 프리팹 생성

### Resources 폴더에 저장

1. **Project > Assets** 우클릭 > **Create > Folder** > 이름: `Resources`
2. **Hierarchy > XR Origin** 선택
3. `Resources` 폴더로 드래그하여 프리팹 생성
4. 이름: `VRPlayer`

### Photon 네트워크로 플레이어 생성 설정

GameManager에서 플레이어를 네트워크로 생성하려면:

```csharp
// GameManager.cs에 추가
void OnJoinedRoom()
{
    // VR 플레이어 생성
    Vector3 spawnPos = new Vector3(0, 0, 0);
    PhotonNetwork.Instantiate("VRPlayer", spawnPos, Quaternion.identity);
}
```

---

## 🧪 10단계: VR 테스트

### Unity 에디터에서 테스트 (시뮬레이션)

1. **Window > XR > XR Device Simulator** 설치
2. **Play** 버튼 클릭
3. 키보드/마우스로 VR 시뮬레이션:
   - **WASD**: 이동
   - **마우스**: HMD 회전
   - **Tab**: 왼손/오른손 전환
   - **Space**: 그립

### 실제 VR 기기에서 테스트

#### Meta Quest (Android)

1. **File > Build Settings**
2. **Platform**: Android 선택
3. **Switch Platform** 클릭
4. Quest를 PC에 USB 연결
5. **Build and Run**

#### PC VR (SteamVR)

1. **File > Build Settings**
2. **Platform**: PC, Mac & Linux Standalone
3. **Build and Run**
4. SteamVR 실행

---

## 📋 체크리스트

설정 완료 확인:

- [ ] XR Plugin Management 설치
- [ ] XR Interaction Toolkit 설치
- [ ] XR Origin (XR Rig) 생성
- [ ] Character Controller 추가
- [ ] VRPlayerController 스크립트 추가
- [ ] VRHandInteraction 스크립트 추가
- [ ] PhotonView 추가
- [ ] 모든 UI Canvas를 World Space로 변경
- [ ] UI 위치 조정 (플레이어 앞 2~3m)
- [ ] VR Player 프리팹 생성 (Resources 폴더)
- [ ] Photon PUN2 설치 및 App ID 설정
- [ ] VR 기기 또는 시뮬레이터로 테스트

---

## 💡 팁

### VR UI 크기 조정
- UI가 너무 크면: Canvas Scale을 `0.005`로 줄이기
- UI가 너무 작으면: Canvas Scale을 `0.02`로 키우기

### VR 멀미 방지
- Snap Turn (스냅 회전) 사용 (현재 구현됨)
- 이동 속도를 `2~3`으로 유지
- Smooth Locomotion 대신 Teleport 사용 (선택사항)

### 퍼포먼스 최적화
- 그래픽 품질 낮추기: **Edit > Project Settings > Quality**
- VR Rendering: **Single Pass Instanced** 사용

---

## 🚀 다음 단계

1. ✅ VR 스크립트 작성 완료
2. ⏳ Unity XR 패키지 설치
3. ⏳ XR Origin 설정
4. ⏳ VR UI World Space 전환
5. ⏳ VR 기기 테스트

---

**VR 설정 중 문제가 있으면 언제든 물어보세요! 🥽**
