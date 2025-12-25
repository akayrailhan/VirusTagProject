using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<PlayerData> CurrentState = new NetworkVariable<PlayerData>(
        new PlayerData { PlayerName = "Player", IsInfected = false, Score = 0 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    // ✅ Temizken (infected değilken) sprite'ın normal rengi neyse O kalsın.
    // Sadece infected olunca bu renge boyayacağız.
    [SerializeField] private Color infectedColor = Color.red;

    // Sprite'ın başlangıçtaki (orijinal) rengini saklarız
    private Color _defaultColor = Color.white;

    private void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        if (bodyRenderer != null)
            _defaultColor = bodyRenderer.color; // ✅ orijinal rengi kaydet
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnStateChanged;

        // İlk renk güncellemesi
        UpdateColor(CurrentState.Value.IsInfected);

        if (IsServer)
            InfectionManager.Instance?.RegisterPlayer(this);

        if (IsOwner)
        {
            string name = ApplicationController.Instance != null
                ? ApplicationController.Instance.GetPlayerName()
                : "Player";

            SubmitNameServerRpc(name);
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(PlayerData previous, PlayerData current)
    {
        UpdateColor(current.IsInfected);
    }

    private void UpdateColor(bool isInfected)
    {
        if (bodyRenderer == null) return;

        // ✅ infected değilse sprite’ın normal rengine dön
        bodyRenderer.color = isInfected ? infectedColor : _defaultColor;
    }

    public void SetInfectionStatus(bool isInfected)
    {
        if (!IsServer) return;

        PlayerData data = CurrentState.Value;
        data.IsInfected = isInfected;
        CurrentState.Value = data;
    }

    [ServerRpc]
    private void SubmitNameServerRpc(string playerName)
    {
        if (!IsServer) return;
        InitPlayer(OwnerClientId, playerName);
    }

    public void InitPlayer(ulong clientId, string name)
    {
        if (!IsServer) return;

        PlayerData data = CurrentState.Value;
        data.ClientId = clientId;
        data.PlayerName = new FixedString32Bytes(name);
        CurrentState.Value = data;
    }
}
