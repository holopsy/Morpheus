using UnityEngine;

public class FlyingFormAnimationEvents : MonoBehaviour
{
    public FlyingFormController controller;

    public void OnSpawnComplete()
    {
        if (controller != null)
            controller.OnSpawnComplete();
    }
}