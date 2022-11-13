using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class GameMode : MonoBehaviour
{
    public UnityEvent OnScoreChange = new UnityEvent();

    [SerializeField] private TextMeshProUGUI highScoreDisplay;
    [SerializeField] private TextMeshProUGUI scoreDisplay;
    [SerializeField] private GameObject gameOverScreen;

    private bool isGameOver = false;
    private float score;

    public void AddScore(float val)
    {
        score += val;
        OnScoreChange?.Invoke();
    }

    public float GetScore()
    { return score; }

    public void GameOver()
    {
        isGameOver = true;
        if (score > PlayerPrefs.GetInt("High Score"))
            PlayerPrefs.SetInt("High Score", (int)score);
        PlayerPrefs.Save();
        var screen = Instantiate(gameOverScreen);
        screen.transform.SetParent(highScoreDisplay.transform.parent.parent);
        screen.transform.localPosition = Vector3.zero;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    private void Start()
    {
        highScoreDisplay.text = PlayerPrefs.GetInt("High Score").ToString();
    }
}