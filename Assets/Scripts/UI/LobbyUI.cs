using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public class LobbyUI : MonoBehaviour
{
    [Header("Main Panel")]
    public Button createLobbyButton;
    public Button refreshButton;
    public TMP_InputField lobbyNameInput;

    [Header("List Panel")]
    public Transform contentParent; // Liste elemanlarının dizileceği yer
    public GameObject lobbyItemPrefab; // Listedeki tek satırın tasarımı (Prefab)

    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateClicked);
        refreshButton.onClick.AddListener(RefreshLobbyList);
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
            await LobbyManager.CreateLobby(lobbyName, 4, joinCode);

            // 3. Oyun Sahnesine Geç (NetworkManager otomatik geçer ama biz şimdilik loglayalım)
            Debug.Log("Lobby Created & Host Started! Waiting for players...");
        }
        else
        {
            createLobbyButton.interactable = true;
        }
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
            // LobbyItem bileşenini bul ve veriyi doldur (Bir sonraki adımda yazacağız)
            LobbyItemUI itemScript = item.GetComponent<LobbyItemUI>();
            if (itemScript != null)
            {
                itemScript.Setup(lobby);
            }
        }
    }
}