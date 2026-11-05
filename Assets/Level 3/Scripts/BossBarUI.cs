using UnityEngine;
using UnityEngine.UI;

public class BossBarUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image fill;      // Image Type = Filled, Horizontal
    [SerializeField] private float speed = 0.8f;

    private float target = 1f;
    private bool shown;

    void Awake()
    {
        group.alpha = 0;
        fill.fillAmount = 0;
    }

    public void Show() { shown = true; }
    public void Hide() { shown = false; }
    public void SetValue(float v) { target = v; }

    void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, shown ? 1 : 0, Time.deltaTime * 2f);
        if (shown)
            fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, Time.deltaTime * speed);
    }
}