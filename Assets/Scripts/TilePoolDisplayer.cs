using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class TilePoolDisplayer : MonoBehaviour
{
    [SerializeField] private Vector2 tileScale;

    private List<GameTile> listTile = new();
    private GameGrid grid;

    // Start is called before the first frame update
    private void Start()
    {
        grid = transform.parent.GetComponent<GameGrid>();
        InitPool();
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
            foreach (var t in listTile)
            {
                int index = listTile.IndexOf(t);
                t.StartMoving(GetWorldPositionAtIndex(index), false);
            }
        }
        else if (grid.TileInfoIdPool.Count > listTile.Count)
        {
            Vector3 spawnPosition;
            var list = grid.TileInfoIdPool;
            for (int i = list.Count - diff; i < list.Count; i++)
            {
                int infoId = list[i];
                spawnPosition = GetWorldPositionAtIndex(i);

                var tile = CreateTile(infoId, spawnPosition, i);
                listTile.Add(tile);
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

            var tile = CreateTile(infoId, spawnPosition, i);
            listTile.Add(tile);
        }
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

    private GameTile CreateTile(int infoId, Vector3 worldPos, int poolIndex, bool playSpawnEffect = false)
    {
        GameObject prefab = grid.TileInfoArr[infoId].tilePrefab;
        var tileGObj = Instantiate(prefab);
        tileGObj.transform.position = worldPos;
        tileGObj.transform.localScale = new(tileScale.x, tileScale.y, 0);
        tileGObj.GetComponent<BoxCollider>().enabled = false;

        var tile = tileGObj.GetComponent<GameTile>();
        tile.Init(grid, poolIndex, infoId, playSpawnEffect);
        tile.isPoolDisplay = true;

        if (!tile.abilities.CanExplodes)
            tileGObj.GetComponent<Animator>().enabled = false;

        return tile;
    }
}