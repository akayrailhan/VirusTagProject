using TMPro;
using UnityEngine;

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
        text.text = data.PlayerName.ToString();
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