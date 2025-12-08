using UnityEngine;
using UnityEngine.UI;

public class MorphWheelUI : MonoBehaviour
{
    [Header("References")]
    public MorphManager morphManager;

    [Header("Slots")]
    public MorphSlotUI defaultSlot;   // MorphWheel
    public MorphSlotUI agileSlot;     // Slot_Agile
    public MorphSlotUI powerSlot;     // Slot_Power
    public MorphSlotUI flyingSlot;    // Slot_Flying

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

    void Start()
    {
        RefreshAll();
    }

    void Update()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        // ---------- DEFAULT ----------
        defaultSlot.SetState(
            isUnlocked: morphManager.defaultUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.defaultForm,
            silhouette: defaultSilhouette,
            fullIcon: defaultFull
        );

        // ---------- AGILE ----------
        agileSlot.SetState(
            isUnlocked: morphManager.agileUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.agileForm,
            silhouette: agileSilhouette,
            fullIcon: agileFull
        );

        // ---------- POWER ----------
        powerSlot.SetState(
            isUnlocked: morphManager.powerUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.powerForm,
            silhouette: powerSilhouette,
            fullIcon: powerFull
        );

        // ---------- FLYING ----------
        flyingSlot.SetState(
            isUnlocked: morphManager.flyingUnlocked,
            isActive: morphManager.currentFormPrefab == morphManager.flyingForm,
            silhouette: flyingSilhouette,
            fullIcon: flyingFull
        );
    }
}
