using TMPro;
using UnityEngine;
using Unity.Netcode;

public class PlayerNameTag : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerState playerState;
    [SerializeField] private TextMeshPro text;

    private void Awake()
    {
        // Inspector’da atanmadıysa otomatik bulmayı dene
        if (playerState == null)
            playerState = GetComponentInParent<PlayerState>();
        if (text == null)
            text = GetComponentInChildren<TextMeshPro>();
    }

    private void Start()
    {
        UpdateName();

    }

    private void UpdateName()
    {
        if (playerState == null || text == null) return;

        var data = playerState.CurrentState.Value;
        string name = data.PlayerName.ToString();

        bool isLocal =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClientId == data.ClientId;

        if (isLocal)
        {
            // Skorboard ile aynı stil: sarı + kalın
            text.text = $"<color=yellow><b>{name}</b></color>";
        }
        else
        {
            text.text = name;
        }
    }

    private void OnEnable()
    {
        if (playerState == null)
            playerState = GetComponentInParent<PlayerState>();

        if (playerState != null)
            playerState.CurrentState.OnValueChanged += OnStateChanged;

        UpdateName(); // ilk açılışta da güncelle
    }

    private void OnDisable()
    {
        if (playerState != null)
            playerState.CurrentState.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(PlayerData previous, PlayerData current)
    {
        UpdateName();
    }
}