using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// VR 퀴즈 시스템 자동 설정 스크립트
/// Unity 메뉴: Tools > VR Quiz > Auto Setup All
/// </summary>
public class VRQuizSetupAutomation : Editor
{
    [MenuItem("Tools/VR Quiz/Auto Setup All")]
    public static void AutoSetupAll()
    {
        Debug.Log("[VR Setup] 자동 설정 시작...");
        
        int successCount = 0;
        
        // Phase 1: EventSystem 설정
        if (SetupEventSystem())
        {
            successCount++;
            Debug.Log("[VR Setup] ✓ Phase 1: EventSystem 설정 완료");
        }
        
        // Phase 2: Canvas 설정
        if (SetupQuizCanvas())
        {
            successCount++;
            Debug.Log("[VR Setup] ✓ Phase 2: Canvas 설정 완료");
        }
        
        // Phase 3: 버튼 Collider 추가
        if (SetupButtonColliders())
        {
            successCount++;
            Debug.Log("[VR Setup] ✓ Phase 3: 버튼 Collider 설정 완료");
        }
        
        // Phase 4: WorkPoint 설정
        if (SetupWorkPoint())
        {
            successCount++;
            Debug.Log("[VR Setup] ✓ Phase 4: WorkPoint 설정 완료");
        }
        
        // Phase 5: Lobby Player 설정 (추가됨)
        if (SetupLobbyPlayer())
        {
            successCount++;
            Debug.Log("[VR Setup] ✓ Phase 5: Lobby Player 설정 완료");
        }
        
        EditorUtility.DisplayDialog(
            "VR 퀴즈 자동 설정 완료",
            $"설정 완료: {successCount}/5\n\n이제 Play 모드로 테스트해보세요!",
            "확인"
        );
    }

    // ... (기존 메서드들) ...

    /// <summary>
    /// Phase 5: Lobby Player 설정 (위치 및 중력 보정)
    /// </summary>
    static bool SetupLobbyPlayer()
    {
        GameObject lobbyRig = GameObject.Find("LobbyCameraRig");
        if (lobbyRig == null)
        {
            // 이름으로 못 찾으면 타입으로 검색
            VRPlayerController controller = GameObject.FindObjectOfType<VRPlayerController>();
            if (controller != null)
            {
                lobbyRig = controller.gameObject;
            }
        }

        if (lobbyRig != null)
        {
            // 1. 위치 보정 (눈높이 1.6m)
            // 기존 X, Z는 유지하고 Y만 1.6으로 변경
            Vector3 currentPos = lobbyRig.transform.position;
            lobbyRig.transform.position = new Vector3(currentPos.x, 1.6f, currentPos.z);
            
            // 2. 중력 제거 (바닥이 없어서 떨어지는 문제 방지)
            VRPlayerController vrController = lobbyRig.GetComponent<VRPlayerController>();
            if (vrController != null)
            {
                vrController.gravity = 0f; // 중력 끄기
                Debug.Log("[VR Setup] Lobby Player 중력 제거됨 (낙하 방지)");
            }
            
            Debug.Log($"[VR Setup] Lobby Player 위치 보정됨: {lobbyRig.transform.position}");
            EditorUtility.SetDirty(lobbyRig);
            return true;
        }
        else
        {
            Debug.LogWarning("[VR Setup] LobbyCameraRig를 찾을 수 없습니다.");
            return false;
        }
    }

    /// <summary>
    /// Phase 1: EventSystem 설정 (New Input System 호환)
    /// </summary>
    static bool SetupEventSystem()
    {
        // EventSystem 찾기 또는 생성
        EventSystem eventSystem = GameObject.FindObjectOfType<EventSystem>();
        
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
            Debug.Log("[VR Setup] EventSystem 생성됨");
        }
        
        // Standalone Input Module 제거 (구형 방식 - 충돌 원인)
        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            DestroyImmediate(standalone);
            Debug.Log("[VR Setup] Standalone Input Module 제거됨 (충돌 해결)");
        }
        
        // InputSystemUIInputModule 추가 (신형 방식 - 마우스 지원)
        // 리플렉션이나 문자열로 찾지 않고, XRUIInputModule이 있으면 그걸 우선하되,
        // 마우스 테스트를 위해 InputSystemUIInputModule을 추가 시도
        
        // 주의: XRUIInputModule은 VR 필수이므로 유지
        XRUIInputModule xrInput = eventSystem.GetComponent<XRUIInputModule>();
        if (xrInput == null)
        {
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("[VR Setup] XRUIInputModule 추가됨 (VR 필수)");
        }

        // New Input System용 UI 모듈 추가 (마우스 클릭용)
        // 타입이 없을 수 있으므로 문자열로 체크하지 않고, UnityEngine.InputSystem.UI가 있다고 가정
        // 만약 컴파일 에러가 나면 사용자가 패키지를 확인해야 함.
        // 안전하게 컴포넌트 이름으로 추가 시도
        var inputModule = eventSystem.GetComponent("InputSystemUIInputModule");
        if (inputModule == null)
        {
            // 리플렉션으로 추가 (네임스페이스 의존성 피하기 위해)
            System.Type inputType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputType != null)
            {
                eventSystem.gameObject.AddComponent(inputType);
                Debug.Log("[VR Setup] InputSystemUIInputModule 추가됨 (마우스 지원)");
            }
            else
            {
                Debug.LogWarning("[VR Setup] InputSystem 패키지를 찾을 수 없어 마우스 모듈을 추가하지 못했습니다.");
            }
        }
        
        EditorUtility.SetDirty(eventSystem.gameObject);
        return true;
    }

    /// <summary>
    /// Phase 2: QuizCanvas 설정
    /// </summary>
    static bool SetupQuizCanvas()
    {
        // QuizCanvas 찾기
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas quizCanvas = null;
        
        foreach (Canvas canvas in canvases)
        {
            if (EditorUtility.IsPersistent(canvas.transform.root.gameObject)) continue;
            if (canvas.name.Contains("Quiz"))
            {
                quizCanvas = canvas;
                break;
            }
        }
        
        if (quizCanvas == null)
        {
            Debug.LogWarning("[VR Setup] QuizCanvas를 찾을 수 없습니다. 수동으로 설정하세요.");
            return false;
        }
        
        // Render Mode를 World Space로 변경
        quizCanvas.renderMode = RenderMode.WorldSpace;
        
        // Event Camera 설정 및 부모 설정 (HUD 모드)
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            quizCanvas.worldCamera = mainCamera;
            
            // 캔버스를 카메라의 자식으로 설정 (HUD 효과)
            if (quizCanvas.transform.parent != mainCamera.transform)
            {
                quizCanvas.transform.SetParent(mainCamera.transform);
                Debug.Log("[VR Setup] QuizCanvas가 HUD 모드로 설정됨 (카메라 자식)");
            }
            
            // 주의: 이미 사용자가 UI를 조정했으므로 Transform은 건드리지 않음!
        }
        else
        {
            Debug.LogWarning("[VR Setup] Main Camera를 찾을 수 없어 World Space로 유지합니다.");
        }
        
        // Tracked Device Graphic Raycaster 추가 (VR용)
        TrackedDeviceGraphicRaycaster vrRaycaster = quizCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (vrRaycaster == null)
        {
            quizCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log("[VR Setup] Tracked Device Graphic Raycaster 추가됨");
        }

        // Graphic Raycaster 추가 (마우스용 - New Input System과 호환됨)
        GraphicRaycaster mouseRaycaster = quizCanvas.GetComponent<GraphicRaycaster>();
        if (mouseRaycaster == null)
        {
            quizCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("[VR Setup] Graphic Raycaster 추가됨 (마우스 지원)");
        }
        
        // Layer를 UI로 설정
        SetLayerRecursively(quizCanvas.gameObject, LayerMask.NameToLayer("UI"));
        
        EditorUtility.SetDirty(quizCanvas.gameObject);
        return true;
    }

    /// <summary>
    /// Phase 3: 버튼에 Collider 추가
    /// </summary>
    static bool SetupButtonColliders()
    {
        // QuizCanvas 찾기
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        Canvas quizCanvas = null;
        
        foreach (Canvas canvas in canvases)
        {
            // 에셋이 아닌 씬에 있는 객체만
            if (EditorUtility.IsPersistent(canvas.transform.root.gameObject)) continue;

            if (canvas.name.Contains("Quiz"))
            {
                quizCanvas = canvas;
                break;
            }
        }

        if (quizCanvas == null)
        {
            Debug.LogWarning("[VR Setup] QuizCanvas를 찾을 수 없습니다.");
            return false;
        }

        // QuizCanvas 하위의 모든 Button 찾기 (비활성화된 것도 포함)
        Button[] buttons = quizCanvas.GetComponentsInChildren<Button>(true);
        int colliderCount = 0;
        
        foreach (Button button in buttons)
        {
            // 이미 Collider가 있으면 패스
            BoxCollider existingCollider = button.GetComponent<BoxCollider>();
            if (existingCollider != null) continue;
            
            // Box Collider 추가
            BoxCollider collider = button.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            
            // 버튼 크기에 맞게 자동 설정
            RectTransform rect = button.GetComponent<RectTransform>();
            collider.size = new Vector3(rect.rect.width, rect.rect.height, 10f);
            
            colliderCount++;
            Debug.Log($"[VR Setup] {button.name}에 Collider 추가됨");
        }
        
        if (colliderCount > 0 || buttons.Length > 0)
        {
            Debug.Log($"[VR Setup] 총 {colliderCount}개 버튼에 Collider 추가 완료 (총 버튼 수: {buttons.Length})");
            return true;
        }
        else
        {
            Debug.LogWarning("[VR Setup] 버튼을 찾을 수 없습니다.");
            return false;
        }
    }

    /// <summary>
    /// Phase 4: WorkPoint 설정
    /// </summary>
    static bool SetupWorkPoint()
    {
        // GameManager 찾기
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("[VR Setup] GameManager를 찾을 수 없습니다.");
            return false;
        }
        
        // WorkPoint가 이미 있으면 Y값만 보정
        if (gameManager.workPoint != null)
        {
            Transform workPoint = gameManager.workPoint;
            
            // 위치 및 회전 설정 (롤백: 다시 자동 설정 활성화)
            workPoint.position = new Vector3(0, 1.6f, 0); // 사람 눈높이 (1.6m)
            workPoint.rotation = Quaternion.Euler(0, 0, 0); // 캔버스를 바라보도록 (0도)
            
            Debug.Log($"[VR Setup] WorkPoint 위치 및 회전 보정됨 (0, 1.6, 0)");
        }
        else
        {
            // WorkPoint 새로 생성
            GameObject wpObj = new GameObject("WorkPoint");
            wpObj.transform.position = new Vector3(0f, 1.6f, 0f); // 사람 눈높이 (1.6m)
            wpObj.transform.rotation = Quaternion.Euler(0, 0, 0); // 0도로 변경 (앞을 봄)
            gameManager.workPoint = wpObj.transform;
            Debug.Log("[VR Setup] WorkPoint 생성됨");
        }
        
        EditorUtility.SetDirty(gameManager);
        return true;
    }

    /// <summary>
    /// 재귀적으로 레이어 설정
    /// </summary>
    static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // 개별 설정 메뉴들
    [MenuItem("Tools/VR Quiz/1. Setup EventSystem Only")]
    public static void SetupEventSystemOnly()
    {
        if (SetupEventSystem())
            EditorUtility.DisplayDialog("완료", "EventSystem 설정 완료", "확인");
    }

    [MenuItem("Tools/VR Quiz/2. Setup Canvas Only")]
    public static void SetupCanvasOnly()
    {
        if (SetupQuizCanvas())
            EditorUtility.DisplayDialog("완료", "Canvas 설정 완료", "확인");
    }

    [MenuItem("Tools/VR Quiz/3. Setup Button Colliders Only")]
    public static void SetupButtonCollidersOnly()
    {
        if (SetupButtonColliders())
            EditorUtility.DisplayDialog("완료", "버튼 Collider 설정 완료", "확인");
    }

    [MenuItem("Tools/VR Quiz/4. Setup WorkPoint Only")]
    public static void SetupWorkPointOnly()
    {
        if (SetupWorkPoint())
            EditorUtility.DisplayDialog("완료", "WorkPoint 설정 완료", "확인");
    }
}
#endif
