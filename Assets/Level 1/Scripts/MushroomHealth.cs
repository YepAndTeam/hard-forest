using UnityEngine;
using UnityEngine.UI;

public class MushroomHealth : MonoBehaviour, IDamageable
{
    [Header("Здоровье")]
    [SerializeField] private float maxHp = 20;
    [SerializeField] private float destroyDelay = 1.5f;

    [Header("Полоска над головой")]
    [SerializeField] private Image fill;          // красный fill
    [SerializeField] private GameObject barRoot;  // HealthCanvas
    [SerializeField] private bool hideBarUntilHit = true;

    [Header("Событие при смерти")]
    [SerializeField] private Gate gateToOpen;

    private float hp;
    private bool isDead;
    private Animator anim;
    private Rigidbody2D rb;
    private MushroomEnemy enemy;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<MushroomEnemy>();
        hp = maxHp;

        UpdateBar();
        if (barRoot != null && hideBarUntilHit)
            barRoot.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        hp = Mathf.Max(0, hp - damage);
        UpdateBar();

        if (barRoot != null)
            barRoot.SetActive(true);

        if (hp <= 0)
        {
            Die();
            return;
        }

        if (enemy != null) enemy.Interrupt(); // сбрасываем атаку, если ударили в замахе
        anim.SetTrigger("Hit");
    }

    void Die()
    {
        isDead = true;

        if (enemy != null) enemy.enabled = false; // больше не ходит и не бьёт
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("Die");

        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;
        rb.simulated = false;

        if (barRoot != null)
            barRoot.SetActive(false);

        if (gateToOpen != null) gateToOpen.Open();
        Destroy(gameObject, destroyDelay);
    }

    void UpdateBar()
    {
        if (fill != null)
            fill.fillAmount = hp / maxHp;
    }
}