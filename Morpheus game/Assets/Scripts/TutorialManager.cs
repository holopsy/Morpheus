using UnityEngine;
using TMPro;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)]
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI (TMP)")]
    public GameObject promptPanel;
    public TMP_Text promptText;

    [Header("Timing")]
    [Range(0.05f, 1f)] public float slowTimeScale = 0.2f;

    private bool stepActive;
    private KeyCode waitingFor;
    private KeyCode[] waitingForMultiple;
    private bool multiMode;

    private float prevTimeScale = 1f;
    private float prevFixedDelta = 0.02f;

    private readonly List<Rigidbody2D> frozenRBs = new List<Rigidbody2D>();
    private readonly Dictionary<Rigidbody2D, RigidbodyConstraints2D> rbPrevConstraints =
        new Dictionary<Rigidbody2D, RigidbodyConstraints2D>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (promptPanel) promptPanel.SetActive(false);
    }

    void Update()
    {
        if (!stepActive) return;

        if (multiMode)
        {
            foreach (var key in waitingForMultiple)
            {
                if (Input.GetKeyDown(key))
                {
                    CompleteStep();
                    return;
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(waitingFor))
            {
                CompleteStep();
                return;
            }
        }

        Input.ResetInputAxes();
    }

    // === Single-key trigger (no custom layout) ===
    public void StartStep(KeyCode requiredKey, string message)
    {
        StartStep(requiredKey, message, Vector2.zero, 1f, 36f);
    }

    // === Single-key trigger (with layout) ===
    public void StartStep(KeyCode requiredKey, string message, Vector2 offset, float scale, float baseSize)
    {
        if (stepActive) return;
        multiMode = false;
        waitingFor = requiredKey;
        StartStepCommon(message, offset, scale, baseSize);
    }

    // === Multi-key trigger (no custom layout) ===
    public void StartStep(KeyCode[] acceptedKeys, string message)
    {
        StartStep(acceptedKeys, message, Vector2.zero, 1f, 36f);
    }

    // === Multi-key trigger (with layout) ===
    public void StartStep(KeyCode[] acceptedKeys, string message, Vector2 offset, float scale, float baseSize)
    {
        if (stepActive) return;
        multiMode = true;
        waitingForMultiple = acceptedKeys;
        StartStepCommon(message, offset, scale, baseSize);
    }

    // === Shared setup logic for all versions ===
    // === Shared setup logic for all versions ===
    private void StartStepCommon(string message, Vector2 offset, float scale, float baseSize)
    {
        stepActive = true;

        // slow motion setup
        prevTimeScale = Time.timeScale;
        prevFixedDelta = Time.fixedDeltaTime;
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // setup text layout and content
        if (promptText)
        {
            // 1) Set text
            promptText.text = message;

            // 2) Make sure TMP obeys font size
            // (Auto Size will override fontSize—turn it OFF)
            var tmp = promptText; // TMP_Text
            tmp.enableAutoSizing = false;
            tmp.fontSize = baseSize * scale;

            // 3) Position: force center anchors so anchoredPosition works
            var rt = promptText.rectTransform; // RectTransform on the TMP object
            if (rt)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = offset; // per-trigger position
            }
        }

        // show the panel
        if (promptPanel) promptPanel.SetActive(true);

        // freeze all player rigidbodies (unchanged)
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

    private void CompleteStep()
    {
        // restore time
        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        // unfreeze rigidbodies
        foreach (var rb in frozenRBs)
        {
            if (rb && rbPrevConstraints.TryGetValue(rb, out var prev))
                rb.constraints = prev;
        }
        frozenRBs.Clear();
        rbPrevConstraints.Clear();

        // hide UI
        if (promptPanel) promptPanel.SetActive(false);

        stepActive = false;
    }
}
