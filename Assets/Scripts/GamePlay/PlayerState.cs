using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerState : NetworkBehaviour
{
    // Tüm oyuncular bu değişkeni görür ve takip eder
    public NetworkVariable<PlayerData> CurrentState = new NetworkVariable<PlayerData>(
        new PlayerData { PlayerName = "Player", IsInfected = false, Score = 0 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Sadece sunucu değiştirebilir
    );

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color cleanColor = Color.blue;
    [SerializeField] private Color infectedColor = Color.red;

    public override void OnNetworkSpawn()
{
    CurrentState.OnValueChanged += OnStateChanged;
    UpdateColor(CurrentState.Value.IsInfected);

    if (IsServer)
        InfectionManager.Instance?.RegisterPlayer(this); // opsiyonel
}




    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnStateChanged;
    }

    // Durum değişince çalışacak fonksiyon
    private void OnStateChanged(PlayerData previous, PlayerData current)
    {
        // Renk güncelle
        UpdateColor(current.IsInfected);

        // İsim güncelle (İleride UI için)
        // Debug.Log($"Player {current.PlayerName} state changed. Infected: {current.IsInfected}");
    }

    private void UpdateColor(bool isInfected)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.color = isInfected ? infectedColor : cleanColor;
        }
    }

    // Sunucu tarafında virüs bulaştırma fonksiyonu
    public void SetInfectionStatus(bool isInfected)
    {
        if (!IsServer) return; // Sadece sunucu yapabilir

        // Mevcut veriyi al, değiştir ve geri yaz (Struct olduğu için)
        PlayerData data = CurrentState.Value;
        data.IsInfected = isInfected;
        CurrentState.Value = data;
    }

    // İsmi ve ID'yi ayarla (Connection Approval sonrası çağrılacak)
    public void InitPlayer(ulong clientId, string name)
    {
        if (!IsServer) return;

        PlayerData data = CurrentState.Value;
        data.ClientId = clientId;
        data.PlayerName = new FixedString32Bytes(name);
        CurrentState.Value = data;
    }
}