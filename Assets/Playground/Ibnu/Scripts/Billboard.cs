using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera targetCamera;

    void Start()
    {
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        transform.rotation = Quaternion.Euler(
            0f,
            targetCamera.transform.eulerAngles.y,
            0f
        );
    }
}