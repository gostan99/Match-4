using System.Collections;
using TMPro;
using UnityEngine;

public class PointsUI : MonoBehaviour
{
    private GameMode gameMode;
    private TextMeshProUGUI text;

    // Start is called before the first frame update
    private void Start()
    {
        text = transform.GetComponentInChildren<TextMeshProUGUI>();
        StartCoroutine(UpdateText());
    }

    private IEnumerator UpdateText()
    {
        while (GameManager.Instance.LevelManager == null || GameManager.Instance.LevelManager.grid == null)
        {
            yield return null;
        }
        gameMode = GameManager.Instance.LevelManager.gameMode;
        int lastScore = (int)(gameMode.GetScore());
        int currentScore = lastScore;
        int score = 0;
        float s = 0, v = 0, t = 0.5f;
        while (true)
        {
            score = (int)(gameMode.GetScore());
            if (score != lastScore)
            {
                lastScore = score;
                s = score - currentScore;
                v = s / t;
            }
            if (currentScore < lastScore)
                currentScore += (int)(v * Time.deltaTime);
            else
                currentScore = lastScore;

            text.text = currentScore.ToString();
            yield return null;
        }
    }
}