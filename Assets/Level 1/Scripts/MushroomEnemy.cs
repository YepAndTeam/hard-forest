using UnityEngine;

public class MushroomEnemy : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private Transform groundCheck; // впереди у ног
    [SerializeField] private Transform wallCheck;   // впереди на уровне тела
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float checkDistance = 0.3f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Атака")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float damage = 10;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private int dir = 1;
    private bool isAttacking;
    private float nextAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        ApplyFacing();
    }

    void Update()
    {
        if (isAttacking || Time.time < nextAttackTime) return;

        if (Physics2D.OverlapCircle(transform.position, attackRange, playerLayer))
        {
            isAttacking = true;
nextAttackTime = Time.time + attackCooldown;
anim.SetTrigger("Attack");
Invoke(nameof(EndAttack), 1f);
        }
    }

    void FixedUpdate()
    {
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            anim.SetFloat("Speed", 0);
            return;
        }

        bool groundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, groundLayer);
        bool wallAhead = Physics2D.Raycast(wallCheck.position, Vector2.right * dir, checkDistance, groundLayer);
        
        if (!groundAhead || wallAhead)
        {
            dir *= -1;
            ApplyFacing();
        }

        rb.linearVelocity = new Vector2(speed * dir, rb.linearVelocity.y);
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    void ApplyFacing()
    {
        Vector3 s = transform.localScale;
        float sign = spriteFacesRight ? dir : -dir;
        s.x = Mathf.Abs(s.x) * sign;
        transform.localScale = s;
    }

    // Animation Event в кадре удара
    public void DealDamage()
{
    Debug.Log("DealDamage вызван");

    Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
    Debug.Log("Нашёл игрока: " + (hit != null ? hit.name : "НЕТ"));

    if (hit != null)
    {
        var hp = hit.GetComponentInParent<PlayerHealth>();
        Debug.Log("PlayerHealth: " + (hp != null ? "есть" : "НЕТ"));
        if (hp != null) hp.TakeDamage(damage);
    }
}

    // Animation Event в последнем кадре атаки
    public void EndAttack()
    {
        isAttacking = false;
    }
    public void Interrupt()
{
    isAttacking = false;
}

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}