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

        Debug.Log($"[LobbyItemUI] JoinLobby returned: {(joinedLobby != null ? "OK" : "NULL")}");
        if (joinedLobby != null)
        {
            Debug.Log($"[LobbyItemUI] Joined lobby players: {joinedLobby.Players.Count}");
            foreach (var p in joinedLobby.Players)
            {
                var name = (p.Data != null && p.Data.ContainsKey("PlayerName")) ? p.Data["PlayerName"].Value : p.Id;
                Debug.Log($" - Player: id={p.Id} name={name}");
            }
        }

        if (joinedLobby != null)
        {
            // Update main UI with joined lobby info if available
            if (LobbyUI.Instance != null)
            {
                LobbyUI.Instance.ShowLobbyDetails(joinedLobby);
            }

            // 2. Lobi Verisinden "JoinCode"u Çek ve HEMEN Relay'e bağlan
            if (joinedLobby.Data != null && joinedLobby.Data.ContainsKey("JoinCode") && !string.IsNullOrEmpty(joinedLobby.Data["JoinCode"].Value))
            {
                string relayCode = joinedLobby.Data["JoinCode"].Value;

                Debug.Log($"[LobbyItemUI] Joining Relay immediately with joinCode={relayCode}");

                bool relayOk = await RelayManager.JoinRelay(relayCode);
                if (!relayOk)
                {
                    Debug.LogError("[LobbyItemUI] Failed to join relay; client will not connect to host.");
                    joinButton.interactable = true;
                }
            }
            else
            {
                Debug.LogError("[LobbyItemUI] Joined lobby does not contain JoinCode in Data. Cannot join relay.");
                joinButton.interactable = true;
            }
        }
        else
        {
            joinButton.interactable = true;
        }
    }
}