using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class HuntzArpg3DCameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(12f, 15f, -12f);
    [SerializeField, Min(0f)] float followSharpness = 8f;
    [SerializeField, Min(1f)] float orthographicSize = 10f;
    [SerializeField, Min(1f)] float minimumZoom = 7f;
    [SerializeField, Min(1f)] float maximumZoom = 15f;
    [SerializeField, Min(0f)] float zoomPerScrollUnit = 0.01f;
    [SerializeField, Min(0f)] float zoomSharpness = 14f;
    [SerializeField] Vector3 lookOffset = new Vector3(0f, 0.8f, 0f);

    Camera cameraComponent;
    InputAction zoomAction;
    float targetZoom;

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        cameraComponent.orthographic = true;
        targetZoom = Mathf.Clamp(orthographicSize, minimumZoom, maximumZoom);
        cameraComponent.orthographicSize = targetZoom;
        cameraComponent.nearClipPlane = 0.1f;
        cameraComponent.farClipPlane = 120f;

        zoomAction = new InputAction("Camera Zoom", InputActionType.Value, expectedControlType: "Axis");
        zoomAction.AddBinding("<Mouse>/scroll/y");
        zoomAction.Enable();
    }

    void OnDestroy()
    {
        zoomAction?.Dispose();
    }

    public void SetTarget(Transform followTarget, bool snap)
    {
        target = followTarget;
        if (snap && target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(target.position + lookOffset - transform.position, Vector3.up);
        }
    }

    void LateUpdate()
    {
        float scroll = zoomAction != null ? zoomAction.ReadValue<float>() : 0f;
        if (Mathf.Abs(scroll) > 0.01f)
            targetZoom = Mathf.Clamp(targetZoom - scroll * zoomPerScrollUnit, minimumZoom, maximumZoom);

        float zoomBlend = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);
        cameraComponent.orthographicSize = Mathf.Lerp(cameraComponent.orthographicSize, targetZoom, zoomBlend);

        if (target == null) return;
        Vector3 desiredPosition = target.position + offset;
        float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
        transform.rotation = Quaternion.LookRotation(target.position + lookOffset - transform.position, Vector3.up);
    }
}
