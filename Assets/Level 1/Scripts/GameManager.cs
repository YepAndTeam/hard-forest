using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TextMeshProUGUI crystalText;   // текст на Canvas
    public string nextLevelName = "Level2";

    private int collected;
    private int total;

    // Портал смотрит на это свойство
    public bool AllCollected => collected >= total;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Сколько кристаллов на уровне, считаем сами при старте
        total = FindObjectsByType<Crystal>(FindObjectsSortMode.None).Length;
        UpdateText();
    }

    public void AddCrystal()
    {
        collected++;
        UpdateText();
        // Уровень здесь больше не грузим, это делает портал
    }

    public void LoadNextLevel()
    {
        SceneManager.LoadScene(nextLevelName);
    }

    void UpdateText()
    {
        if (crystalText != null)
            crystalText.text = collected + " / " + total;
    }
}