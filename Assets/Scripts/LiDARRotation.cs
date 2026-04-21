using UnityEngine;

public class LiDARRotation : MonoBehaviour
{
    public float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
    }
}