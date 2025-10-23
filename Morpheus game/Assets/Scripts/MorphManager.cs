using UnityEngine;

public class MorphManager : MonoBehaviour
{
    public GameObject defaultForm;
    public GameObject flyingForm;
    public GameObject powerForm;
    public GameObject agileForm;
    public CameraFollow cameraFollow; // reference to camera follow script

    private GameObject currentForm;
    private int lastFacingDir = 1; // 1 = right, -1 = left

    void Start()
    {
        // Spawn default form at start
        MorphTo(defaultForm);
    }

    void Update()
    {
        // Track last facing dir from input (NOT velocity)
        float inputX = Input.GetAxisRaw("Horizontal");
        if (inputX > 0.01f) lastFacingDir = 1;
        else if (inputX < -0.01f) lastFacingDir = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            MorphTo(defaultForm);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            MorphTo(agileForm);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            MorphTo(powerForm);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            MorphTo(flyingForm);
    }

    void MorphTo(GameObject formPrefab)
    {
        Vector3 spawnPos = transform.position;

        if (currentForm != null)
        {
            spawnPos = currentForm.transform.position;
            Destroy(currentForm);
        }

        // Spawn new form
        currentForm = Instantiate(formPrefab, spawnPos, Quaternion.identity);

        // Apply last facing direction even if idle
        Vector3 scale = currentForm.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (lastFacingDir >= 0 ? 1 : -1);
        currentForm.transform.localScale = scale;

        // If this is the flying form, pass direction
        var flying = currentForm.GetComponent<FlyingFormController>();
        if (flying != null)
        {
            flying.InitializeDirection(lastFacingDir);
        }

        // Update camera to follow new form
        if (cameraFollow != null)
        {
            cameraFollow.target = currentForm.transform;
        }
    }
}