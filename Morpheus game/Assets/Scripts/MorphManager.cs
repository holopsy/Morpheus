using UnityEngine;

public class MorphManager : MonoBehaviour
{
    public GameObject defaultForm;
    public GameObject flyingForm;
    public GameObject powerForm;
    public GameObject agileForm;
    public CameraFollow cameraFollow; // reference to camera follow script

    private GameObject currentForm;
    private GameObject currentFormPrefab; // track which prefab is active
    private int lastFacingDir = 1; // 1 = right, -1 = left

    void Start()
    {
        MorphTo(defaultForm);
    }

    void Update()
    {
        // Track last facing dir from input (NOT velocity)
        float inputX = Input.GetAxisRaw("Horizontal");
        if (inputX > 0.01f) lastFacingDir = 1;
        else if (inputX < -0.01f) lastFacingDir = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) MorphTo(defaultForm);
        if (Input.GetKeyDown(KeyCode.Alpha2)) MorphTo(agileForm);
        if (Input.GetKeyDown(KeyCode.Alpha3)) MorphTo(powerForm);
        if (Input.GetKeyDown(KeyCode.Alpha4)) MorphTo(flyingForm);
    }

    void MorphTo(GameObject formPrefab)
    {
        if (formPrefab == null) return;

        // ✋ Ignore morphs to the same form (prevents spam respawn)
        if (currentForm != null && currentFormPrefab == formPrefab) return;

        Vector3 spawnPos = transform.position;
        if (currentForm != null)
        {
            spawnPos = currentForm.transform.position;
            Destroy(currentForm);
        }

        // Spawn new form
        currentForm = Instantiate(formPrefab, spawnPos, Quaternion.identity);
        currentFormPrefab = formPrefab;

        // ✅ Initialize facing on the new form (no root flipping!)
        TryInitializeFacing(currentForm, lastFacingDir);

        // Flying form still needs direction info
        var flying = currentForm.GetComponent<FlyingFormController>();
        if (flying != null) flying.InitializeDirection(lastFacingDir);

        // Camera follow
        if (cameraFollow != null) cameraFollow.target = currentForm.transform;
    }

    // Tries to pass facing to whatever controller the form uses
    void TryInitializeFacing(GameObject form, int dir)
    {
        // Default form
        var def = form.GetComponent<DefaultMovement>();
        if (def != null) { def.InitializeFacing(dir); return; }

        // Agile form
        var agile = form.GetComponent<AgileFormController>();
        if (agile != null)
        {
            // Add a method InitializeFacing(int) to Agile if you want consistent spawn facing
            var m = typeof(AgileFormController).GetMethod("InitializeFacing");
            if (m != null) m.Invoke(agile, new object[] { dir });
            return;
        }

        // Power form (if you have a controller and want facing on spawn, add the same method)
        var power = form.GetComponent<MonoBehaviour>(); // placeholder if you make Power controller later
        // Extend similarly if needed.
    }
}
