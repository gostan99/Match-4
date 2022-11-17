using System.Collections.Generic;
using UnityEngine;

public class TilePoolDisplayer : MonoBehaviour
{
    [SerializeField] private Vector2 tileScale;
    [SerializeField] private float firstTileScale = 1.3f;
    [SerializeField] private float tileMoveSpeed = 0.3f;

    private List<GameTile> listTile = new();
    private GameGrid grid;

    // Start is called before the first frame update
    private void Start()
    {
        grid = transform.parent.GetComponent<GameGrid>();
        InitPool();
        grid.GetGameMode().OnScoreChange.AddListener(OnScoreChange);
    }

    private void OnScoreChange()
    {
        int diff = Mathf.Abs(grid.TileInfoIdPool.Count - listTile.Count);
        Vector3 spawnPosition;
        var list = grid.TileInfoIdPool;
        for (int i = list.Count - diff; i < list.Count; i++)
        {
            int infoId = list[i];
            spawnPosition = GetWorldPositionAtIndex(i);

            var tile = CreateTile(infoId, spawnPosition, i, true);
            listTile.Add(tile);
        }
    }

    private void Update()
    {
        int diff = Mathf.Abs(grid.TileInfoIdPool.Count - listTile.Count);
        if (grid.TileInfoIdPool.Count < listTile.Count)
        {
            for (int i = 0; i < diff; i++)
            {
                var tile = listTile[0];
                listTile.Remove(tile);
                tile.DestroyTile();
            }

            for (int i = 0; i < listTile.Count; i++)
            {
                GameTile t = listTile[i];
                if (i != 0)
                    t.StartMoving(GetWorldPositionAtIndex(i));
                else
                    t.StartMoving(GetWorldPositionAtIndex(i), firstTileScale);
            }
        }
    }

    private void InitPool()
    {
        Vector3 spawnPosition;
        for (int i = 0; i < grid.TileInfoIdPool.Count; ++i)
        {
            int infoId = grid.TileInfoIdPool[i];
            spawnPosition = GetWorldPositionAtIndex(i);
            GameTile tile;
            if (i == 0)
                tile = CreateTile(infoId, spawnPosition, i, true, firstTileScale);
            else
                tile = CreateTile(infoId, spawnPosition, i, true);
            listTile.Add(tile);
        }
        //listTile[0].StartMoving(listTile[0].transform.position, firstTileScale);
        //listTile[0].transform.Find("Tile Mesh").localScale = Vector3.one * firstTileScale;
    }

    private Vector3 GetWorldPositionAtIndex(int poolIndex)
    {
        Vector3 center = transform.position;
        //  +(grid.TileSize.x * 0.5f - grid.TileSize.x * 0.25f)
        Vector3 outLocation = new(-(grid.poolSize * 0.5f) * (grid.TileSize.x * tileScale.x * 0.5f) + (grid.TileSize.x * tileScale.x * 0.25f), 0.0f, 0.0f); // very left position
        outLocation.x += (grid.TileSize.x * tileScale.x * 0.5f) * (float)(poolIndex);
        outLocation += center;
        outLocation.z = poolIndex;

        return outLocation;
    }

    private GameTile CreateTile(int infoId, Vector3 worldPos, int poolIndex, bool playSpawnEffect = false, float tileMeshScale = 1)
    {
        GameObject prefab = grid.TileInfoArr[infoId].tilePrefab;
        var tileGObj = Instantiate(prefab);
        tileGObj.transform.parent = transform;
        tileGObj.transform.position = worldPos;
        tileGObj.transform.localScale = new(tileScale.x, tileScale.y, 0);
        tileGObj.GetComponent<BoxCollider>().enabled = false;

        var tile = tileGObj.GetComponent<GameTile>();
        tile.Init(grid, poolIndex, infoId, playSpawnEffect, tileMeshScale);
        tile.isPoolDisplay = true;
        tile.SetSpeed(tileMoveSpeed);

        if (!tile.abilities.CanExplodes)
            tileGObj.GetComponent<Animator>().enabled = false;

        return tile;
    }
}