using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Атака")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.6f;
    [SerializeField] private float damage = 10;
    [SerializeField] private float cooldown = 0.4f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool damageOnAnimationEvent = false;

    [Header("Управление")]
    [SerializeField] private KeyCode attackKey = KeyCode.F;
    [SerializeField] private KeyCode altAttackKey = KeyCode.J;
    [SerializeField] private bool allowMouse = true;

    [Header("Блокировка движения")]
    [Tooltip("Сколько секунд игрок не может ходить и прыгать после начала атаки (примерно длина анимации)")]
    [SerializeField] private float moveLockTime = 0.4f;

    [Header("Направление")]
    // true, если спрайт героя по умолчанию (flipX = false) смотрит вправо
    [SerializeField] private bool spriteFacesRight = true;

    private Animator anim;
    private SpriteRenderer sr;
    private PlayerController controller;
    private PlayerHealth health;
    private float pointOffsetX;
    private float nextAttackTime;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        pointOffsetX = Mathf.Abs(attackPoint.localPosition.x);
    }

    void Update()
    {
        UpdateAttackPointSide();

        // NEW: экранная кнопка атаки; флаг сбрасываем сразу,
        // чтобы нажатие во время оглушения не "выстрелило" позже
        bool mobileAttack = MobileInput.Attack;
        MobileInput.Attack = false;

        if (health != null && health.IsDead) return;
        if (controller != null && controller.IsStunned) return;

        bool pressed = Input.GetKeyDown(attackKey)
                    || Input.GetKeyDown(altAttackKey)
                    || (allowMouse && Input.GetMouseButtonDown(0))
                    || mobileAttack; // NEW

        if (pressed && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + cooldown;
            anim.SetTrigger("Attack");

            if (controller != null) controller.LockMovement(moveLockTime);
            if (!damageOnAnimationEvent) DealDamage();
        }
    }

    // Ставит точку удара с той стороны, куда смотрит герой
    void UpdateAttackPointSide()
    {
        bool facingRight = spriteFacesRight ? !sr.flipX : sr.flipX;
        float side = facingRight ? 1f : -1f;

        Vector3 p = attackPoint.localPosition;
        attackPoint.localPosition = new Vector3(pointOffsetX * side, p.y, p.z);
    }

    // Если есть анимация удара, вызови это Animation Event'ом в кадре удара
    public void DealDamage()
    {
        var hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var h in hits)
        {
            var target = h.GetComponentInParent<IDamageable>();
            // у одного врага может быть несколько коллайдеров, бьём каждого один раз
            if (target != null && alreadyHit.Add(target))
                target.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}