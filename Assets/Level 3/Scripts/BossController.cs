using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BossController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform player;
    [SerializeField] private BossBarUI bar;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private Transform meleePoint;
    [SerializeField] private Transform laserPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject laserVisual;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Пробуждение")]
    [SerializeField] private float wakeRange = 8f;
    [SerializeField] private float wakeTime = 2f;

    [Header("Дальняя атака")]
    [SerializeField] private float shootCooldown = 2.4f;
    [SerializeField] private float shootDelay = 0.4f;
    [SerializeField] private float shootRecovery = 0.4f;
    [SerializeField] private float projectileDamage = 20f;
    [SerializeField] private float maxAimAngle = 40f;

    [Header("Ближний бой")]
    [SerializeField] private float closeRange = 3f;
    [SerializeField] private float closeTimeToMelee = 0.25f;
    [SerializeField] private float closeCooldown = 6.5f;
    [SerializeField] private float meleeHitTimeout = 0.85f;
    [SerializeField] private float meleeAnimTime = 0.95f;
    [SerializeField] private float meleeRadius = 3.6f;
    [SerializeField] private float meleeDamage = 13f;
    [SerializeField] private float knockback = 11f;
    [SerializeField] private float meleeRecovery = 0.5f;

    [Header("Щит (по порогам HP)")]
    [SerializeField] private float shieldTime = 1.2f;
    [SerializeField] private float healPercent = 0.05f;

    [Header("Лазер")]
    [SerializeField] private float laserCastTime = 1.1f;
    [SerializeField] private float laserActiveTime = 0.55f;
    [SerializeField] private float hoverHeight = 4f;
    [SerializeField] private float riseTime = 0.75f;
    [SerializeField] private float maxTilt = 75f;
    [SerializeField] private float tiltTime = 0.35f;
    [SerializeField] private float sweepDistance = 8f;
    [SerializeField] private float sweepGroundOffset = 0.15f;
    [SerializeField] private float laserWidth = 1.15f;

    [Header("Гибернация (один раз, после лазера на 50% HP)")]
    [SerializeField] private float hibernateDamageTaken = 0.25f;   // во сне получает 25% урона
    [SerializeField] private float hibernateTime = 10f;            // сколько секунд спит
    [SerializeField] private float hibernateSleepAnimTime = 1f;    // длина анимации "голем -> камень"
    [SerializeField] private float hibernateHealPercent = 0.045f;  // суммарный хил за сон: 4.5% от макс. HP, плавно
    [SerializeField] private float hibernateWakeTime = 1f;         // длина анимации "камень -> голем" (скорость -1)

    [Header("После 30% HP (защита)")]
    [SerializeField] private float lateDamageTaken = 0.5f;         // получает 50% урона до конца боя
    [SerializeField] private float lateBossDamageMultiplier = 1.15f; // босс бьёт сильнее (поставь 1, если не нужно)

    [Header("Шипы под боссом")]
    [SerializeField] private GameObject spikesPrefab;
    [SerializeField] private Vector2 spikesSize = new Vector2(3f, 1.5f);
    [SerializeField] private float spikesDelay = 0.35f;
    [SerializeField] private float spikesDamage = 25f;
    [SerializeField] private float spikesTick = 0.5f;
    [SerializeField] private bool spikesKill = false;

    [Header("События")]
    [SerializeField] private UnityEvent onDeath;

    private BossHealth health;
    private Animator anim;
    private Collider2D laserCol;
    private SpriteRenderer[] laserSprites;
    private int facing = 1;
    private float damageMultiplier = 1f;
    private float nextShoot;
    private float nextClose;
    private bool laser1Done, lastLaserDodged;
    private bool spikesActive;
    private bool shield75Done;
    private bool shield45Done;
    private bool shield30Done;
    private bool interruptAttack;
    private bool meleeHitEvent;

    void Awake()
    {
        health = GetComponent<BossHealth>();
        anim = GetComponent<Animator>();
        if (laserVisual != null)
        {
            laserCol = laserVisual.GetComponentInChildren<Collider2D>(true);
            laserSprites = laserVisual.GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    void Start()
    {
        health.Invulnerable = true;
        SetLaser(false, 1f);
        StartCoroutine(Fight());
    }

    // Вызывается из Animation Event в кадре, где рука бьёт
    public void OnMeleeHit() { meleeHitEvent = true; }

    float DistToPlayer() => Vector2.Distance(transform.position, player.position);

    void Face()
    {
        facing = player.position.x >= transform.position.x ? 1 : -1;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (spriteFacesRight ? facing : -facing);
        transform.localScale = s;
    }

    Vector2 PlayerCenter()
    {
        var col = player.GetComponent<Collider2D>();
        return col != null ? (Vector2)col.bounds.center : (Vector2)player.position;
    }

    // Нужно ли оборвать ближний бой, потому что пора делать лазер (только на 50%)
    bool ShouldInterruptMelee()
    {
        if (interruptAttack) return true;
        if (!laser1Done && health.Percent <= 0.5f) return true;
        return false;
    }

    IEnumerator Fight()
    {
        while (DistToPlayer() > wakeRange) yield return null;

        Face();
        anim.SetTrigger("WakeUp");
        bar.Show();
        yield return new WaitForSeconds(wakeTime);
        anim.SetTrigger("WakeDone");

        health.Invulnerable = false;
        nextShoot = Time.time + 1.3f;
        float closeTimer = 0f;

        while (!health.IsDead)
        {
            var ph = player.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsDead) yield break;

            Face();
            float hp = health.Percent;

            if (hp <= 0.02f)
            {
                yield return FinalPhase();
                yield break;
            }

            // 50% HP: лазер -> гибернация (один раз за бой)
            if (!laser1Done && hp <= 0.5f)
            {
                laser1Done = true;
                interruptAttack = true;
                yield return LaserAttack(true);
                interruptAttack = false;

                nextShoot = Time.time + 1f;
                nextClose = Time.time + 1f;
                closeTimer = 0f;
                continue;
            }

            // 30% HP: просто защита, до конца боя получает 50% урона
            if (!shield30Done && hp <= 0.3f)
            {
                shield30Done = true;
                yield return ShieldPhase30();

                nextShoot = Time.time + 1f;
                nextClose = Time.time + 1f;
                closeTimer = 0f;
                continue;
            }

            if (!shield75Done && hp <= 0.75f)
            {
                shield75Done = true;
                yield return ShieldHeal();
                continue;
            }
            if (!shield45Done && hp <= 0.45f)
            {
                shield45Done = true;
                yield return ShieldHeal();
                continue;
            }

            bool close = DistToPlayer() < closeRange;
            closeTimer = close ? closeTimer + Time.deltaTime : 0f;

            if (closeTimer >= closeTimeToMelee && Time.time >= nextClose)
            {
                closeTimer = 0f;
                yield return MeleeAttack();
                nextClose = Time.time + closeCooldown;
                continue;
            }

            if (!close && Time.time >= nextShoot)
            {
                yield return Shoot();
                nextShoot = Time.time + shootCooldown;
                continue;
            }

            yield return null;
        }
    }

    IEnumerator Shoot()
    {
        anim.SetTrigger("Shoot");
        yield return new WaitForSeconds(shootDelay);

        Vector2 target = PlayerCenter();
        Vector2 dir = target - (Vector2)shootPoint.position;

        Vector2 forward = Vector2.right * facing;
        float angle = Vector2.SignedAngle(forward, dir);
        angle = Mathf.Clamp(angle, -maxAimAngle, maxAimAngle);
        dir = Quaternion.Euler(0f, 0f, angle) * forward;

        var go = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        go.GetComponent<BossProjectile>().Init(dir, projectileDamage * damageMultiplier);

        yield return new WaitForSeconds(shootRecovery);
    }

    IEnumerator ShieldHeal()
    {
        health.Invulnerable = true;
        anim.SetBool("Shield", true);

        yield return new WaitForSeconds(shieldTime * 0.45f);
        health.Heal(health.MaxHp * healPercent);
        yield return new WaitForSeconds(shieldTime * 0.55f);

        anim.SetBool("Shield", false);
        health.Invulnerable = false;
        yield return new WaitForSeconds(0.12f);
    }

    // Защита на 30% HP: без хила, дальше получает меньше урона до конца боя
    IEnumerator ShieldPhase30()
    {
        health.Invulnerable = true;
        anim.SetBool("Shield", true);

        yield return new WaitForSeconds(shieldTime);

        anim.SetBool("Shield", false);
        health.Invulnerable = false;
        health.DamageTakenMultiplier = lateDamageTaken;
        damageMultiplier = lateBossDamageMultiplier;

        yield return new WaitForSeconds(0.12f);
    }

    // ===================== БЛИЖНИЙ УДАР =====================
    IEnumerator MeleeAttack()
    {
        Face();
        interruptAttack = false;
        meleeHitEvent = false;

        anim.ResetTrigger("Melee");
        anim.SetTrigger("Melee");

        float waited = 0f;
        while (!meleeHitEvent && waited < meleeHitTimeout)
        {
            if (ShouldInterruptMelee()) yield break;
            waited += Time.deltaTime;
            yield return null;
        }

        var hit = Physics2D.OverlapCircle(meleePoint.position, meleeRadius, playerLayer);
        if (hit != null)
        {
            var php = hit.GetComponentInParent<PlayerHealth>();
            if (php != null && !php.IsDead)
            {
                bool applied = php.TakeDamage(meleeDamage * damageMultiplier);
                if (applied && !php.IsDead) Knockback(php);
            }
        }

        float remaining = Mathf.Max(0f, meleeAnimTime - waited);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        nextShoot = Mathf.Max(nextShoot, Time.time + meleeRecovery);
        yield return new WaitForSeconds(meleeRecovery);
    }

    // Оглушение теперь делает сам PlayerHealth (Stun), здесь только толчок
    void Knockback(PlayerHealth php)
    {
        var rb = php.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        float dir = Mathf.Sign(php.transform.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * knockback, knockback * 0.38f);
    }

    // ===================== ГИБЕРНАЦИЯ =====================
    // 1) анимация "голем -> камень" (клип на скорости 1)
    // 2) 10 секунд сна: получает 25% урона, один раз небольшой хил
    // 3) анимация "камень -> голем" (тот же клип на скорости -1, это же состояние WakeUp)
    IEnumerator Hibernate()
    {
        anim.SetBool("Glow", false);
        anim.ResetTrigger("WakeUp");
        anim.ResetTrigger("WakeDone");

        health.Invulnerable = false;
        health.DamageTakenMultiplier = hibernateDamageTaken;
        anim.SetBool("Hibernate", true);

        // Хил растянут на всё время сна (после анимации засыпания)
        float healDuration = Mathf.Max(0.1f, hibernateTime - hibernateSleepAnimTime);
        float healPerSecond = health.MaxHp * hibernateHealPercent / healDuration;

        float t = 0f;

        while (t < hibernateTime)
        {
            if (health.IsDead) yield break;

            var ph = player.GetComponent<PlayerHealth>();
            if (ph != null && ph.IsDead) yield break;

            t += Time.deltaTime;

            // плавный хил, пока он "окаменел"
            if (t >= hibernateSleepAnimTime)
                health.Heal(healPerSecond * Time.deltaTime);

            yield return null;
        }

        // Просыпаемся: возвращаем множитель урона
        health.DamageTakenMultiplier = shield30Done ? lateDamageTaken : 1f;

        // Hibernate -> WakeUp (клип на скорости -1)
        anim.SetBool("Hibernate", false);
        anim.SetTrigger("WakeUp");

        // На время пробуждения босс неуязвим, чтобы не убили в самый уязвимый момент
        health.Invulnerable = true;
        yield return new WaitForSeconds(hibernateWakeTime);
        health.Invulnerable = false;

        anim.SetTrigger("WakeDone");
        yield return new WaitForSeconds(0.1f);
    }

    // ===================== ЛАЗЕР =====================
    IEnumerator MoveY(float toY, float time)
    {
        float fromY = transform.position.y;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            var p = transform.position;
            p.y = Mathf.Lerp(fromY, toY, t / time);
            transform.position = p;
            yield return null;
        }
        var final = transform.position;
        final.y = toY;
        transform.position = final;
    }

    float CurrentTilt()
    {
        float z = transform.eulerAngles.z;
        return z > 180f ? z - 360f : z;
    }

    IEnumerator TiltTo(float toZ, float time)
    {
        float fromZ = CurrentTilt();
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float z = Mathf.Lerp(fromZ, toZ, t / time);
            transform.rotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, toZ);
    }

    float TiltToAim(Vector2 point, Vector2 from)
    {
        Vector2 dir = point - from;
        if (dir.sqrMagnitude < 0.001f) return CurrentTilt();

        float desiredAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float localBeamAngle = 0f;

        float needed = Mathf.DeltaAngle(localBeamAngle, desiredAngle);
        if (transform.localScale.x < 0f) needed = -needed;

        return Mathf.Clamp(needed, -maxTilt, maxTilt);
    }

    void SetLaser(bool active, float alpha)
    {
        if (laserVisual == null) return;
        laserVisual.SetActive(active);
        if (!active || laserSprites == null) return;

        foreach (var sr in laserSprites)
        {
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    bool LaserHitsPlayer(out PlayerHealth php)
    {
        php = null;
        Vector2 origin = laserPoint != null ? (Vector2)laserPoint.position : (Vector2)transform.position;

        float angle = CurrentTilt();
        if (transform.localScale.x < 0f) angle = -angle;

        Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
        float length = sweepDistance + 1.5f;
        Vector2 center = origin + dir * (length * 0.5f);
        Vector2 size = new Vector2(length, laserWidth);

        var hit = Physics2D.OverlapBox(center, size, angle, playerLayer);
        if (hit != null)
        {
            php = hit.GetComponentInParent<PlayerHealth>();
            return php != null;
        }
        return false;
    }

    float FindFloorY(float fallbackY)
    {
        if (groundLayer.value == 0) return fallbackY;
        var hit = Physics2D.Raycast(transform.position, Vector2.down, 40f, groundLayer);
        return hit.collider != null ? hit.point.y : fallbackY;
    }

    IEnumerator SpikesRoutine(Vector2 floorPoint)
    {
        yield return new WaitForSeconds(spikesDelay);
        if (!spikesActive) yield break;

        GameObject spikes = null;
        if (spikesPrefab != null)
            spikes = Instantiate(spikesPrefab, floorPoint, Quaternion.identity);

        Vector2 zoneCenter = floorPoint + Vector2.up * (spikesSize.y * 0.5f);
        float nextTick = 0f;

        while (spikesActive)
        {
            if (Time.time >= nextTick)
            {
                var hit = Physics2D.OverlapBox(zoneCenter, spikesSize, 0f, playerLayer);
                if (hit != null)
                {
                    var php = hit.GetComponentInParent<PlayerHealth>();
                    if (php != null && !php.IsDead)
                    {
                        if (spikesKill) php.Kill();
                        else php.TakeDamage(spikesDamage * damageMultiplier);
                        nextTick = Time.time + spikesTick;
                    }
                }
            }
            yield return null;
        }

        if (spikes != null) Destroy(spikes);
    }

    IEnumerator LaserAttack(bool goHibernate)
    {
        anim.SetBool("Glow", false);
        anim.ResetTrigger("Melee");

        health.Invulnerable = true;
        Face();
        int sweepDirSign = facing;
        float groundY = transform.position.y;
        float floorY = FindFloorY(groundY);

        spikesActive = true;
        StartCoroutine(SpikesRoutine(new Vector2(transform.position.x, floorY)));

        anim.SetTrigger("LaserCast");
        yield return MoveY(groundY + hoverHeight, riseTime);

        SetLaser(true, 0.35f);
        Vector2 from = laserPoint != null ? (Vector2)laserPoint.position : (Vector2)transform.position;
        Vector2 underPoint = new Vector2(from.x, floorY + sweepGroundOffset);
        float underTilt = TiltToAim(underPoint, from);
        yield return TiltTo(underTilt, tiltTime);

        yield return new WaitForSeconds(Mathf.Max(0.08f, laserCastTime - tiltTime));

        from = laserPoint != null ? (Vector2)laserPoint.position : (Vector2)transform.position;
        Vector2 endPoint = new Vector2(from.x + sweepDirSign * sweepDistance, floorY + sweepGroundOffset);
        float targetTilt = TiltToAim(endPoint, from);

        SetLaser(true, 1f);
        lastLaserDodged = true;

        float startTilt = CurrentTilt();
        float t = 0f;
        while (t < laserActiveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / laserActiveTime);
            float z = Mathf.Lerp(startTilt, targetTilt, k);
            transform.rotation = Quaternion.Euler(0f, 0f, z);

            if (LaserHitsPlayer(out var php))
            {
                php.Kill();
                lastLaserDodged = false;
                break;
            }
            yield return null;
        }

        SetLaser(false, 1f);
        yield return TiltTo(0f, tiltTime);
        yield return MoveY(groundY, riseTime);

        spikesActive = false;

        if (!lastLaserDodged) yield break;

        if (goHibernate)
        {
            yield return Hibernate();
        }
        else
        {
            health.Invulnerable = false;
        }
    }

    IEnumerator FinalPhase()
    {
        health.Invulnerable = true;
        anim.SetBool("Shield", true);
        yield return new WaitForSeconds(1.4f);
        anim.SetBool("Shield", false);

        yield return LaserAttack(false);

        if (lastLaserDodged) yield return Die();
    }

    IEnumerator Die()
    {
        spikesActive = false;
        health.Kill();
        anim.SetTrigger("Die");
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        yield return new WaitForSeconds(1f);
        onDeath?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wakeRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, closeRange);
        if (meleePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePoint.position, meleeRadius);
        }
        if (laserPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(laserPoint.position, 0.25f);
        }
    }
}