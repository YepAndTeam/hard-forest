using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Поставь 180, если картинка снаряда смотрит влево")]
    [SerializeField] private float spriteAngleOffset = 0f;

    private float damage;

    public void Init(Vector2 direction, float dmg)
    {
        damage = dmg;
        direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & groundLayer) != 0)
            Destroy(gameObject);
    }
}