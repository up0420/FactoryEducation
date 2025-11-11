using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다른 플레이어들의 아바타를 생성하고 관리하는 매니저
/// </summary>
public class RemotePlayerManager : MonoBehaviour {
    [Header("Avatar Prefabs")]
    public GameObject[] avatarPrefabs;  // 다양한 색상의 HumanDummy 프리팹들

    [Header("Settings")]
    public float positionSmoothing = 10f;  // 위치 보간 속도
    public float rotationSmoothing = 10f;  // 회전 보간 속도

    // 플레이어 ID -> 아바타 오브젝트 매핑
    private readonly Dictionary<string, GameObject> _avatars = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Vector3> _targetPositions = new Dictionary<string, Vector3>();
    private readonly Dictionary<string, Quaternion> _targetRotations = new Dictionary<string, Quaternion>();

    private int _nextPrefabIndex = 0;

    /// <summary>
    /// 새로운 플레이어의 아바타를 생성
    /// </summary>
    public GameObject CreateAvatar(string playerId, Vector3 position, Quaternion rotation) {
        if (_avatars.ContainsKey(playerId)) {
            Debug.LogWarning($"Avatar for {playerId} already exists!");
            return _avatars[playerId];
        }

        // 프리팹이 없으면 기본 큐브 생성
        GameObject avatar;
        if (avatarPrefabs != null && avatarPrefabs.Length > 0) {
            // 순환하면서 다른 색상의 아바타 생성
            int prefabIndex = _nextPrefabIndex % avatarPrefabs.Length;
            _nextPrefabIndex++;
            avatar = Instantiate(avatarPrefabs[prefabIndex], position, rotation);
        } else {
            // 프리팹이 없으면 기본 캡슐 생성
            avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.transform.position = position;
            avatar.transform.rotation = rotation;
            avatar.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        }

        avatar.name = $"RemotePlayer_{playerId}";
        _avatars[playerId] = avatar;
        _targetPositions[playerId] = position;
        _targetRotations[playerId] = rotation;

        // 이름표 추가 (선택 사항)
        CreateNameTag(avatar, playerId);

        Debug.Log($"Created avatar for player: {playerId}");
        return avatar;
    }

    /// <summary>
    /// 플레이어의 목표 위치/회전 업데이트 (실제 이동은 Update에서 보간)
    /// </summary>
    public void UpdatePlayerPose(string playerId, Vector3 position, Quaternion rotation) {
        if (!_avatars.ContainsKey(playerId)) {
            // 아바타가 없으면 생성
            CreateAvatar(playerId, position, rotation);
            return;
        }

        _targetPositions[playerId] = position;
        _targetRotations[playerId] = rotation;
    }

    /// <summary>
    /// 플레이어의 아바타를 제거
    /// </summary>
    public void RemoveAvatar(string playerId) {
        if (_avatars.TryGetValue(playerId, out GameObject avatar)) {
            Destroy(avatar);
            _avatars.Remove(playerId);
            _targetPositions.Remove(playerId);
            _targetRotations.Remove(playerId);
            Debug.Log($"Removed avatar for player: {playerId}");
        }
    }

    /// <summary>
    /// 특정 플레이어의 아바타 가져오기
    /// </summary>
    public GameObject GetAvatar(string playerId) {
        return _avatars.TryGetValue(playerId, out GameObject avatar) ? avatar : null;
    }

    /// <summary>
    /// 모든 아바타 제거
    /// </summary>
    public void RemoveAllAvatars() {
        foreach (var avatar in _avatars.Values) {
            if (avatar != null) Destroy(avatar);
        }
        _avatars.Clear();
        _targetPositions.Clear();
        _targetRotations.Clear();
    }

    void Update() {
        // 부드러운 보간으로 아바타 이동
        foreach (var kvp in _avatars) {
            string playerId = kvp.Key;
            GameObject avatar = kvp.Value;

            if (avatar == null) continue;

            if (_targetPositions.TryGetValue(playerId, out Vector3 targetPos)) {
                avatar.transform.position = Vector3.Lerp(
                    avatar.transform.position,
                    targetPos,
                    Time.deltaTime * positionSmoothing
                );
            }

            if (_targetRotations.TryGetValue(playerId, out Quaternion targetRot)) {
                avatar.transform.rotation = Quaternion.Slerp(
                    avatar.transform.rotation,
                    targetRot,
                    Time.deltaTime * rotationSmoothing
                );
            }
        }
    }

    /// <summary>
    /// 아바타 위에 이름표 생성 (선택 사항)
    /// </summary>
    private void CreateNameTag(GameObject avatar, string playerId) {
        GameObject nameTagObj = new GameObject("NameTag");
        nameTagObj.transform.SetParent(avatar.transform);
        nameTagObj.transform.localPosition = new Vector3(0, 2.2f, 0);

        // 3D TextMesh 생성
        TextMesh textMesh = nameTagObj.AddComponent<TextMesh>();
        textMesh.text = playerId;
        textMesh.characterSize = 0.1f;
        textMesh.fontSize = 50;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // 카메라를 향하도록 하는 스크립트 추가 (선택 사항)
        nameTagObj.AddComponent<Billboard>();
    }

    void OnDestroy() {
        RemoveAllAvatars();
    }
}

/// <summary>
/// 이름표가 항상 카메라를 향하도록 하는 헬퍼 클래스
/// </summary>
public class Billboard : MonoBehaviour {
    private Camera _mainCamera;

    void Start() {
        _mainCamera = Camera.main;
    }

    void LateUpdate() {
        if (_mainCamera != null) {
            transform.LookAt(transform.position + _mainCamera.transform.forward);
        }
    }
}
