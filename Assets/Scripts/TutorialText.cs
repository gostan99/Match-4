using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialText : MonoBehaviour
{
    [SerializeField] private string[] texts;
    private TextMeshProUGUI text;
    private int index = 0;

    private void Start()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        CellState[] cellStates = JsonConvert.DeserializeObject<CellState[]>(PlayerPrefs.GetString("CellStates"));
        if (cellStates != null)
        {
            gameObject.SetActive(false);
        }
        else
        {
            text.text = texts[index++];
        }
    }

    public void OnClick()
    {
        if (index == texts.Length)
        {
            gameObject.SetActive(false);
            return;
        }
        text.text = texts[index++];
    }
}