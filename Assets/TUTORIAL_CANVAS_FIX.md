# Tutorial 씬 LobbyCanvas 안 보이는 문제 해결

## 🔴 문제

Tutorial 씬을 시작하면 LobbyCanvas (PlayerCountText, StartGameButton)가 보이지 않습니다.

---

## 🔍 원인

LobbyCanvas의 **Render Mode가 World Space**로 설정되어 있습니다.

```
현재 설정 (문제):
  Canvas > Render Mode: World Space (2)

문제점:
  - World Space는 3D 공간에 배치되는 Canvas
  - Camera Rig 하위에 있어서 플레이어가 움직이면 따라다님
  - VR에서는 보이지만 위치가 이상함
```

---

## ✅ 해결 방법

### 방법 1: Unity Inspector에서 수정 (가장 쉬움)

#### Step 1: LobbyCanvas 선택
```
Hierarchy > LobbyCanvas 클릭
```

#### Step 2: Render Mode 변경
```
Inspector > Canvas (Component)
  Render Mode: World Space → Screen Space - Overlay
```

#### Step 3: LobbyCanvas를 Scene Root로 이동
```
현재 구조 (잘못됨):
[BuildingBlock] Camera Rig
└── LobbyCanvas  ← 여기에 있음

올바른 구조:
Tutorial Scene
├── [BuildingBlock] Camera Rig
├── LobbyCanvas  ← Scene Root로 이동
└── TutorialManager
```

**이동 방법:**
```
Hierarchy에서 LobbyCanvas를 드래그
→ Scene Root (Tutorial Scene 바로 아래)로 드롭
```

---

### 방법 2: Canvas 처음부터 다시 만들기

#### Step 1: 기존 LobbyCanvas 백업
```
LobbyCanvas 선택 > 우클릭 > Duplicate
이름: LobbyCanvas_Backup
Active 체크 해제 (비활성화)
```

#### Step 2: 새 Canvas 생성
```
Hierarchy 우클릭 > UI > Canvas
이름: LobbyCanvas_New
```

#### Step 3: Canvas 설정
```
Canvas (Component):
  Render Mode: Screen Space - Overlay  ✅
  Pixel Perfect: ☑️ (체크)

Canvas Scaler (Component):
  UI Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
  Match: 0.5
```

#### Step 4: UI 요소 복사
```
기존 LobbyCanvas에서:
  - PlayerCountText
  - StartGameButton
  - Tutorial (Image)

→ LobbyCanvas_New로 복사 (Ctrl+C, Ctrl+V)
```

#### Step 5: 기존 Canvas 삭제
```
LobbyCanvas 삭제
LobbyCanvas_New 이름을 LobbyCanvas로 변경
```

---

## 🛠️ 올바른 Canvas 설정

### LobbyCanvas Inspector 설정

```
┌─────────────────────────────────────────┐
│ Canvas                                  │
├─────────────────────────────────────────┤
│ Render Mode: Screen Space - Overlay     │ ✅
│ Pixel Perfect: ☑️                       │
│ Sort Order: 0                           │
│ Target Display: Display 1               │
│ Additional Shader Channels: None        │
├─────────────────────────────────────────┤
│ Canvas Scaler                           │
├─────────────────────────────────────────┤
│ UI Scale Mode: Scale With Screen Size   │
│ Reference Resolution: 1920 x 1080       │
│ Screen Match Mode: Match Width Or Height│
│ Match: 0.5                              │
│ Reference Pixels Per Unit: 100          │
├─────────────────────────────────────────┤
│ Graphic Raycaster                       │
├─────────────────────────────────────────┤
│ Ignore Reversed Graphics: ☑️            │
│ Blocking Objects: None                  │
│ Blocking Mask: Everything               │
└─────────────────────────────────────────┘
```

---

## 🎯 최종 Hierarchy 구조

```
Tutorial Scene
├── EventSystem (자동 생성)
├── [BuildingBlock] Camera Rig (VR 플레이어)
├── LobbyCanvas (Screen Space - Overlay) ✅
│   ├── PlayerCountText
│   ├── StartGameButton
│   │   └── Text (TMP)
│   └── Tutorial (Image)
├── GatesDoorLeft_Pivot
│   └── GatesDoorLeft
├── GatesDoorRight_Pivot
│   └── GatesDoorRight
├── TutorialManager
├── Directional Light
└── Factory 모델들
```

---

## 🔧 TutorialManager 연결 확인

Canvas를 Scene Root로 이동한 후:

```
TutorialManager Inspector:
  Tutorial Canvas: [LobbyCanvas 드래그] ✅

  (Canvas가 Camera Rig 하위에 있으면 잘못된 것!)
```

---

## 📝 UI 요소 위치 재설정

Canvas를 Screen Space로 변경한 후 UI 요소 위치를 조정해야 할 수 있습니다.

### PlayerCountText
```
Rect Transform:
  Anchor: Top Center
  Pos X: 0
  Pos Y: -50
  Width: 600
  Height: 100
```

### StartGameButton
```
Rect Transform:
  Anchor: Bottom Center
  Pos X: 0
  Pos Y: 150
  Width: 400
  Height: 100
```

### Tutorial (Image)
```
Rect Transform:
  Anchor: Center
  Pos X: 0
  Pos Y: 0
  Width: 800
  Height: 600
```

---

## 🐛 트러블슈팅

### Canvas가 여전히 안 보임
**확인 사항:**
- [ ] Render Mode: Screen Space - Overlay
- [ ] Canvas가 Scene Root에 있음 (Camera Rig 하위 아님)
- [ ] EventSystem이 존재함
- [ ] UI 요소들이 Active 상태임

### Canvas가 너무 작거나 큼
**해결:**
```
Canvas Scaler:
  UI Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
```

### VR에서 Canvas가 안 보임
**원인:** Screen Space - Overlay는 VR에서 작동하지 않을 수 있음

**해결:**
```
VR용 Canvas 설정:
  Render Mode: World Space
  Position: 플레이어 앞 (0, 1.5, 2)
  Scale: (0.001, 0.001, 0.001)
  Camera: Main Camera
```

**또는:**
```
Render Mode: Screen Space - Camera
Event Camera: Main Camera (VR HMD)
Plane Distance: 2
```

---

## 🎮 VR vs PC 모니터 차이

### PC 모니터 (권장):
```
Render Mode: Screen Space - Overlay
→ 화면에 항상 표시됨
```

### VR HMD:
```
옵션 1: World Space
  - 3D 공간에 배치
  - 플레이어 앞에 고정

옵션 2: Screen Space - Camera
  - VR HMD 카메라 기준
  - 시야에 항상 표시
```

---

## ✅ 빠른 수정 (3분)

### 1단계: Canvas 설정 변경 (1분)
```
Hierarchy > LobbyCanvas 선택
Inspector > Canvas > Render Mode: Screen Space - Overlay
```

### 2단계: Canvas 위치 변경 (1분)
```
LobbyCanvas를 드래그
→ Scene Root로 이동 (Camera Rig 밖으로)
```

### 3단계: 테스트 (1분)
```
Play 버튼 클릭
→ LobbyCanvas가 화면에 보이는지 확인
```

---

## 🎯 PC vs VR 빌드 설정

### PC 빌드 (모니터):
```
Canvas:
  Render Mode: Screen Space - Overlay ✅
```

### VR 빌드 (Quest, SteamVR):
```
Canvas:
  Render Mode: World Space
  또는
  Render Mode: Screen Space - Camera
  Event Camera: Main Camera
```

**TutorialManager.cs는 두 경우 모두 작동합니다!**

---

## 📊 Canvas Render Mode 비교

| Render Mode | 용도 | VR 지원 | 설정 난이도 |
|-------------|------|---------|-------------|
| Screen Space - Overlay | PC UI | ❌ | 쉬움 ⭐ |
| Screen Space - Camera | VR/PC 겸용 | ✅ | 보통 ⭐⭐ |
| World Space | VR 3D UI | ✅ | 어려움 ⭐⭐⭐ |

---

## 💡 권장 설정 (VR 게임)

Tutorial 씬이 VR 게임이라면:

```
Canvas:
  Render Mode: Screen Space - Camera
  Render Camera: Main Camera (VR HMD)
  Plane Distance: 2

Canvas Scaler:
  UI Scale Mode: Scale With Screen Size
  Reference Resolution: 1920 x 1080
```

이렇게 하면 VR에서도 UI가 플레이어 시야에 표시됩니다.

---

## 🚀 즉시 해결 (1분)

```
Hierarchy > LobbyCanvas 선택

Inspector > Canvas:
  Render Mode: World Space → Screen Space - Overlay

Hierarchy:
  LobbyCanvas를 Scene Root로 드래그

Play 버튼 → 확인!
```

---

완료! 이제 LobbyCanvas가 화면에 보일 겁니다! 🎉
