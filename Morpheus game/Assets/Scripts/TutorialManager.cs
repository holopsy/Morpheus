using UnityEngine;
using TMPro;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)] // Run BEFORE other scripts so the same key press reaches them too
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI (TMP)")]
    public GameObject promptPanel;   // assign your panel
    public TMP_Text   promptText;    // assign your TMP text

    [Header("Timing")]
    [Range(0.05f, 1f)] public float slowTimeScale = 0.2f;

    private bool   stepActive;
    private KeyCode waitingFor;
    private float  prevTimeScale = 1f;
    private float  prevFixedDelta = 0.02f;

    // Freeze/unfreeze RB constraints safely (we do NOT disable scripts anymore)
    private readonly List<Rigidbody2D> frozenRBs = new List<Rigidbody2D>();
    private readonly Dictionary<Rigidbody2D, RigidbodyConstraints2D> rbPrevConstraints =
        new Dictionary<Rigidbody2D, RigidbodyConstraints2D>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (promptPanel) promptPanel.SetActive(false);
    }

    void Update()
    {
        if (!stepActive) return;

        // If required key pressed this frame:
        if (Input.GetKeyDown(waitingFor))
        {
            // End step NOW so other scripts (which run AFTER us) see the same key press.
            CompleteStep();
            return;
        }

        // Otherwise block all other input for this frame (keeps only the required key meaningful)
        Input.ResetInputAxes();
    }

    public void StartStep(KeyCode requiredKey, string message)
    {
        if (stepActive) return;
        stepActive  = true;
        waitingFor = requiredKey;

        // Slow time but keep physics responsive
        prevTimeScale   = Time.timeScale;
        prevFixedDelta  = Time.fixedDeltaTime;
        Time.timeScale  = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // keep ~50 fixed steps per real second

        // Show prompt
        if (promptText)  promptText.text = message;
        if (promptPanel) promptPanel.SetActive(true);

        // Freeze all player rigidbodies so nothing moves while tip is active
        frozenRBs.Clear();
        rbPrevConstraints.Clear();
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            var rbs = p.GetComponentsInChildren<Rigidbody2D>(true);
            foreach (var rb in rbs)
            {
                if (!rbPrevConstraints.ContainsKey(rb))
                    rbPrevConstraints[rb] = rb.constraints;

                rb.constraints = RigidbodyConstraints2D.FreezeAll;
                frozenRBs.Add(rb);
            }
        }
    }

    void CompleteStep()
    {
        // Restore time/physics
        Time.timeScale      = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        // Unfreeze rigidbodies BEFORE other scripts run this frame
        foreach (var rb in frozenRBs)
        {
            if (rb && rbPrevConstraints.TryGetValue(rb, out var prev))
                rb.constraints = prev;
        }
        frozenRBs.Clear();
        rbPrevConstraints.Clear();

        // Hide UI
        if (promptPanel) promptPanel.SetActive(false);

        stepActive = false;
    }
}
