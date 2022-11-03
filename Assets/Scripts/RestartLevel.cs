using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestartLevel : MonoBehaviour
{
    public void OnBtnClick(int levelIndex)
    {
        GameManager.Instance.StartLevel(levelIndex);
    }
}