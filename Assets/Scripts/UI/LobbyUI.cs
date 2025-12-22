using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Services.Lobbies;
using System.Threading.Tasks;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("Main Panel")]
    public Button createLobbyButton;
    public Button refreshButton;
    public Button readyButton;
    public Image lobbyDetailsPanel;
    public TextMeshProUGUI lobbyName;
    public TextMeshProUGUI playerCount;
    public Button startGameButton;
    public TMP_InputField lobbyNameInput;

    [Header("List Panel")]
    public Transform contentParent;
    public Transform playerList;
    public GameObject lobbyItemPrefab;
    public GameObject playerListItem;

    private bool _isInLobby = false;
    private bool _localReady = false;

    private void Awake() { Instance = this; ClearLobbyDetails(); }

    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
    }

    private async void OnCreateClicked()
    {
        string name = string.IsNullOrWhiteSpace(lobbyNameInput.text) ? "New Lobby" : lobbyNameInput.text;
        createLobbyButton.interactable = false;

        string joinCode = await RelayManager.CreateRelay(4);
        if (!string.IsNullOrEmpty(joinCode))
        {
            Lobby createdLobby = await LobbyManager.CreateLobby(name, 4, joinCode);
            if (createdLobby != null) ShowLobbyDetails(createdLobby);
            else createLobbyButton.interactable = true;
        }
        else createLobbyButton.interactable = true;
    }

    private async void OnReadyClicked()
    {
        if (string.IsNullOrEmpty(LobbyManager.CurrentLobbyId)) return;
        _localReady = !_localReady;
        bool ok = await LobbyManager.SetPlayerReady(LobbyManager.CurrentLobbyId, _localReady);
        if (ok)
        {
            var txt = readyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = _localReady ? "Unready" : "Ready";
        }
        else _localReady = !_localReady;
    }

    private async void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null || !LobbyManager.IsHostLocal) return;
        if (!await LobbyManager.AreAllPlayersReady(LobbyManager.CurrentLobbyId)) return;

        startGameButton.interactable = false;
        // Olay Tabanlı Başlatma: Sunucu hazır olana kadar lobi bayrağını güncelleme
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        RelayManager.StartHost();
    }

    private async void HandleServerStarted()
    {
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        // ÖNCE sunucu başladı, ŞİMDİ bayrağı "1" yapıyoruz
        bool ok = await LobbyManager.SetGameStarted(LobbyManager.CurrentLobbyId);
        if (ok) NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void ShowLobbyDetails(Lobby lobby)
    {
        if (lobby == null) return;
        _isInLobby = true;
        if (lobbyName != null) lobbyName.text = lobby.Name;
        if (playerCount != null) playerCount.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        lobbyDetailsPanel.gameObject.SetActive(true);

        if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode") && !LobbyManager.IsHostLocal)
        {
            LobbyJoinWaiter.StartWaiting(lobby.Id, lobby.Data["JoinCode"].Value);
        }

        StopAllCoroutines();
        StartCoroutine(PollLobbyUpdates());
    }

    private IEnumerator PollLobbyUpdates()
    {
        while (_isInLobby && !string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
        {
            var task = LobbyManager.GetLobbyById(LobbyManager.CurrentLobbyId);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result != null)
            {
                Lobby lobby = task.Result;
                if (playerCount != null) playerCount.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
                RefreshPlayerList(lobby);
            }
            yield return new WaitForSeconds(3f);
        }
    }

    public void RefreshPlayerList(Lobby lobby)
    {
        foreach (Transform child in playerList) Destroy(child.gameObject);
        foreach (var p in lobby.Players)
        {
            GameObject item = Instantiate(playerListItem, playerList);
            PlayerListItemUI itemScript = item.GetComponent<PlayerListItemUI>();
            if (itemScript != null)
            {
                string pName = p.Data?.ContainsKey("PlayerName") == true ? p.Data["PlayerName"].Value : "Unknown";
                bool isReady = p.Data?.ContainsKey("Ready") == true && p.Data["Ready"].Value == "1";
                itemScript.Setup(pName, isReady);
            }
        }
    }

    public void ClearLobbyDetails()
    {
        _isInLobby = false;
        if (lobbyDetailsPanel != null) lobbyDetailsPanel.gameObject.SetActive(false);
        StopAllCoroutines();
    }
    
    private async void RefreshLobbyList()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        List<Lobby> lobbies = await LobbyManager.GetLobbies();
        foreach (Lobby lobby in lobbies)
        {
            GameObject item = Instantiate(lobbyItemPrefab, contentParent);
            LobbyItemUI itemScript = item.GetComponent<LobbyItemUI>();
            if (itemScript != null) itemScript.Setup(lobby);
        }
    }
}