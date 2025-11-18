# 산업안전 교육 시뮬레이션 - 빠른 참조 가이드

## 핵심 스크립트 위치

### Managers
```
Assets/Scripts/Managers/
├── GameManager.cs          - 게임 전체 흐름 및 Phase 관리
├── TurnManager.cs          - 턴제 시스템 관리
├── ScoreManager.cs         - 점수 관리 및 동기화
└── VideoManager.cs         - 영상 재생 및 제어
```

### Player
```
Assets/Scripts/Player/
├── PlayerController.cs     - 1인칭 이동 및 카메라 제어
└── PlayerInteraction.cs    - 양손 상호작용 시스템
```

### Equipment
```
Assets/Scripts/Equipment/
└── SafetyEquipmentManager.cs - 보호구 선택 시스템
```

### Press
```
Assets/Scripts/Press/
├── PressMachine.cs         - 프레스 기계 작동
├── ProductionSystem.cs     - 제품 제작 시스템
└── ProductSpawner.cs       - 제품 소재 생성
```

### UI
```
Assets/Scripts/UI/
└── ScoreboardManager.cs    - 결과 점수판
```

---

## 게임 흐름 다이어그램

```
시작
  ↓
[Phase 1] 로비 (대기실)
  - 3명 플레이어 대기
  - 플레이어 카운트 표시
  - 3명 접속 완료 → Phase 2
  ↓
[Phase 2] 영상 시청
  - 모든 플레이어 이동 잠김
  - 안전교육 영상 자동 재생
  - 영상 종료 → Phase 3
  ↓
[Phase 3] 보호구 선택
  - 5개 중 3개 선택
  - 정답: 안전화, 안전모, 안전장갑
  - 점수 계산 (정답 +100, 오답 -50)
  - 3명 모두 선택 완료 → Phase 4
  ↓
[Phase 4-7] 프레스 작업 (턴제)
  - Player 1 턴 (3회 제품 제작)
  - Player 2 턴 (3회 제품 제작)
  - Player 3 턴 (3회 제품 제작)
  - 각 완료 시 +200점
  ↓
[Phase 8] 결과 점수판
  - 점수 집계 및 순위 계산
  - 1위, 2위, 3위 표시
  - 다시 시작 or 종료
```

---

## 주요 클래스 및 메서드

### GameManager
```csharp
// 싱글톤 인스턴스
GameManager.Instance

// Phase 전환 (마스터 클라이언트만)
GameManager.Instance.ChangePhase(GameManager.GamePhase.EquipmentSelection);

// 로컬 플레이어 인덱스 (0, 1, 2)
int playerIndex = GameManager.Instance.GetLocalPlayerIndex();

// GamePhase 열거형
public enum GamePhase
{
    Lobby,              // 대기실
    VideoEducation,     // 영상 시청
    EquipmentSelection, // 보호구 선택
    PressWork,          // 프레스 작업
    Result              // 결과
}
```

### VideoManager
```csharp
// 영상 재생 (마스터 클라이언트)
VideoManager videoManager = FindObjectOfType<VideoManager>();
videoManager.PlaySafetyVideo();

// 영상 일시정지/재개 (디버그용)
videoManager.PauseVideo();
videoManager.ResumeVideo();
```

### SafetyEquipmentManager
```csharp
// 보호구 선택 시작
SafetyEquipmentManager equipmentManager = FindObjectOfType<SafetyEquipmentManager>();
equipmentManager.StartSelection();

// UI 숨기기
equipmentManager.HideSelectionUI();
```

### ScoreManager
```csharp
// 싱글톤 인스턴스
ScoreManager.Instance

// 보호구 점수 추가
ScoreManager.Instance.AddEquipmentScore(playerIndex, 100);

// 작업 점수 추가
ScoreManager.Instance.AddWorkScore(playerIndex, 200);

// 안전 감점 추가
ScoreManager.Instance.AddSafetyPenalty(playerIndex, -100);

// 플레이어 점수 가져오기
ScoreManager.PlayerScore score = ScoreManager.Instance.GetPlayerScore(playerIndex);

// 정렬된 점수 리스트
List<ScoreManager.PlayerScore> sortedScores = ScoreManager.Instance.GetSortedScores();
```

### TurnManager
```csharp
// 싱글톤 인스턴스
TurnManager.Instance

// 턴제 시작 (마스터 클라이언트)
TurnManager.Instance.StartTurnSystem();

// 제품 성공 알림 (마스터 클라이언트)
TurnManager.Instance.OnProductSuccess(playerIndex);

// 현재 턴인지 확인
bool isMyTurn = TurnManager.Instance.IsMyTurn();

// 현재 턴 플레이어 인덱스
int currentTurn = TurnManager.Instance.currentPlayerTurn;
```

### PressMachine
```csharp
// 프레스 상태
public enum PressState
{
    Idle,       // 대기 중
    Countdown,  // 카운트다운 중
    Operating   // 작동 중
}

// 프레스 정지
PressMachine pressMachine = FindObjectOfType<PressMachine>();
pressMachine.StopPress();
```

### ProductionSystem
```csharp
// 제품 완성 알림 (마스터 클라이언트)
ProductionSystem productionSystem = FindObjectOfType<ProductionSystem>();
productionSystem.OnProductComplete(playerIndex);

// 성공 카운트 가져오기
int count = productionSystem.GetPlayerSuccessCount(playerIndex);
```

### PlayerController
```csharp
// 이동 잠금/해제
PlayerController player = GetComponent<PlayerController>();
player.SetMovementLock(true);  // 잠금
player.SetMovementLock(false); // 해제
```

### PlayerInteraction
```csharp
// 양손에 오브젝트를 들고 있는지 확인
PlayerInteraction interaction = GetComponent<PlayerInteraction>();
bool bothHands = interaction.IsBothHandsHolding();

// 특정 손에 오브젝트를 들고 있는지 확인
bool leftHand = interaction.IsHolding(true);  // 왼손
bool rightHand = interaction.IsHolding(false); // 오른손
```

---

## Photon 네트워크 메서드

### 마스터 클라이언트 확인
```csharp
if (PhotonNetwork.IsMasterClient)
{
    // 마스터 클라이언트만 실행
}
```

### 로컬 플레이어 확인
```csharp
if (photonView.IsMine)
{
    // 로컬 플레이어만 실행
}
```

### RPC 호출
```csharp
// 모든 클라이언트에게 전송
photonView.RPC("RPC_MethodName", RpcTarget.All);

// 다른 클라이언트에게만 전송 (자신 제외)
photonView.RPC("RPC_MethodName", RpcTarget.Others);

// 마스터 클라이언트에게만 전송
photonView.RPC("RPC_MethodName", RpcTarget.MasterClient);

// 매개변수 전달
photonView.RPC("RPC_MethodName", RpcTarget.All, param1, param2);
```

### RPC 메서드 정의
```csharp
[PunRPC]
void RPC_MethodName(int param1, string param2)
{
    // RPC 처리
}
```

### Photon Instantiate
```csharp
// 네트워크 오브젝트 생성
GameObject obj = PhotonNetwork.Instantiate("PrefabName", position, rotation);

// 네트워크 오브젝트 파괴
PhotonNetwork.Destroy(obj);
```

---

## 점수 시스템

### 점수 항목
| 항목 | 점수 |
|------|------|
| 보호구 정답 1개 | +100점 |
| 보호구 오답 1개 | -50점 |
| 프레스 작업 완료 (3회) | +200점 |
| 안전 위반 (손 감지) | -100점 |

### 예시 점수 계산
```
Player 1:
- 보호구: 안전화(O), 안전모(O), 더미1(X) → 100 + 100 - 50 = 150점
- 프레스 작업 완료 → +200점
- 안전 위반 1회 → -100점
총점: 150 + 200 - 100 = 250점

Player 2:
- 보호구: 안전화(O), 안전모(O), 안전장갑(O) → 100 + 100 + 100 = 300점
- 프레스 작업 완료 → +200점
총점: 300 + 200 = 500점 (1위)

Player 3:
- 보호구: 안전모(O), 더미1(X), 더미2(X) → 100 - 50 - 50 = 0점
- 프레스 작업 완료 → +200점
총점: 0 + 200 = 200점
```

---

## 입력 시스템

### 플레이어 이동
- **W/S**: 전진/후진
- **A/D**: 좌/우 이동
- **마우스 이동**: 카메라 회전

### 상호작용
- **E키**: 왼손 잡기/놓기
- **R키**: 오른손 잡기/놓기

---

## 태그 및 레이어

### 필수 태그
- `ProductMaterial` - 제품 소재
- `Interactable` - 상호작용 가능한 오브젝트
- `PlayerHand` - 플레이어 손
- `PressZone` - 프레스 작업 구역

### 필수 레이어 (선택사항)
- `Interactable` (Layer 8) - 상호작용 레이어

---

## 디버그 명령어

### 콘솔 로그 필터
Unity 콘솔에서 다음 키워드로 필터링:
- `[GameManager]` - 게임 흐름 로그
- `[VideoManager]` - 영상 관련 로그
- `[TurnManager]` - 턴 시스템 로그
- `[PressMachine]` - 프레스 작동 로그
- `[ScoreManager]` - 점수 관련 로그

### Phase 강제 전환 (개발용)
```csharp
// Inspector에서 GameManager 선택 후
// 또는 코드에서 직접 호출
GameManager.Instance.ChangePhase(GameManager.GamePhase.Result);
```

---

## 문제 해결 빠른 가이드

### Photon 연결 실패
1. App ID 확인 (Window > Photon Unity Networking > Highlight Server Settings)
2. 인터넷 연결 확인
3. 방화벽 설정 확인

### 플레이어가 생성되지 않음
1. Player 프리팹이 Resources 폴더에 있는지 확인
2. PhotonView 컴포넌트 확인
3. Scene에 NetworkManager가 있는지 확인

### 영상이 재생되지 않음
1. 영상 파일 경로 확인: `StreamingAssets/Videos/SafetyEducation.mp4`
2. VideoPlayer 컴포넌트 설정 확인
3. RenderTexture 할당 확인

### 점수가 동기화되지 않음
1. PhotonView 컴포넌트 확인
2. RPC 메서드에 `[PunRPC]` 어트리뷰트 있는지 확인
3. 마스터 클라이언트 권한 확인

### 프레스가 작동하지 않음
1. 현재 턴 플레이어인지 확인
2. PressHead Transform 할당 확인
3. 카운트다운 UI 확인

---

## 자주 사용하는 Unity 메서드

### GameObject 찾기
```csharp
// 타입으로 찾기
GameManager gameManager = FindObjectOfType<GameManager>();

// 모든 인스턴스 찾기
PlayerController[] players = FindObjectsOfType<PlayerController>();

// 태그로 찾기
GameObject obj = GameObject.FindWithTag("ProductMaterial");

// 이름으로 찾기 (권장하지 않음)
GameObject obj = GameObject.Find("ObjectName");
```

### Coroutine
```csharp
// Coroutine 시작
StartCoroutine(MyCoroutine());

// Coroutine 정의
IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(1.0f);
    // 1초 후 실행
}

// Coroutine 중지
StopCoroutine(myCoroutine);
```

### UI 업데이트
```csharp
// TextMeshProUGUI
using TMPro;
TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();
text.text = "새로운 텍스트";
text.color = Color.red;

// Button
using UnityEngine.UI;
Button button = GetComponent<Button>();
button.onClick.AddListener(OnButtonClick);
button.interactable = false; // 비활성화

// Image
Image image = GetComponent<Image>();
image.color = Color.green;
```

---

## 유용한 링크

- [Photon PUN 2 Documentation](https://doc.photonengine.com/pun/v2/getting-started/pun-intro)
- [Unity Scripting Reference](https://docs.unity3d.com/ScriptReference/)
- [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html)
- [Unity Video Player](https://docs.unity3d.com/Manual/class-VideoPlayer.html)

---

**빠른 참조 버전**: 1.0.0
**최종 업데이트**: 2025-01-31
