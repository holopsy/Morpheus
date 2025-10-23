using UnityEngine;

public class BodySwitcher : MonoBehaviour
{
    public GameObject Body1Prefab;
    public GameObject Body2Prefab;

    private GameObject currentBody;

    void Start()
    {
        SwitchBody(1); //starts with body1
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchBody(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchBody(2);
        }
    }

    private void SwitchBody(int bodyNumber)
    {
        if (currentBody != null)
        {
            Destroy(currentBody);
        }

        if (bodyNumber == 1 && Body1Prefab != null)
        {
            currentBody = Instantiate(Body1Prefab, transform);
        }
        else if (bodyNumber == 2 && Body1Prefab != null)
        {
            currentBody = Instantiate(Body2Prefab, transform);
        }

        if (currentBody != null)
        {
            currentBody.transform.localPosition = Vector3.zero;
            currentBody.transform.localRotation = Quaternion.identity;
        }
    }
}
