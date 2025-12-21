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

    public Image lobbyDetailsPanel;

    public TextMeshProUGUI lobbyName;
    public TextMeshProUGUI playerCount;
    public Button startGameButton;
    public TMP_InputField lobbyNameInput;

    [Header("List Panel")]
    public Transform contentParent; // Liste elemanlarının dizileceği yer
    public GameObject lobbyItemPrefab; // Listedeki tek satırın tasarımı (Prefab)

    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        startGameButton.onClick.AddListener(OnStartGameClicked);
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
        }

        _isInLobby = true;
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
    }

    private bool _isInLobby = false;

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null) return;

        // Only the local lobby owner can start the game (we track ownership in LobbyManager)
        if (!LobbyManager.IsHostLocal)
        {
            Debug.Log("Only host can start the game.");
            return;
        }

        // 1) Host'u başlat (Relay transport zaten hazırlanmıştı)
        RelayManager.StartHost();

        // 2) Lobide GameStarted bayrağını koy, böylece client'lar relay'e bağlanmaya başlar
        if (!string.IsNullOrEmpty(LobbyManager.CurrentLobbyId))
        {
            _ = LobbyManager.SetGameStarted(LobbyManager.CurrentLobbyId);
        }

        // 3) Sahneyi yükle (sunucu sahneyi yüklerse client'lar da otomatik geçer)
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
}