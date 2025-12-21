using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

using Unity.Netcode;              // EKLE
using UnityEngine.SceneManagement; // EKLE

public class LobbyUI : MonoBehaviour
{
    [Header("Main Panel")]
    public Button createLobbyButton;
    public Button refreshButton;
    public Button startGameButton;
    public TMP_InputField lobbyNameInput;

    [Header("List Panel")]
    public Transform contentParent;
    public GameObject lobbyItemPrefab;

    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        // Başlangıçta güvenli olsun
        startGameButton.interactable = false;
    }

    private void Update()
    {
        // Start butonu sadece HOST/SERVER'da aktif olsun
        if (NetworkManager.Singleton == null)
        {
            startGameButton.interactable = false;
            return;
        }

        startGameButton.interactable = NetworkManager.Singleton.IsServer;
        // İsterseniz tamamen gizlemek:
        // startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
    }

    private async void OnCreateClicked()
    {
        string lobbyName = lobbyNameInput.text;
        if (string.IsNullOrEmpty(lobbyName)) lobbyName = "New Lobby";

        createLobbyButton.interactable = false;

        // 1) Relay aç + host başlat
        string joinCode = await RelayManager.CreateRelay(4);

        if (!string.IsNullOrEmpty(joinCode))
        {
            // 2) Lobby oluştur + join code göm
            await LobbyManager.CreateLobby(lobbyName, 4, joinCode);

            Debug.Log("Lobby Created & Host Started! Waiting for players...");
        }
        else
        {
            createLobbyButton.interactable = true;
        }
    }

    private async void RefreshLobbyList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        List<Lobby> lobbies = await LobbyManager.GetLobbies();

        foreach (Lobby lobby in lobbies)
        {
            GameObject item = Instantiate(lobbyItemPrefab, contentParent);
            LobbyItemUI itemScript = item.GetComponent<LobbyItemUI>();
            if (itemScript != null)
                itemScript.Setup(lobby);
        }
    }

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Only host can start the game.");
            return;
        }

        // ÖNEMLİ: NetworkManager > NetworkConfig > Enable Scene Management açık olmalı
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}
