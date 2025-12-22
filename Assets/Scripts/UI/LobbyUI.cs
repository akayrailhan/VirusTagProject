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

<<<<<<< HEAD
=======
    private async void OnCreateClicked()
    {
        string lobbyName = lobbyNameInput.text;
        if (string.IsNullOrEmpty(lobbyName)) lobbyName = "New Lobby";

        createLobbyButton.interactable = false;

        // 1. Önce Relay (Sunucu) Aç ve Kod Al
        string joinCode = await RelayManager.CreateRelay(4); // Max 4 oyuncu

        if (joinCode != null)
        {
            // 2. Sonra Lobi Aç ve Kodu İçine Göm
            var createdLobby = await LobbyManager.CreateLobby(lobbyName, 4, joinCode);

            // Show created lobby details in main panel
            if (createdLobby != null)
            {
                ShowLobbyDetails(createdLobby);

                // Host'u hemen başlat (Lobby sahnesindeyken)
                // Böylece client'lar lobiye katılır katılmaz Relay'e bağlanıp
                // NetworkManager üzerinden host'a bağlanabilirler.
                RelayManager.StartHost();
            }

            // 3. Oyun Sahnesine Geç (KRİTİK ADIM)
            // NetworkManager.SceneManager kullanarak sahne yüklenir.
            // Bu sayede bağlı olan ve sonradan gelen tüm Client'lar da otomatik olarak bu sahneye geçer.
            //NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

            Debug.Log("Lobby Created & Host Started! Loading Game Scene...");
        }
        else
        {
            createLobbyButton.interactable = true;
        }
    }

    public void ShowLobbyDetails(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobbyName != null)
        {
            lobbyName.text = lobby.Name;
            lobbyName.gameObject.SetActive(true);
            lobbyDetailsPanel.gameObject.SetActive(true);
        }

        if (playerCount != null)
        {
            playerCount.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
            playerCount.gameObject.SetActive(true);
            // populate immediately from provided lobby to avoid timing issues
            RefreshPlayerList(lobby);
        }

        _isInLobby = true;

        // polling disabled: UI will update on explicit events (join/create/ready)

        // initialize cache from provided lobby
        if (lobby != null)
        {
            UpdatePlayerCacheFromLobby(lobby);
        }

        StopAllCoroutines(); // Önce varsa eskiyi durdur
        StartCoroutine(PollLobbyUpdates());

    }

    // Oyuncu herhangi bir lobide değilse çağrılır
    public void ClearLobbyDetails()
    {
        _isInLobby = false;
        if (lobbyName != null)
        {
            lobbyName.text = string.Empty;
            lobbyName.gameObject.SetActive(false);
            lobbyDetailsPanel.gameObject.SetActive(false);
        }

        if (playerCount != null)
        {
            playerCount.text = string.Empty;
            playerCount.gameObject.SetActive(false);
        }

        // polling disabled: nothing to stop
        StopAllCoroutines();
    }

    private bool _isInLobby = false;

    private int _lastPlayerCount = -1;
    // cache player ready states to avoid unnecessary UI refreshes
    private System.Collections.Generic.Dictionary<string, bool> _playerReadyCache = new System.Collections.Generic.Dictionary<string, bool>();

>>>>>>> fix-from-daab11f
    private async void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null || !LobbyManager.IsHostLocal) return;
        if (!await LobbyManager.AreAllPlayersReady(LobbyManager.CurrentLobbyId)) return;

<<<<<<< HEAD
        startGameButton.interactable = false;
        // Olay Tabanlı Başlatma: Sunucu hazır olana kadar lobi bayrağını güncelleme
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        RelayManager.StartHost();
=======
        // Only the local lobby owner can start the game (we track ownership in LobbyManager)
        if (!LobbyManager.IsHostLocal)
        {
            Debug.Log("Only host can start the game.");
            return;
        }

        // Before starting, ensure all players are ready
        if (string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
        {
            Debug.LogWarning("No current lobby id set.");
            return;
        }

        bool allReady = await LobbyManager.AreAllPlayersReady(LobbyManager.CurrentLobbyId);
        if (!allReady)
        {
            Debug.Log("Cannot start game: not all players are ready.");
            return;
        }

        // Host zaten Lobby oluştururken başlatıldı.
        // Sadece GameStarted bayrağını güncelle (isteğe bağlı) ve sahneyi yükle.
        _ = await LobbyManager.SetGameStarted(LobbyManager.CurrentLobbyId);

        // Networked scene load: host Game sahnesini yüklediğinde, bağlı tüm client'lar
        // otomatik olarak aynı sahneye geçer.
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
>>>>>>> fix-from-daab11f
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