using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PoolCountUI : MonoBehaviour
{
    private GameGrid gameGrid;
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
        gameGrid = GameManager.Instance.LevelManager.grid;
        while (true)
        {
            text.text = gameGrid.TileInfoIdPool.Count.ToString();
            yield return null;
        }
    }
}