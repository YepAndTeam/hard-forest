using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Проверка земли")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Падение")]
    [Tooltip("Если включено, при падении не играется анимация прыжка")]
    public bool hideFallAnimation = true;
    public float fallThreshold = -0.1f;

    [Header("Оглушение (длится, пока играет анимация получения урона)")]
    [Tooltip("Точное имя состояния в Аниматоре")]
    public string hurtStateName = "hurt";
    [Tooltip("Если анимация не началась за это время, оглушение снимается (защита от неверной настройки Аниматора)")]
    public float hurtStartTimeout = 0.25f;
    [Tooltip("Как быстро гасится горизонтальная скорость (отталкивание) во время оглушения")]
    public float stunFriction = 25f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private CapsuleCollider2D col;

    private ContactFilter2D wallFilter;
    private RaycastHit2D[] hits = new RaycastHit2D[4];

    private bool isGrounded;
    private float attackEndTime;
    private bool stunnedFlag;
    private bool sawHurt;
    private float stunStartTime;

    public bool IsStunned => stunnedFlag;
    public bool IsAttackLocked => Time.time < attackEndTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CapsuleCollider2D>();

        wallFilter.useLayerMask = true;
        wallFilter.SetLayerMask(groundLayer);
        wallFilter.useTriggers = false;
    }

    // Вызывается из PlayerAttack: на это время ходить и прыгать нельзя
    public void LockMovement(float time)
    {
        attackEndTime = Time.time + time;
    }

    // Вызывается из PlayerHealth при получении урона
    public void Stun()
    {
        stunnedFlag = true;
        sawHurt = false;
        stunStartTime = Time.time;
        attackEndTime = 0f;           // оглушение прерывает атаку
        anim.ResetTrigger("Attack");  // чтобы атака не "выстрелила" после оглушения
    }

    // Оглушение снимается, когда анимация получения урона закончилась
    void UpdateStun()
    {
        if (!stunnedFlag) return;

        bool inHurt = anim.GetCurrentAnimatorStateInfo(0).IsName(hurtStateName)
                   || (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsName(hurtStateName));

        if (inHurt)
        {
            sawHurt = true;
        }
        else if (sawHurt)
        {
            stunnedFlag = false;      // анимация доиграла и ушла в idle
        }
        else if (Time.time - stunStartTime > hurtStartTimeout)
        {
            stunnedFlag = false;      // анимация так и не началась
        }
    }

    // Упираемся ли в стену в сторону движения
    bool IsBlockedByWall(float dir)
    {
        if (dir == 0) return false;

        int count = col.Cast(new Vector2(dir, 0), wallFilter, hits, 0.05f);
        for (int i = 0; i < count; i++)
        {
            // стена имеет горизонтальную нормаль, земля под ногами нет
            if (Mathf.Abs(hits[i].normal.x) > 0.5f)
                return true;
        }
        return false;
    }

    void Update()
    {
        UpdateStun();

        // NEW: прыжок с клавиатуры или с экранной кнопки; флаг сбрасываем сразу,
        // чтобы нажатие во время оглушения/удара не "выстрелило" позже
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || MobileInput.Jump;
        MobileInput.Jump = false;

        // 1. Стоим ли на земле
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        bool stunned = IsStunned;
        bool locked = IsAttackLocked;

        // 2. Ходьба (во время удара и оглушения управлять нельзя)
        float move = 0f;
        if (!stunned && !locked)
        {
            move = Input.GetAxisRaw("Horizontal");
            if (move == 0) move = MobileInput.Horizontal; // NEW: экранные кнопки ◀ ▶
        }

        if (stunned)
        {
            // не перебиваем отталкивание, а плавно гасим его
            float vx = Mathf.MoveTowards(rb.linearVelocity.x, 0f, stunFriction * Time.deltaTime);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);
        }

        // 3. Поворот спрайта
        if (move > 0) sr.flipX = false;
        else if (move < 0) sr.flipX = true;

        // 4. Прыжок
        if (jumpPressed && isGrounded && !locked && !stunned) // NEW: jumpPressed
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // 5. Animator: у стены скорость считаем нулевой
        bool blocked = IsBlockedByWall(move);
        float animSpeed = blocked ? 0f : Mathf.Abs(move);

        // При падении "притворяемся", что стоим на земле: анимация прыжка не играет
        bool falling = hideFallAnimation && !isGrounded && rb.linearVelocity.y < fallThreshold;

        anim.SetFloat("Speed", animSpeed);
        anim.SetBool("IsGrounded", isGrounded || falling);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}