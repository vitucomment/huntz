using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public static readonly List<Coin> Active = new List<Coin>();

    Vector3 startPos;
    Vector3 baseScale;
    float phase;

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    void Start()
    {
        startPos = transform.position;
        baseScale = transform.localScale;
        phase = transform.position.x * 0.7f;
    }

    void Update()
    {
        // Bob up and down and fake a spin by squashing the X scale.
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * 3f + phase) * 0.15f;
        float spin = Mathf.Max(0.15f, Mathf.Abs(Mathf.Cos(Time.time * 2.5f + phase)));
        transform.localScale = new Vector3(baseScale.x * spin, baseScale.y, baseScale.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null) return;
        if (GameManager.Instance != null) GameManager.Instance.AddCoin();
        Destroy(gameObject);
    }
}
