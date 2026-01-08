using UnityEngine;

public class AudioTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            AudioManager.I.PlayMusic(SoundLibrary.I.mainTheme);
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            AudioManager.I.PlaySFX(SoundLibrary.I.attack);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            AudioManager.I.PlaySFX(SoundLibrary.I.uiSelect);
        }
    }
}