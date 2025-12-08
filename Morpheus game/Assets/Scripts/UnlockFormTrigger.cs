using UnityEngine;

public class UnlockFormTrigger : MonoBehaviour
{
    public MorphManager.MorphType formToUnlock;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        var mm = FindObjectOfType<MorphManager>();
        if (mm != null)
        {
            mm.UnlockForm(formToUnlock);
        }

        Destroy(gameObject); // one-time unlock
    }
}