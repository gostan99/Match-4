using UnityEngine;

public class Level0Manger : LevelManager
{
    [SerializeField] private GameObject gridPrefab;

    public GameMode gameMode;
    public GameGrid grid;

    private void Start()
    {
        gameMode = GetComponent<GameMode>();
        var gridGObj = Instantiate(gridPrefab);
        grid = gridGObj.GetComponent<GameGrid>();
        grid.Init();
        grid.SetGameMode(gameMode);
        grid.MakeGrid();
    }
}