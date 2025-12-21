using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyItemUI : MonoBehaviour
{
    public TextMeshProUGUI lobbyNameText;
    public TextMeshProUGUI playerCountText;
    public Button joinButton;

    private Lobby _lobby;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private async void OnJoinClicked()
    {
        joinButton.interactable = false;

        // 1. Lobiye Katıl
        Lobby joinedLobby = await LobbyManager.JoinLobby(_lobby.Id);

        if (joinedLobby != null)
        {
            // Update main UI with joined lobby info if available
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.ShowLobbyDetails(joinedLobby);
            }

            // 2. Lobi Verisinden "JoinCode"u Çek
            string relayCode = joinedLobby.Data["JoinCode"].Value;

            // 3. Relay ile hemen bağlanma: Host oyunu başlatana kadar bekle.
            //    Bir waiter başlatıyoruz; host lobide "GameStarted" işaretini koyduğunda
            //    waiter RelayManager.JoinRelay çağıracak.
            LobbyJoinWaiter.StartWaiting(joinedLobby.Id, relayCode);
        }
        else
        {
            joinButton.interactable = true;
        }
    }
}