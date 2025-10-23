using UnityEngine;
using TMPro;

public class InfoTextManager : MonoBehaviour
{
    public static InfoTextManager Instance;
    private TextMeshProUGUI infoText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        infoText = GetComponent<TextMeshProUGUI>();
        HideText();
    }

    public void ShowText(string message)
    {
        infoText.text = message;
        infoText.enabled = true;
    }

    public void HideText()
    {
        infoText.enabled = false;
    }
}