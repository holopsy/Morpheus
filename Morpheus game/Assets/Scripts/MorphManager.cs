using System;
using UnityEngine;

public class MorphManager : MonoBehaviour
{
    [Header("Forms")]
    public GameObject defaultForm;
    public GameObject flyingForm;
    public GameObject powerForm;
    public GameObject agileForm;

    [Header("References")]
    public CameraFollow cameraFollow; // reference to camera follow script

    private GameObject currentForm;
    private GameObject currentFormPrefab; // track which prefab is active
    private int lastFacingDir = 1; // 1 = right, -1 = left
    private FormHealthMemory healthMemory; // remembers HP for each form

    void Awake()
    {
        // Ensure we have a memory component available
        healthMemory = GetComponent<FormHealthMemory>();
        if (!healthMemory)
            healthMemory = gameObject.AddComponent<FormHealthMemory>();
    }

    void Start()
    {
        MorphTo(defaultForm);
    }

    void Update()
    {
        // Track last facing dir from input (NOT velocity)
        float inputX = Input.GetAxisRaw("Horizontal");
        if (inputX > 0.01f) lastFacingDir = 1;
        else if (inputX < -0.01f) lastFacingDir = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) MorphTo(defaultForm);
        if (Input.GetKeyDown(KeyCode.Alpha2)) MorphTo(agileForm);
        if (Input.GetKeyDown(KeyCode.Alpha3)) MorphTo(powerForm);
        if (Input.GetKeyDown(KeyCode.Alpha4)) MorphTo(flyingForm);
    }

    // ---------------- MORPHING ----------------
    public void MorphTo(GameObject formPrefab)
    {
        if (formPrefab == null) return;
        if (currentForm != null && currentFormPrefab == formPrefab) return;

        Vector3 spawnPos = currentForm ? currentForm.transform.position : transform.position;

        // Save outgoing form HP
        if (currentForm != null && currentFormPrefab != null)
        {
            var hp = currentForm.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                string key = GetFormKey(currentFormPrefab);
                healthMemory.Save(key, hp.current, hp.maxHealth);
            }
        }

        if (currentForm) Destroy(currentForm);

        // Do NOT refill HP on regular morphs
        SpawnFormInternal(formPrefab, spawnPos, playSpawnAnim: true, refillHealth: false);
    }

    // ---------------- RESPAWN (from checkpoints) ----------------
    public void ForceRespawn(GameObject formPrefab, Vector3 position, bool refillHealth)
    {
        // ✅ Clear all stored HP so every form resets after death
        if (healthMemory != null)
            healthMemory.ClearAll();

        if (currentForm) Destroy(currentForm);
        SpawnFormInternal(formPrefab, position, playSpawnAnim: true, refillHealth: refillHealth);
    }

    // ---------------- INTERNAL SPAWNER ----------------
    void SpawnFormInternal(GameObject formPrefab, Vector3 position, bool playSpawnAnim, bool refillHealth)
    {
        position.z = 0f;

        currentForm = Instantiate(formPrefab, position, Quaternion.identity);
        currentFormPrefab = formPrefab;

        TryInitializeFacing(currentForm, lastFacingDir);

        // Flying form special handling
        var flying = currentForm.GetComponent<FlyingFormController>();
        if (flying != null) flying.InitializeDirection(lastFacingDir);

        if (cameraFollow != null)
            cameraFollow.target = currentForm.transform;

        // Subscribe to death event BEFORE anything else
        var ph = currentForm.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.OnDeath += () =>
            {
                if (currentFormPrefab != null && healthMemory != null)
                {
                    string key = GetFormKey(currentFormPrefab);
                    healthMemory.Save(key, Mathf.Max(0, ph.current), ph.maxHealth);
                }
            };
        }

        // Restore remembered health or set defaults
        ApplyOrRestoreHealth(currentForm, refillHealth);

        // Ensure visuals are visible & animators are active
        PostSpawnSanity(currentForm, playSpawnAnim);
    }

    // ---------------- HEALTH HANDLING ----------------
    void ApplyOrRestoreHealth(GameObject form, bool refill)
    {
        var hp = form.GetComponent<PlayerHealth>();
        if (!hp) return;

        string key = GetFormKey(currentFormPrefab);

        if (healthMemory != null && healthMemory.TryLoad(key, out var state))
        {
            hp.maxHealth = Mathf.Max(1, state.max);
            hp.current = Mathf.Clamp(refill ? hp.maxHealth : state.current, 0, hp.maxHealth);
            hp.OnHealthChanged?.Invoke(hp.current, hp.maxHealth);
            return;
        }

        // Defaults for new form
        bool isFlying = form.GetComponent<FlyingFormController>() != null;
        hp.maxHealth = isFlying ? 1 : 3;
        hp.current = hp.maxHealth;
        hp.OnHealthChanged?.Invoke(hp.current, hp.maxHealth);
    }

    // ---------------- POST SPAWN CLEANUP ----------------
    void PostSpawnSanity(GameObject form, bool playSpawnAnim)
    {
        // Enable all visuals
        var srs = form.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.enabled = true;
            var c = sr.color;
            if (c.a < 1f) sr.color = new Color(c.r, c.g, c.b, 1f);
            sr.gameObject.SetActive(true);
        }

        // Enable all animators
        var anims = form.GetComponentsInChildren<Animator>(true);
        foreach (var a in anims)
        {
            if (!a.enabled) a.enabled = true;
            if (a.speed == 0f) a.speed = 1f;
            TryResetTrigger(a, "Die");
            TrySetBool(a, "IsDead", false);
            if (playSpawnAnim) TrySetTrigger(a, "Spawn");
        }

        // Enable movement controllers
        var def = form.GetComponent<DefaultMovement>(); if (def) def.enabled = true;
        var agile = form.GetComponent<AgileFormController>(); if (agile) agile.enabled = true;
        var fly = form.GetComponent<FlyingFormController>(); if (fly) fly.enabled = true;
        var power = form.GetComponent<PowerFormController>(); if (power) power.enabled = true;
    }

    // ---------------- UTILITIES ----------------
    string GetFormKey(GameObject prefab)
    {
        if (prefab == flyingForm) return "Flying";
        if (prefab == powerForm) return "Power";
        if (prefab == agileForm) return "Agile";
        if (prefab == defaultForm) return "Default";
        return prefab ? prefab.name : "UnknownForm";
    }

    void TryInitializeFacing(GameObject form, int dir)
    {
        var def = form.GetComponent<DefaultMovement>();
        if (def != null) { def.InitializeFacing(dir); return; }

        var agile = form.GetComponent<AgileFormController>();
        if (agile != null)
        {
            var m = typeof(AgileFormController).GetMethod("InitializeFacing");
            if (m != null) m.Invoke(agile, new object[] { dir });
            return;
        }
    }

    // Animator helpers
    void TrySetTrigger(Animator a, string name)
    {
        if (!a) return;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
            { a.SetTrigger(name); return; }
    }
    void TryResetTrigger(Animator a, string name)
    {
        if (!a) return;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
            { a.ResetTrigger(name); return; }
    }
    void TrySetBool(Animator a, string name, bool val)
    {
        if (!a) return;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
            { a.SetBool(name, val); return; }
    }

    // Manual fallback (used by PlayerHealth if needed)
    public void SaveCurrentFormHealthOnDeath(PlayerHealth hp)
    {
        if (currentForm == null || currentFormPrefab == null || hp == null || healthMemory == null)
            return;

        string key = GetFormKey(currentFormPrefab);
        healthMemory.Save(key, Mathf.Max(0, hp.current), hp.maxHealth);
    }
}
