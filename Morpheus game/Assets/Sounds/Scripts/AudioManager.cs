using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource footstepSource;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        if (!sfxSource || !musicSource || !footstepSource)
        {
            Debug.LogError("AudioManager: One or more AudioSources not assigned!");
            return;
        }

        sfxSource.playOnAwake = false;
        musicSource.playOnAwake = false;

        footstepSource.playOnAwake = false;
        footstepSource.loop = true;
    }

    public void PlaySFX(SoundEntry entry)
    {
        if (entry == null || entry.clip == null) return;
        sfxSource.volume = entry.volume;
        sfxSource.PlayOneShot(entry.clip);
    }

    public void StartFootsteps(SoundEntry entry)
    {
        if (entry == null || entry.clip == null) return;
        if (footstepSource.isPlaying) return;

        footstepSource.clip = entry.clip;
        footstepSource.volume = entry.volume;
        footstepSource.Play();
    }

    public void StopFootsteps()
    {
        if (footstepSource.isPlaying)
            footstepSource.Stop();
    }

    public void StopAllLooping()
    {
        StopFootsteps();
    }

    public void PlayMusic(SoundEntry entry)
    {
        if (entry == null || entry.clip == null) return;

        if (musicSource.clip == entry.clip && musicSource.isPlaying)
            return;

        musicSource.clip = entry.clip;
        musicSource.loop = true;
        musicSource.volume = entry.volume;
        musicSource.Play();
    }
}