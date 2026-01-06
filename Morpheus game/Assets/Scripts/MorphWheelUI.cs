using UnityEngine;

public class MorphWheelUI : MonoBehaviour
{
    [Header("References")]
    public MorphManager morphManager;

    [Header("Slots")]
    public MorphSlotUI defaultSlot;
    public MorphSlotUI agileSlot;
    public MorphSlotUI powerSlot;
    public MorphSlotUI flyingSlot;

    [Header("Sprites - Silhouettes")]
    public Sprite defaultSilhouette;
    public Sprite agileSilhouette;
    public Sprite powerSilhouette;
    public Sprite flyingSilhouette;

    [Header("Sprites - Full Icons")]
    public Sprite defaultFull;
    public Sprite agileFull;
    public Sprite powerFull;
    public Sprite flyingFull;

    private void Awake()
    {
        // Auto-find in the scene if not assigned
        if (!morphManager)
            morphManager = FindFirstObjectByType<MorphManager>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (!morphManager) return;

        morphManager.OnUnlockStateChanged += RefreshAll;
        morphManager.OnFormChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (!morphManager) return;

        morphManager.OnUnlockStateChanged -= RefreshAll;
        morphManager.OnFormChanged -= RefreshAll;
    }

    public void RefreshAll()
    {
        if (!morphManager) return;

        defaultSlot.SetState(
            isUnlocked: morphManager.defaultUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.defaultForm,
            silhouette: defaultSilhouette,
            fullIcon: defaultFull
        );

        agileSlot.SetState(
            isUnlocked: morphManager.agileUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.agileForm,
            silhouette: agileSilhouette,
            fullIcon: agileFull
        );

        powerSlot.SetState(
            isUnlocked: morphManager.powerUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.powerForm,
            silhouette: powerSilhouette,
            fullIcon: powerFull
        );

        flyingSlot.SetState(
            isUnlocked: morphManager.flyingUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.flyingForm,
            silhouette: flyingSilhouette,
            fullIcon: flyingFull
        );
    }
}
