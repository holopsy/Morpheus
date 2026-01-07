using UnityEngine;

public class BossHeartsUI : MonoBehaviour
{
    [Header("Heart Sprites")]
    public Sprite fullHeart;
    public Sprite emptyHeart;

    [Header("Layout")]
    public int maxHearts = 5;
    public float spacing = 0.25f;

    [Header("Rendering")]
    public int sortingOrder = 999;

    private int currentHP;
    private SpriteRenderer[] hearts;

    private void LateUpdate()
    {
        // ✅ Prevent inheriting boss scale (boss is big, UI should stay normal)
        transform.localScale = Vector3.one;
    }

    public void SetMax(int hpMax)
    {
        maxHearts = Mathf.Max(1, hpMax);
        Build();
    }

    public void SetHP(int hp)
    {
        currentHP = Mathf.Clamp(hp, 0, maxHearts);
        Refresh();
    }

    private void Awake()
    {
        Build();
        Refresh();
    }

    private void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        hearts = new SpriteRenderer[maxHearts];

        float totalWidth = (maxHearts - 1) * spacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < maxHearts; i++)
        {
            GameObject h = new GameObject("Heart_" + i);
            h.transform.SetParent(transform, false);
            h.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
            h.transform.localScale = Vector3.one;

            var sr = h.AddComponent<SpriteRenderer>();
            sr.sprite = fullHeart;
            sr.sortingOrder = sortingOrder;
            hearts[i] = sr;
        }
    }

    private void Refresh()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (!hearts[i]) continue;
            hearts[i].sprite = (i < currentHP) ? fullHeart : emptyHeart;
        }
    }
}