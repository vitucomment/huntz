using UnityEngine;

public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() != null && GameManager.Instance != null)
            GameManager.Instance.ReachGoal();
    }
}
