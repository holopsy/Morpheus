using UnityEngine;

public class VisualReactivator : MonoBehaviour
{
    [Tooltip("Name of the visual child that holds the SpriteRenderer + Animator")]
    public string visualChildName = "Visual";

    void OnEnable()
    {
        // Find the visual child and re-enable it
        var visual = transform.Find(visualChildName);
        if (visual != null)
        {
            visual.gameObject.SetActive(true);

            // Make sure all SpriteRenderers are visible
            foreach (var sr in visual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.enabled = true;
                var c = sr.color;
                if (c.a < 1f) sr.color = new Color(c.r, c.g, c.b, 1f);
            }

            // Wake up Animator
            var anim = visual.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                anim.speed = 1f;
                anim.Rebind();     // reset to default state
                anim.Update(0f);   // force refresh
            }
        }

        // Retarget the camera if it lost its target
        var cam = FindObjectOfType<CameraFollow>();
        if (cam != null && cam.target == null)
            cam.target = transform;
    }
}