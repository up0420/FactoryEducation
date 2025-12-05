# Unity에서 Pivot Point 변경 방법

## 🎯 문제 상황

문 모델의 Pivot Point가 중앙에 있으면, 회전할 때 경첩이 아닌 중앙을 기준으로 회전합니다.

```
잘못된 Pivot (중앙):        올바른 Pivot (경첩):
┌─────────┐                ┌─────────┐
│    ●    │                ●         │
│         │                │         │
└─────────┘                └─────────┘
(중앙 회전 - 이상함)         (경첩 회전 - 정상)
```

---

## ✅ 해결 방법 (3가지)

### 방법 1: Empty GameObject로 감싸기 (가장 쉬움!) ⭐

Unity에서 직접 Pivot을 조정하는 방법입니다.

#### Step 1: Empty GameObject 생성
```
Hierarchy 우클릭 > Create Empty
이름: GatesDoorLeft_Pivot
Position: (경첩 위치 좌표)
```

#### Step 2: 문 모델을 Empty 하위로 이동
```
기존:
└── GatesDoorLeft (문 모델)

변경:
└── GatesDoorLeft_Pivot (Empty, 경첩 위치)
    └── GatesDoorLeft (문 모델)
```

#### Step 3: 위치 조정
1. **GatesDoorLeft_Pivot** 선택
2. Scene View에서 **경첩 위치로 이동**
3. **GatesDoorLeft (문 모델)** 선택
4. Position을 조정하여 원래 위치로 이동

#### Step 4: TutorialManager 연결
```
TutorialManager Inspector:
  Gates Door Left: [GatesDoorLeft_Pivot 드래그]  ← Empty 오브젝트
```

---

### 방법 2: 3D 모델링 툴에서 수정 (Blender 등)

3D 모델 자체의 Pivot을 수정합니다.

#### Blender에서 수정:
1. Blender에서 문 모델 열기 (.fbx, .obj 등)
2. **Edit Mode** (Tab 키)
3. **전체 선택** (A 키)
4. **3D Cursor를 경첩 위치로 이동**
   - Shift + S > Cursor to Selected
   - 또는 직접 3D Cursor 이동
5. **Object > Set Origin > Origin to 3D Cursor**
6. **Export** (FBX 또는 OBJ)
7. Unity에 다시 임포트

---

### 방법 3: Unity에서 수동 조정 (시간 소요)

#### Step 1: 문 모델 복제
```
GatesDoorLeft 선택 > Ctrl+D (복제)
이름: GatesDoorLeft_Original (백업)
```

#### Step 2: Pivot 위치 계산
```
문의 경첩 위치를 계산:
- 문 너비: 2m
- 경첩이 왼쪽에 있다면: X = -1m (문 중심에서 왼쪽으로)
```

#### Step 3: Empty GameObject 생성 및 배치
```
1. Create Empty > GatesDoorLeft_Pivot
2. Position을 경첩 위치로 설정
3. GatesDoorLeft를 하위로 드래그
4. GatesDoorLeft의 Local Position 조정
```

---

## 🛠️ 실전 예제: 왼쪽 문 설정

### 시나리오
- 문 크기: 2m (너비) x 3m (높이)
- 경첩 위치: 문의 왼쪽 가장자리
- 문 중심 좌표: (5, 0, 10)

### Step-by-Step

#### 1. 현재 문 위치 확인
```
GatesDoorLeft (문 모델)
Transform:
  Position: (5, 0, 10)   ← 문 중심
  Rotation: (0, 0, 0)
  Scale: (1, 1, 1)
```

#### 2. Empty GameObject 생성
```
Hierarchy 우클릭 > Create Empty
이름: GatesDoorLeft_Pivot

Transform:
  Position: (4, 0, 10)   ← 경첩 위치 (문 중심 - 1m)
  Rotation: (0, 0, 0)
  Scale: (1, 1, 1)
```

**계산:**
- 문 너비 = 2m
- 경첩은 왼쪽 가장자리
- 중심에서 왼쪽으로 1m → X = 5 - 1 = 4

#### 3. 문 모델을 Empty 하위로 이동
```
Drag: GatesDoorLeft → GatesDoorLeft_Pivot

결과:
GatesDoorLeft_Pivot (경첩 위치)
└── GatesDoorLeft (문 모델)
```

#### 4. 문 모델 위치 재조정
```
GatesDoorLeft (문 모델) 선택

Transform:
  Position: (1, 0, 0)   ← Local Position (부모 기준)
  Rotation: (0, 0, 0)
  Scale: (1, 1, 1)
```

**계산:**
- 부모(Pivot)가 (4, 0, 10)
- 문 중심이 (5, 0, 10)이 되려면
- Local Position = (1, 0, 0)

#### 5. 테스트
```
GatesDoorLeft_Pivot 선택
Inspector > Rotation > Y: 90

→ 문이 경첩을 중심으로 회전하는지 확인!
```

---

## 🎮 오른쪽 문도 동일하게 설정

### GatesDoorRight 설정

```
시나리오:
- 문 중심: (10, 0, 10)
- 경첩 위치: 오른쪽 가장자리 (11, 0, 10)

Step 1: Create Empty
이름: GatesDoorRight_Pivot
Position: (11, 0, 10)  ← 경첩 위치

Step 2: 문 모델을 하위로 이동
GatesDoorRight → GatesDoorRight_Pivot

Step 3: 문 모델 위치 조정
GatesDoorRight Local Position: (-1, 0, 0)
```

---

## 🔧 TutorialManager 연결

### 수정 전:
```
TutorialManager Inspector:
  Gates Door Left: [GatesDoorLeft]   ← 문 모델 직접 연결
  Gates Door Right: [GatesDoorRight]
```

### 수정 후:
```
TutorialManager Inspector:
  Gates Door Left: [GatesDoorLeft_Pivot]   ← Empty 오브젝트 연결
  Gates Door Right: [GatesDoorRight_Pivot]
```

---

## 🎯 Pivot 위치 찾는 방법

### 방법 1: Scene View에서 수동 측정
```
1. Scene View에서 문 모델 확인
2. 경첩이 있어야 할 위치 파악
3. Empty GameObject를 그 위치에 배치
4. Position 값 확인
```

### 방법 2: 문 크기 계산
```
1. GatesDoorLeft 선택
2. Inspector > Mesh Renderer > Bounds
3. Size 확인 (예: 2, 3, 0.2)
4. Center 확인
5. 경첩 위치 계산:
   - 왼쪽 경첩: Center.x - Size.x / 2
   - 오른쪽 경첩: Center.x + Size.x / 2
```

### 방법 3: Bounds 정보 출력
```csharp
// 디버그 스크립트
MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
Bounds bounds = meshRenderer.bounds;

Debug.Log($"문 중심: {bounds.center}");
Debug.Log($"문 크기: {bounds.size}");
Debug.Log($"왼쪽 경첩: {bounds.center.x - bounds.size.x / 2}");
Debug.Log($"오른쪽 경첩: {bounds.center.x + bounds.size.x / 2}");
```

---

## 📝 최종 Hierarchy 구조

### 수정 전:
```
Tutorial Scene
├── GatesDoorLeft (문 모델, Pivot이 중앙)
├── GatesDoorRight (문 모델, Pivot이 중앙)
└── TutorialManager
```

### 수정 후:
```
Tutorial Scene
├── GatesDoorLeft_Pivot (Empty, 경첩 위치)
│   └── GatesDoorLeft (문 모델)
├── GatesDoorRight_Pivot (Empty, 경첩 위치)
│   └── GatesDoorRight (문 모델)
└── TutorialManager
    └── Gates Door Left: GatesDoorLeft_Pivot
    └── Gates Door Right: GatesDoorRight_Pivot
```

---

## 🐛 트러블슈팅

### 문이 이상한 곳으로 회전함
**원인:** Empty GameObject의 Position이 잘못됨

**해결:**
1. Scene View에서 Empty GameObject를 경첩 위치로 정확히 이동
2. 문 모델의 Local Position 조정

### 문이 제자리에 없음
**원인:** 문 모델의 Local Position이 잘못됨

**해결:**
1. 부모(Pivot)의 World Position + 문의 Local Position = 원래 문 위치
2. Local Position 재계산

### 회전 방향이 반대
**원인:** 경첩이 반대편에 있음

**해결:**
1. Pivot Position을 반대편으로 변경
2. 또는 TutorialManager.cs의 회전 방향 변경:
   ```csharp
   // Line 195-196
   Quaternion leftTargetRotation = leftStartRotation * Quaternion.Euler(0, 90, 0);  // -90 → 90
   ```

---

## ✅ 빠른 설정 체크리스트

### 왼쪽 문 (GatesDoorLeft)
- [ ] Empty GameObject 생성: `GatesDoorLeft_Pivot`
- [ ] Empty를 경첩 위치로 이동 (왼쪽 가장자리)
- [ ] 문 모델을 Empty 하위로 이동
- [ ] 문 모델의 Local Position 조정
- [ ] TutorialManager > Gates Door Left = `GatesDoorLeft_Pivot`

### 오른쪽 문 (GatesDoorRight)
- [ ] Empty GameObject 생성: `GatesDoorRight_Pivot`
- [ ] Empty를 경첩 위치로 이동 (오른쪽 가장자리)
- [ ] 문 모델을 Empty 하위로 이동
- [ ] 문 모델의 Local Position 조정
- [ ] TutorialManager > Gates Door Right = `GatesDoorRight_Pivot`

### 테스트
- [ ] Play 모드 실행
- [ ] Pivot 선택 후 Rotation Y 값 변경
- [ ] 문이 경첩을 중심으로 회전하는지 확인

---

## 🎨 시각적 가이드

### Pivot 위치 확인 방법 (Gizmos)

Scene View에서 Empty GameObject를 선택하면 Pivot 위치를 확인할 수 있습니다:

```
Scene View:
  └── Gizmos 표시
      ├── X축: 빨강
      ├── Y축: 초록
      └── Z축: 파랑

Pivot 위치 = 3개 축이 만나는 점 (●)
```

---

## 💡 팁

### 1. Scene View에서 Pivot 시각화
```
Scene View 상단:
  └── Pivot / Center 토글 버튼 클릭
  └── "Pivot" 모드 선택
```

### 2. 정확한 위치 설정
```
Inspector > Transform > Position
  └── 직접 좌표 입력 (예: 4, 0, 10)
```

### 3. 문 모델 백업
```
수정 전 원본 문 모델을 복제해서 백업:
  └── GatesDoorLeft_Original (비활성화)
```

---

## 🚀 5분 빠른 설정

1. **왼쪽 문** (2분)
   - Create Empty > 이름: `GatesDoorLeft_Pivot`
   - 경첩 위치로 이동
   - 문 모델을 하위로 드래그
   - Local Position 조정

2. **오른쪽 문** (2분)
   - 동일한 방법 반복

3. **TutorialManager 연결** (1분)
   - Gates Door Left: `GatesDoorLeft_Pivot`
   - Gates Door Right: `GatesDoorRight_Pivot`

---

이제 문이 경첩을 중심으로 정확하게 회전합니다! 🎉
