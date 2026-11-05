using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private float delay = 0.5f;

    private bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;

        // Пока не собраны все кристаллы, портал закрыт
        if (GameManager.Instance != null && !GameManager.Instance.AllCollected)
        {
            Debug.Log("Портал закрыт: соберите все кристаллы");
            return;
        }

        used = true;
        Invoke(nameof(LoadNext), delay);
    }

    void LoadNext()
    {
        GameManager.Instance.LoadNextLevel();
    }
}