using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

// Her client kendi local oyuncusunu takip eden Cinemachine kamera
[RequireComponent(typeof(CinemachineCamera))]
public class CameraFollowLocalPlayer : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private float searchInterval = 0.5f;

    private float _nextSearchTime;
    private bool _locked;

    private void Awake()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineCamera>();
    }

    private void Update()
    {
        if (_locked) return;
        if (virtualCamera == null) return;

        if (Time.time < _nextSearchTime) return;
        _nextSearchTime = Time.time + searchInterval;

        TryLockOnLocalPlayer();
    }

    private void TryLockOnLocalPlayer()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClient == null) return;

        var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (playerObject == null) return;

        Transform target = playerObject.transform;
        virtualCamera.Follow = target;
        virtualCamera.LookAt = target;
        _locked = true;
    }
}