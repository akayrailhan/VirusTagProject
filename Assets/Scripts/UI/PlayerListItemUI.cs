using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerListItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Text legacyText;
    public Image readyIndicator; // green when ready, gray when not
    public TextMeshProUGUI readyText;
    public Sprite readySprite;
    public Sprite notReadySprite;
    // If true, tint the indicator and text colors based on ready state. Default false to avoid unexpected color changes.
    public bool useColorTint = false;

    // Setup with player name and optional ready state
    public void Setup(string playerName, bool ready = false)
    {
        if (nameText != null)
        {
            nameText.text = playerName;
        }
        else if (legacyText != null)
        {
            legacyText.text = playerName;
        }

        if (readyIndicator != null)
        {
            if (readySprite != null && notReadySprite != null)
            {
                readyIndicator.sprite = ready ? readySprite : notReadySprite;
                readyIndicator.color = ready ? Color.green : Color.red;
            }
        }
    }
}
