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

    [Header("Defaults")]
    [Range(0.05f, 1f)] public float defaultSlowTimeScale = 0.2f;

    private bool stepActive;      // key-required mode only
    private bool showOnlyActive;  // show-only mode
    private KeyCode waitingFor;
    private KeyCode[] waitingForMultiple;
    private bool multiMode;

    private float prevTimeScale = 1f;
    private float prevFixedDelta = 0.02f;

    private readonly List<Rigidbody2D> frozenRBs = new List<Rigidbody2D>();
    private readonly Dictionary<Rigidbody2D, RigidbodyConstraints2D> rbPrevConstraints =
        new Dictionary<Rigidbody2D, RigidbodyConstraints2D>();

    public enum TimeEffect { None, Slow, Freeze }

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
        // ✅ Only block input / listen for keys in key-required mode
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

        // Prevent buffered input while a modal tutorial is active
        Input.ResetInputAxes();
    }

    // ---------------- SHOW-ONLY MODE (no key, no input blocking) ----------------
    public void ShowOnly(string message, Vector2 offset, float scale, float baseSize,
                         TimeEffect timeEffect, float slowScaleOverride = -1f)
    {
        if (stepActive || showOnlyActive) return;
        showOnlyActive = true;

        // Save time state
        prevTimeScale = Time.timeScale;
        prevFixedDelta = Time.fixedDeltaTime;

        ApplyTimeEffect(timeEffect, slowScaleOverride);

        SetupPrompt(message, offset, scale, baseSize);
        if (promptPanel) promptPanel.SetActive(true);
    }

    // ---------------- KEY-REQUIRED MODE ----------------
    public void StartStep(KeyCode requiredKey, string message, Vector2 offset, float scale, float baseSize,
                          TimeEffect timeEffect, float slowScaleOverride = -1f)
    {
        if (stepActive || showOnlyActive) return;
        multiMode = false;
        waitingFor = requiredKey;

        stepActive = true;

        prevTimeScale = Time.timeScale;
        prevFixedDelta = Time.fixedDeltaTime;

        ApplyTimeEffect(timeEffect, slowScaleOverride);

        SetupPrompt(message, offset, scale, baseSize);
        if (promptPanel) promptPanel.SetActive(true);
    }

    public void StartStep(KeyCode[] acceptedKeys, string message, Vector2 offset, float scale, float baseSize,
                          TimeEffect timeEffect, float slowScaleOverride = -1f)
    {
        if (stepActive || showOnlyActive) return;
        multiMode = true;
        waitingForMultiple = acceptedKeys;

        stepActive = true;

        prevTimeScale = Time.timeScale;
        prevFixedDelta = Time.fixedDeltaTime;

        ApplyTimeEffect(timeEffect, slowScaleOverride);

        SetupPrompt(message, offset, scale, baseSize);
        if (promptPanel) promptPanel.SetActive(true);
    }

    void SetupPrompt(string message, Vector2 offset, float scale, float baseSize)
    {
        if (!promptText) return;

        promptText.text = message;
        promptText.enableAutoSizing = false;
        promptText.fontSize = baseSize * scale;

        var rt = promptText.rectTransform;
        if (rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
        }
    }

    private void ApplyTimeEffect(TimeEffect mode, float slowScaleOverride)
    {
        frozenRBs.Clear();
        rbPrevConstraints.Clear();

        if (mode == TimeEffect.None)
        {
            return;
        }

        if (mode == TimeEffect.Slow)
        {
            float slow = (slowScaleOverride > 0f) ? slowScaleOverride : defaultSlowTimeScale;
            Time.timeScale = slow;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            return;
        }

        // Freeze mode
        Time.timeScale = 0f;

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
        EndStep();
    }

    // ✅ Works for BOTH modes (show-only + key-required)
    public void EndStep()
    {
        if (!stepActive && !showOnlyActive) return;

        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        foreach (var rb in frozenRBs)
        {
            if (rb && rbPrevConstraints.TryGetValue(rb, out var prev))
                rb.constraints = prev;
        }
        frozenRBs.Clear();
        rbPrevConstraints.Clear();

        if (promptPanel) promptPanel.SetActive(false);

        stepActive = false;
        showOnlyActive = false;
        multiMode = false;
        waitingForMultiple = null;
    }
}
