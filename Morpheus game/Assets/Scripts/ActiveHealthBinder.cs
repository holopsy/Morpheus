using UnityEngine;

public class ActiveHealthBinder : MonoBehaviour
{
    void OnEnable()
    {
        var ph = GetComponent<PlayerHealth>();
        var ui = FindObjectOfType<HeartUI>(true); // only here; OK to keep
        if (ph != null && ui != null)
            ui.SetPlayerHealth(ph);
    }
}