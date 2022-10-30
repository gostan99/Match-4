using System;
using UnityEngine;

public class GameMode : MonoBehaviour
{
    private bool isGameOver = false;
    private float score;

    public void AddScore(float val)
    {
        score += val;
    }

    public float GetScore()
    { return score; }

    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over!");
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    private void Update()
    {
        Debug.Log(score);
    }
}