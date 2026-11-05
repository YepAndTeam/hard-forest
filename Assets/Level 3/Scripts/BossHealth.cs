using UnityEngine;

public class BossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 300;
    [SerializeField] private BossBarUI bar;

    public float Percent => hp / maxHp;
    public float MaxHp => maxHp;
    public bool Invulnerable { get; set; }
    public bool IsDead { get; private set; }

    // 1 = полный урон, 0.25 = получает 25% урона, 0.6 = получает 60% урона
    public float DamageTakenMultiplier { get; set; } = 1f;

    private float hp;

    void Awake() { hp = maxHp; }

    public void TakeDamage(float damage)
    {
        if (Invulnerable || IsDead) return;
        damage *= DamageTakenMultiplier;
        // ниже 1% урон не опускает, добить может только сам босс
        hp = Mathf.Max(maxHp * 0.01f, hp - damage);
        if (bar != null) bar.SetValue(Percent);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        hp = Mathf.Min(maxHp, hp + amount);
        if (bar != null) bar.SetValue(Percent);
    }

    public void Kill()
    {
        IsDead = true;
        hp = 0;
        if (bar != null) { bar.SetValue(0); bar.Hide(); }
    }
}