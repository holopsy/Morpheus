using UnityEngine;
using UnityEngine.UI;

public class MorphSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;      // child: Icon
    public Image lockImage; // child: Lock
    public Image glow;      // child: Glow (behind everything)

    public void SetState(bool isUnlocked, bool isActive, Sprite silhouette, Sprite fullIcon)
    {
        // If locked, show silhouette. If unlocked, show full icon.
        icon.sprite = isUnlocked ? fullIcon : silhouette;

        // Lock toggle
        lockImage.gameObject.SetActive(!isUnlocked);

        // Glow only if this form is both active AND unlocked
        glow.gameObject.SetActive(isActive && isUnlocked);
    }
}