using UnityEngine;

public class MusicStarter : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.I != null && SoundLibrary.I != null)
        {
            AudioManager.I.PlayMusic(SoundLibrary.I.mainTheme);
        }
    }
}