using UnityEngine;

[System.Serializable]
public class SoundEntry
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}

public class SoundLibrary : MonoBehaviour
{
    public static SoundLibrary I { get; private set; }

    [Header("UI")]
    public SoundEntry uiSelect;

    [Header("Player")]
    public SoundEntry walk;
    public SoundEntry jump;
    public SoundEntry attack;
    public SoundEntry hurt;
    public SoundEntry death;
    public SoundEntry respawn;
    public SoundEntry morph;

    [Header("Flying")]
    public SoundEntry flying;

    [Header("Pickups")]
    public SoundEntry coin;
    public SoundEntry pickupPutdown;

    [Header("Music")]
    public SoundEntry mainTheme;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }
}