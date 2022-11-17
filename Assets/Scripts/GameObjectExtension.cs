using UnityEngine;

public static class GameObjectExtension
{
    public static void SaveState(this GameObject go)
    {
        if (go.TryGetComponent<Level0Manger>(out var lvMgr))
        {
            lvMgr.grid.SaveGridState();
        }
    }
}