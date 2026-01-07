using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FinishPopupUI popup;

    [Header("Input")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Text")]
    [SerializeField] private string title = "PAUSED";
    [TextArea(2, 4)]
    [SerializeField] private string body = "Game is paused.";

    private bool isPaused;

    private void Awake()
    {
        if (!popup)
            popup = FindFirstObjectByType<FinishPopupUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        if (!popup) return;

        isPaused = true;

        // Show as "NotEnough" so NEXT stays hidden, and Resume just closes.
        popup.Show(FinishPopupUI.ResultType.NotEnough, 0, 0, 0);

        // Override the texts for pause mode
        // (Assumes your FinishPopupUI uses titleText/bodyText internally; we expose a helper below.)
        popup.SetCustomText(title, body);
    }

    private void Resume()
    {
        isPaused = false;
        if (popup) popup.Close();
    }
}