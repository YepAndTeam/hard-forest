using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;

    public void SetHealth(float hp, float maxHp)
    {
        fill.fillAmount = hp / maxHp;
    }
}