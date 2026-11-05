using UnityEngine;

public class Crystal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.AddCrystal();
            Destroy(gameObject);
        }
    }
}