using UnityEngine;

[DisallowMultipleComponent]
public sealed class HuntzRigPilotCameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField, Min(0f)] float smoothTime = 0.12f;
    [SerializeField] Vector2 lookOffset = new Vector2(0f, 0.15f);

    Vector3 velocity;

    public void Configure(Transform followTarget)
    {
        target = followTarget;
        velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(target.position.x + lookOffset.x,
            target.position.y + lookOffset.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
