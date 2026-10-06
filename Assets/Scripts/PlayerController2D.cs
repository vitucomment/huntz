using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController2D : MonoBehaviour
{
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float jumpVelocity = 14f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [SerializeField] float riseGravity = 3f;
    [SerializeField] float fallGravity = 5f;
    [SerializeField] float fallLimitY = -12f;

    Rigidbody2D rb;
    BoxCollider2D col;
    Transform eyes;
    Vector3 spawnPoint;
    float lastGroundedTime = -1f;
    float lastJumpPressTime = -1f;
    readonly Collider2D[] groundHits = new Collider2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        eyes = transform.Find("Eyes");
        spawnPoint = transform.position;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float input = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;

        bool jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
        bool jumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
        if (jumpPressed) lastJumpPressTime = Time.time;
        if (IsGrounded()) lastGroundedTime = Time.time;

        Vector2 v = rb.linearVelocity;
        v.x = input * moveSpeed;

        // Coyote time + jump buffer make the jump feel forgiving.
        if (Time.time - lastJumpPressTime <= jumpBufferTime && Time.time - lastGroundedTime <= coyoteTime)
        {
            v.y = jumpVelocity;
            lastJumpPressTime = lastGroundedTime = -1f;
        }
        // Releasing jump early cuts the jump short.
        else if (!jumpHeld && v.y > jumpVelocity * 0.4f)
        {
            v.y = jumpVelocity * 0.4f;
        }

        rb.gravityScale = v.y < 0f ? fallGravity : riseGravity;
        rb.linearVelocity = v;

        if (eyes != null && input != 0f)
            eyes.localPosition = new Vector3(0.12f * Mathf.Sign(input), eyes.localPosition.y, 0f);

        if (transform.position.y < fallLimitY) Respawn();
    }

    bool IsGrounded()
    {
        Bounds b = col.bounds;
        var center = new Vector2(b.center.x, b.min.y - 0.05f);
        var size = new Vector2(b.size.x * 0.9f, 0.1f);
        int count = Physics2D.OverlapBox(center, size, 0f, ContactFilter2D.noFilter, groundHits);
        for (int i = 0; i < count; i++)
            if (groundHits[i] != col && !groundHits[i].isTrigger) return true;
        return false;
    }

    public void Respawn()
    {
        transform.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;
    }
}
