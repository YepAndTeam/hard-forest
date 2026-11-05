using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHp = 100;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private float restartDelay = 2f;

    [Header("Получение урона")]
    [SerializeField] private float invulnerableTime = 0.8f;  // неуязвимость после удара (защита от стан-лока)
    [SerializeField] private float blinkInterval = 0.08f;    // мигание спрайта при неуязвимости

    private float hp;
    private bool isDead;
    private float invulnerableUntil;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerController controller;

    public bool IsDead => isDead;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        hp = maxHp;
        healthBar.SetHealth(hp, maxHp);
    }

    // Возвращает true, если урон реально прошёл
    public bool TakeDamage(float damage)
    {
        if (isDead || IsInvulnerable) return false;

        hp = Mathf.Max(0, hp - damage);
        healthBar.SetHealth(hp, maxHp);

        if (hp <= 0)
        {
            Die();
            return true;
        }

        // анимация получения урона + оглушение
        if (anim != null)
        {
            anim.ResetTrigger("Hurt");
            anim.SetTrigger("Hurt");
        }
        if (controller != null) controller.Stun();

        invulnerableUntil = Time.time + invulnerableTime;
        StartCoroutine(Blink());
        return true;
    }

    // Мгновенная смерть (лазер и т.п.), неуязвимость не учитывается
    public void Kill()
    {
        if (isDead) return;

        hp = 0;
        healthBar.SetHealth(hp, maxHp);
        Die();
    }

    IEnumerator Blink()
    {
        while (Time.time < invulnerableUntil && !isDead)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
        if (sr != null) sr.enabled = true;
    }

    void Die()
    {
        isDead = true;

        if (sr != null) sr.enabled = true;

        // отключаем управление
        if (controller != null) controller.enabled = false;

        rb.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetTrigger("Die");

        Invoke(nameof(Restart), restartDelay);
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}