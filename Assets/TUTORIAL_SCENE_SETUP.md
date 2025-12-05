# Tutorial 씬 설정 가이드

## 🎯 구현 기능

1. **트리거 버튼 (첫 번째)**: 튜토리얼 이미지 Canvas 표시
2. **트리거 버튼 (두 번째)**: Canvas 숨기고 문 열림 애니메이션
3. **문 열림 후 4초**: Onegiog 씬으로 자동 전환

---

## 📂 생성된 파일

- [TutorialManager.cs](Assets/Scripts/Managers/TutorialManager.cs)

---

## 🛠️ Unity 씬 설정 방법

### 1️⃣ TutorialManager 오브젝트 생성

#### Step 1: 빈 오브젝트 생성
```
Hierarchy 우클릭 > Create Empty
이름: TutorialManager
Position: (0, 0, 0)
```

#### Step 2: TutorialManager 스크립트 추가
```
TutorialManager 선택
Inspector > Add Component
TutorialManager 스크립트 드래그 앤 드롭
```

---

### 2️⃣ Inspector 설정

```
TutorialManager (Script)
┌─────────────────────────────────────────┐
│ Tutorial UI                             │
│   Tutorial Canvas: [드래그]             │ ← LobbyCanvas (또는 튜토리얼 이미지 Canvas)
│                                         │
│ Door References                         │
│   Gates Door Left: [드래그]             │ ← Hierarchy > GatesDoorLeft
│   Gates Door Right: [드래그]            │ ← Hierarchy > GatesDoorRight
│                                         │
│ Door Animation Settings                 │
│   Door Open Angle: 90                   │ ← 문이 열리는 각도 (Y축 회전)
│   Door Open Duration: 2                 │ ← 문 열리는 시간 (초)
│   Scene Transition Delay: 4             │ ← 씬 전환 대기 시간 (초)
│                                         │
│ Scene Settings                          │
│   Next Scene Name: "Onegiog"            │ ← 다음 씬 이름
└─────────────────────────────────────────┘
```

---

### 3️⃣ Hierarchy 구조 확인

Tutorial 씬에 다음 오브젝트들이 있어야 합니다:

```
Tutorial Scene
├── [BuildingBlock] Camera Rig (VR 플레이어)
├── LobbyCanvas (튜토리얼 이미지 Canvas)
├── GatesDoorLeft (왼쪽 문)
├── GatesDoorRight (오른쪽 문)
├── TutorialManager (신규 생성)
└── Directional Light
```

---

### 4️⃣ 문 오브젝트 설정

#### GatesDoorLeft (왼쪽 문)
- 문이 **안쪽으로 -90도** 회전하여 열림
- Pivot Point가 문 경첩 위치에 있어야 함

#### GatesDoorRight (오른쪽 문)
- 문이 **안쪽으로 +90도** 회전하여 열림
- Pivot Point가 문 경첩 위치에 있어야 함

**중요:** 문 모델의 Pivot Point가 경첩 위치에 있지 않으면 이상하게 회전합니다!

---

### 5️⃣ Build Settings에 씬 추가

#### Step 1: Build Settings 열기
```
Unity 메뉴 > File > Build Settings
```

#### Step 2: 씬 추가
```
Build Settings 창:
  Scenes In Build:
    0. Tutorial         ✅ (현재 씬)
    1. Onegiog          ✅ (다음 씬)
```

**추가 방법:**
1. Project > Assets > Scenes > Scenes 폴더 열기
2. **Tutorial.unity** 드래그 → Build Settings 창
3. **Onegiog.unity** 드래그 → Build Settings 창
4. 또는: Build Settings > **Add Open Scenes** 버튼 클릭

---

## 🎮 게임 플로우

```
1. Tutorial 씬 시작
   └─ VR 플레이어 스폰
   └─ 튜토리얼 Canvas 숨김 상태

2. 트리거 버튼 (첫 번째) 누름
   └─ 튜토리얼 이미지 Canvas 표시

3. 트리거 버튼 (두 번째) 누름
   └─ 튜토리얼 이미지 Canvas 숨김
   └─ 문 열림 애니메이션 시작 (2초)
      ├─ GatesDoorLeft: Y축 -90도 회전
      └─ GatesDoorRight: Y축 +90도 회전

4. 문 완전히 열림
   └─ 4초 대기

5. Onegiog 씬으로 자동 전환
   └─ SceneManager.LoadScene("Onegiog")
```

---

## 🔧 커스터마이징 옵션

### 문 열림 방향 변경
```csharp
// TutorialManager.cs Line 195-196
Quaternion leftTargetRotation = leftStartRotation * Quaternion.Euler(0, -doorOpenAngle, 0);  // 왼쪽 문
Quaternion rightTargetRotation = rightStartRotation * Quaternion.Euler(0, doorOpenAngle, 0); // 오른쪽 문
```

**옵션:**
- `-doorOpenAngle`: 안쪽으로 회전
- `+doorOpenAngle`: 바깥쪽으로 회전

### 문 열림 속도 변경
```
Inspector > Door Open Duration: 2 (초)
```
- 작은 값: 빠르게 열림
- 큰 값: 천천히 열림

### 씬 전환 대기 시간 변경
```
Inspector > Scene Transition Delay: 4 (초)
```
- 작은 값: 빠르게 전환
- 큰 값: 천천히 전환

---

## 🐛 트러블슈팅

### 1. 트리거 버튼이 작동하지 않음
**원인:** VR 컨트롤러가 연결되지 않음

**해결:**
- Console 로그 확인: `[TutorialManager] Left/Right Controller: ..., Valid: True`
- VR 기기가 연결되어 있는지 확인
- XR Interaction Toolkit이 설치되어 있는지 확인

### 2. 튜토리얼 Canvas가 표시/숨김되지 않음
**원인:** Tutorial Canvas가 연결되지 않음

**해결:**
```
TutorialManager Inspector:
  Tutorial Canvas: [LobbyCanvas 드래그]
```

### 3. 문이 이상하게 회전함
**원인:** Pivot Point가 잘못된 위치에 있음

**해결:**
- 3D 모델의 Pivot Point를 문 경첩 위치로 수정
- 또는 문 모델을 Empty GameObject 하위에 배치하고 회전

### 4. 씬 전환이 안 됨
**원인:** Build Settings에 Onegiog 씬이 없음

**해결:**
```
File > Build Settings
Scenes In Build:
  0. Tutorial
  1. Onegiog  ← 추가!
```

**Console 에러:**
```
[TutorialManager] 'Onegiog' 씬을 찾을 수 없습니다! Build Settings에 추가하세요.
```

### 5. 문이 회전하지 않음
**원인:** GatesDoorLeft/Right가 연결되지 않음

**해결:**
```
TutorialManager Inspector:
  Gates Door Left: [GatesDoorLeft 드래그]
  Gates Door Right: [GatesDoorRight 드래그]
```

---

## 📝 코드 동작 원리

### 1. 트리거 버튼 감지
```csharp
void HandleTriggerInput()
{
    bool leftTrigger = false;
    bool rightTrigger = false;

    // 왼쪽/오른쪽 트리거 버튼 감지
    if (leftController.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger) && leftTrigger)
    {
        OnTriggerPressed();
    }
}
```

### 2. 상태 관리
```csharp
private bool isTutorialImageVisible = false; // 튜토리얼 이미지 표시 여부

void OnTriggerPressed()
{
    if (isTutorialImageVisible)
    {
        // 두 번째 트리거: 이미지 숨기고 문 열기
        HideTutorialImage();
        OpenDoors();
    }
    else
    {
        // 첫 번째 트리거: 이미지 표시
        ShowTutorialImage();
    }
}
```

### 3. 문 애니메이션 (Quaternion.Slerp)
```csharp
IEnumerator OpenDoorsAnimation()
{
    // 초기 회전값
    Quaternion leftStartRotation = gatesDoorLeft.rotation;

    // 목표 회전값 (Y축 -90도)
    Quaternion leftTargetRotation = leftStartRotation * Quaternion.Euler(0, -90, 0);

    // 부드러운 회전 보간
    while (elapsedTime < doorOpenDuration)
    {
        float t = Mathf.SmoothStep(0f, 1f, elapsedTime / doorOpenDuration);
        gatesDoorLeft.rotation = Quaternion.Slerp(leftStartRotation, leftTargetRotation, t);
        yield return null;
    }

    // 4초 대기 후 씬 전환
    yield return new WaitForSeconds(4f);
    SceneManager.LoadScene("Onegiog");
}
```

---

## 🎯 테스트 방법

### 1. Unity Editor 테스트
1. Tutorial 씬 열기
2. **Play 버튼** 클릭
3. VR 컨트롤러 트리거 버튼 누르기 (또는 키보드 시뮬레이션)

### 2. VR 빌드 테스트
1. **File > Build Settings**
2. **Build and Run**
3. VR 기기에서 실행
4. 트리거 버튼으로 테스트

### 3. 디버그 메서드 (Inspector에서 수동 실행)
```csharp
// TutorialManager 선택 후 Inspector 하단
public void DebugOpenDoors() // 문 열기 테스트
public void DebugLoadNextScene() // 씬 전환 테스트
```

---

## 📊 Inspector 설정 예시

### 기본 설정 (권장)
```
Door Open Angle: 90          (90도 회전)
Door Open Duration: 2        (2초 동안 열림)
Scene Transition Delay: 4    (4초 후 전환)
Next Scene Name: "Onegiog"
```

### 빠른 테스트용
```
Door Open Angle: 90
Door Open Duration: 0.5      (빠르게 열림)
Scene Transition Delay: 1    (빠르게 전환)
Next Scene Name: "Onegiog"
```

### 느린 연출용
```
Door Open Angle: 90
Door Open Duration: 4        (천천히 열림)
Scene Transition Delay: 6    (여유롭게 전환)
Next Scene Name: "Onegiog"
```

---

## ✅ 최종 체크리스트

### Hierarchy 확인
- [ ] TutorialManager 오브젝트 생성
- [ ] GatesDoorLeft 존재
- [ ] GatesDoorRight 존재
- [ ] LobbyCanvas (튜토리얼 이미지) 존재

### Inspector 연결
- [ ] Tutorial Canvas → LobbyCanvas
- [ ] Gates Door Left → GatesDoorLeft
- [ ] Gates Door Right → GatesDoorRight
- [ ] Next Scene Name: "Onegiog"

### Build Settings
- [ ] Tutorial.unity 추가
- [ ] Onegiog.unity 추가

### 테스트
- [ ] Play 모드 실행
- [ ] 트리거 버튼으로 Canvas 토글 확인
- [ ] 문 열림 애니메이션 확인
- [ ] 4초 후 Onegiog 씬 전환 확인

---

## 🚀 빠른 설정 (5분)

1. **TutorialManager 생성** (1분)
   - Create Empty > TutorialManager
   - TutorialManager.cs 추가

2. **Inspector 연결** (2분)
   - Tutorial Canvas: LobbyCanvas
   - Gates Door Left: GatesDoorLeft
   - Gates Door Right: GatesDoorRight

3. **Build Settings** (1분)
   - Tutorial.unity 추가
   - Onegiog.unity 추가

4. **테스트** (1분)
   - Play 버튼
   - 트리거 버튼으로 테스트

**총 소요 시간: 5분**

---

완료되었습니다! 🎉

이제 Tutorial 씬에서 트리거 버튼으로 튜토리얼 → 문 열림 → Onegiog 전환이 가능합니다!
