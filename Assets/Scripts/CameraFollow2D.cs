using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector2 offset = new Vector2(2f, 1.5f);
    [SerializeField] float smoothTime = 0.2f;
    [SerializeField] float minY = 0f;

    Vector3 velocity;

    public void SetTarget(Transform t) => target = t;

    void LateUpdate()
    {
        if (target == null) return;
        var desired = new Vector3(target.position.x + offset.x, Mathf.Max(minY, target.position.y + offset.y), transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
