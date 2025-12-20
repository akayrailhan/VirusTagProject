using UnityEngine;
using TMPro; // TextMeshPro için gerekli
using UnityEngine.UI;

public class BootstrapUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public Button connectButton;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        // Varsa eski ismi yükle
        nameInputField.text = ApplicationController.Instance.GetPlayerName();

        // Butona tıklanınca Connect fonksiyonunu çağır
        connectButton.onClick.AddListener(OnConnectClicked);
    }

    private async void OnConnectClicked()
    {
        string playerName = nameInputField.text;

        // İsim boş olamaz (Quest 5 kuralı)
        if (string.IsNullOrWhiteSpace(playerName))
        {
            statusText.text = "Name cannot be empty!";
            statusText.color = Color.red;
            return;
        }

        connectButton.interactable = false;
        statusText.text = "Connecting to Unity Services...";
        statusText.color = Color.yellow;

        // 1. İsmi Kaydet
        ApplicationController.Instance.SavePlayerName(playerName);

        // 2. Authentication Yap
        bool success = await AuthenticationWrapper.LoginAnonymously();

        if (success)
        {
            statusText.text = "Success! Loading Lobby...";
            statusText.color = Color.green;

            // 3. Lobby Sahnesine Geç (Biraz bekleyip)
            await System.Threading.Tasks.Task.Delay(1000);
            ApplicationController.Instance.GoToLobbyMenu();
        }
        else
        {
            statusText.text = "Connection Failed. Try again.";
            statusText.color = Color.red;
            connectButton.interactable = true;
        }
    }
}