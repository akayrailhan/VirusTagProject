using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    private void Awake()
    {
        Instance = this;
        // Başlangıçta oyuncu herhangi bir lobide değilse alanları gizle
        ClearLobbyDetails();
    }
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
    public Transform contentParent; // Liste elemanlarının dizileceği yer
    public Transform playerList;
    public GameObject lobbyItemPrefab; // Listedeki tek satırın tasarımı (Prefab)
    public GameObject playerListItem;
    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
    }

    private bool _localReady = false;

    private async void OnReadyClicked()
    {
        if (string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
        {
            Debug.LogWarning("Not in a lobby.");
            return;
        }

        _localReady = !_localReady;
        bool ok = await LobbyManager.SetPlayerReady(LobbyManager.CurrentLobbyId, _localReady);
        if (!ok)
        {
            Debug.LogWarning("Failed to set ready state.");
            _localReady = !_localReady; // revert
            return;
        }

        // Update button label if it's a TMP text child
        var txt = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = _localReady ? "Unready" : "Ready";

        // Refresh list to show updated ready states
        RefreshPlayerList();
    }

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

    private async void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null) return;

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
        InfectionManager.Instance?.StartMatchServerRpc();
        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    private async void RefreshLobbyList()
    {
        // Eski listeyi temizle
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Yeni listeyi çek
        List<Lobby> lobbies = await LobbyManager.GetLobbies();

        // Ekrana bas
        foreach (Lobby lobby in lobbies)
        {
            GameObject item = Instantiate(lobbyItemPrefab, contentParent);
            // LobbyItem bileşenini bul ve veriyi doldur
            LobbyItemUI itemScript = item.GetComponent<LobbyItemUI>();
            if (itemScript != null)
            {
                itemScript.Setup(lobby);
            }
        }
    }

    private async void RefreshPlayerList()
    {
        // default: fetch from server
        var players = await LobbyManager.GetPlayersInfoById(LobbyManager.CurrentLobbyId);
        RefreshPlayerListFromInfo(players);
    }

    // Overload: populate list immediately from a Lobby instance
    private void RefreshPlayerList(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        var infos = new System.Collections.Generic.List<LobbyManager.PlayerInfo>();
        if (lobby != null && lobby.Players != null)
        {
            foreach (var p in lobby.Players)
            {
                var info = new LobbyManager.PlayerInfo { Id = p.Id, Name = p.Id, Ready = false };
                if (p.Data != null)
                {
                    if (p.Data.ContainsKey("PlayerName") && !string.IsNullOrEmpty(p.Data["PlayerName"].Value))
                        info.Name = p.Data["PlayerName"].Value;
                    if (p.Data.ContainsKey("Ready") && p.Data["Ready"].Value == "1")
                        info.Ready = true;
                }
                infos.Add(info);
            }
        }

        RefreshPlayerListFromInfo(infos);
    }

    private void RefreshPlayerListFromInfo(System.Collections.Generic.List<LobbyManager.PlayerInfo> players)
    {
        // Eski listeyi temizle
        foreach (Transform child in playerList) Destroy(child.gameObject);

        // Ekrana bas
        foreach (var p in players)
        {
            GameObject item = Instantiate(playerListItem, playerList, false);
            // ensure transform/scale are correct for UI layout
            item.transform.localScale = Vector3.one;
            var itemRt = item.GetComponent<RectTransform>();
            if (itemRt != null)
            {
                // reset anchors/position so LayoutGroup controls placement
                itemRt.anchorMin = new Vector2(0f, 1f);
                itemRt.anchorMax = new Vector2(1f, 1f);
                itemRt.anchoredPosition = Vector2.zero;
                itemRt.sizeDelta = new Vector2(itemRt.sizeDelta.x, itemRt.sizeDelta.y);

                // ensure there's a LayoutElement so parent LayoutGroup can size it
                var le = item.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le == null) le = item.AddComponent<UnityEngine.UI.LayoutElement>();
                float pref = UnityEngine.UI.LayoutUtility.GetPreferredHeight(itemRt);
                if (pref <= 0f) pref = 30f; // fallback
                le.preferredHeight = pref;
                le.flexibleWidth = 1f;
            }
            // PlayerListItemUI bileşenini bulup ismi ata
            PlayerListItemUI itemScript = item.GetComponent<PlayerListItemUI>();
            if (itemScript != null)
            {
                itemScript.Setup(p.Name, p.Ready);
                continue;
            }

            // Fallback: doğrudan TextMeshPro veya Text bulunup yaz
            var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) { tmp.text = p.Name; continue; }
            var uiText = item.GetComponentInChildren<Text>();
            if (uiText != null) uiText.text = p.Name;
        }
        // Force layout rebuild so items are positioned correctly (requires a LayoutGroup on parent)
        var rt = playerList as RectTransform;
        if (rt != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    // Helpers to detect lobby changes (players join/leave or ready state changes)
    private void UpdatePlayerCacheFromLobby(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        _playerReadyCache.Clear();
        if (lobby == null || lobby.Players == null) return;
        foreach (var p in lobby.Players)
        {
            bool ready = false;
            if (p.Data != null && p.Data.ContainsKey("Ready") && p.Data["Ready"].Value == "1") ready = true;
            _playerReadyCache[p.Id] = ready;
        }
    }

    private bool HasLobbyChanged(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        if (lobby == null || lobby.Players == null) return false;

        // count changed
        if (lobby.Players.Count != _playerReadyCache.Count) return true;

        foreach (var p in lobby.Players)
        {
            bool ready = false;
            if (p.Data != null && p.Data.ContainsKey("Ready") && p.Data["Ready"].Value == "1") ready = true;

            if (!_playerReadyCache.ContainsKey(p.Id)) return true;
            if (_playerReadyCache[p.Id] != ready) return true;
        }

        return false;
    }

    private System.Collections.Generic.List<LobbyManager.PlayerInfo> ConvertLobbyToPlayerInfo(Unity.Services.Lobbies.Models.Lobby lobby)
    {
        var infos = new System.Collections.Generic.List<LobbyManager.PlayerInfo>();
        if (lobby == null || lobby.Players == null) return infos;

        foreach (var p in lobby.Players)
        {
            var info = new LobbyManager.PlayerInfo { Id = p.Id, Name = p.Id, Ready = false };
            if (p.Data != null)
            {
                if (p.Data.ContainsKey("PlayerName") && !string.IsNullOrEmpty(p.Data["PlayerName"].Value))
                    info.Name = p.Data["PlayerName"].Value;
                if (p.Data.ContainsKey("Ready") && p.Data["Ready"].Value == "1")
                    info.Ready = true;
            }
            infos.Add(info);
        }

        return infos;
    }

    private System.Collections.IEnumerator PollLobbyUpdates()
    {
        while (_isInLobby && !string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
        {
            var task = LobbyManager.GetLobbyById(LobbyManager.CurrentLobbyId);
            while (!task.IsCompleted) yield return null;

            var lobby = task.Result;
            if (lobby != null && lobby.Players != null)
            {
                _lastPlayerCount = lobby.Players.Count;
                if (playerCount != null)
                {
                    playerCount.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
                    playerCount.gameObject.SetActive(true);
                }

                // Only refresh UI when players join/leave or ready state changes
                if (HasLobbyChanged(lobby))
                {
                    var infos = ConvertLobbyToPlayerInfo(lobby);
                    RefreshPlayerListFromInfo(infos);
                    UpdatePlayerCacheFromLobby(lobby);
                }
            }

            float timer = 0f;
            while (timer < 3f)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

    }
}