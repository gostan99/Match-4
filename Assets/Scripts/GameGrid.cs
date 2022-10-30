using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameGrid : MonoBehaviour
{
    public static GameGrid Instance { get; private set; }
    public Vector2 TileSize => tileSize;
    public TileInfo[] TileInfoArr => tileInfoArr;

    [Header("\nDO NOT TOUCH THESE IN EDITOR!\n")]
    public OnExecuteMatch onExecuteMatch = new();

    public TileMoveType lastMoveType;
    public List<GameTile> bombsBeingExplode;
    public List<int> TileInfoIdPool => tileInfoIdPool;

    [Header("\nPARAMETERS\n")]
    public int poolSize;

    public int initialPoolElementNum;
    [SerializeField] private TileInfo[] tileInfoArr;
    [SerializeField] private Vector2 tileSize = new(1, 1);
    [SerializeField] private Vector2Int gridSize = new(4, 4);
    [SerializeField] private int minimumMatchedCount = 4;
    [SerializeField] private int imovablePoint = 4;
    [SerializeField] private float confirmSwipeDirLength = 0.4f; // độ dài nhỏ nhất để xác nhận chiều mà người dùng kéo tile
    [SerializeField] private float confirmMoveTileLength = 1f;   // độ dài nhỏ nhất để xác nhận bắt đầu di chuyển tile
    [SerializeField] private float matchingDelayTime = 0.5f;

    private GameTile[] tiles;
    private List<GameTile> movingTiles;
    private List<int> rowNotContainImovableTile;
    private List<int> columnNotContainImovableTile;
    private List<int> tileInfoIdPool;
    private GameMode gameMode;
    private GameTile currentlySelectedTile;
    private int combos = 1;
    private int moveSuccessCount = 0;
    private WaitForSeconds executeMatchDelay;
    private bool isExecuteMatchDelay = false;
    private int lastMatchedTilesCount = 0;

    // Tiles that are currently reacting to being matches.
    private List<GameTile> tilesBeingDestroyed;

    public void Init()
    {
        tiles = new GameTile[gridSize.x * gridSize.y];
        movingTiles = new();
        rowNotContainImovableTile = new();
        columnNotContainImovableTile = new();
        for (int y = 0; y < gridSize.y; y++)
        {
            rowNotContainImovableTile.Add(y);
        }
        for (int x = 0; x < gridSize.x; x++)
        {
            columnNotContainImovableTile.Add(x);
        }

        tilesBeingDestroyed = new();
        tileInfoIdPool = new();

        executeMatchDelay = new WaitForSeconds(matchingDelayTime);
    }

    public void MakeGrid()
    {
        //Array.Clear(tiles, 0, tiles.Length);
        Vector3 spawnPosition;
        for (int column = 0; column < gridSize.x; ++column)
        {
            for (int row = 0; row < gridSize.y; ++row)
            {
                int infoId = GetRandomTileInfoId();
                int gridAddress = new();
                GetGridAddressWithOffset(0, new(row, column), ref gridAddress);
                spawnPosition = GetWorldPosFromGridAddress(gridAddress);

                tiles[gridAddress] = CreateTile(infoId, spawnPosition, gridAddress);
            }
        }

        for (int i = 0; i < initialPoolElementNum; i++)
        {
            float stored = tileInfoArr[1].probability;
            tileInfoArr[1].probability = 0;
            tileInfoIdPool.Add(GetRandomTileInfoId());
            tileInfoArr[1].probability = stored;
        }

        StartCoroutine(DelayExecuteMatch());
    }

    public void OnTileWasSelected(GameTile tile)
    {
        // Can't select tiles while tiles are animating/moving, or game is not active.
        if (movingTiles.Count > 0 || tilesBeingDestroyed.Count > 0 || isExecuteMatchDelay || !GameManager.Instance.IsGameActive() || gameMode.IsGameOver())
        {
            return;
        }

        StartCoroutine(HandleInput(tile));
    }

    public void OnTileFinishedMoving(GameTile tile, int landingGridAddress)
    {
        int returnGridAddress = new();

        // Remove the tile from its original position if it's still there (hasn't been replaced by another moving tile).
        if (GetGridAddressWithOffset(tile.GetAddress(), Vector2Int.zero, ref returnGridAddress))
        {
            if (tiles[returnGridAddress] == tile)
            {
                Array.Clear(tiles, returnGridAddress, 0);
            }
        }

        // This tile is no longer moving, remove it from the list.
        movingTiles.Remove(tile);

        // Validate new grid address and replace whatever is there.
        if (GetGridAddressWithOffset(landingGridAddress, Vector2Int.zero, ref returnGridAddress))
        {
            tiles[returnGridAddress] = tile;
            tile.SetGridAddress(returnGridAddress);
        }
        else
        {
            tile.DestroyTile();     // this tile is off the grid so we destroy it here..
        }

        if (movingTiles.Count == 0)
        {
            // Done with all moving tiles. Start matching process.
            List<int> matchedAddresses = FindMatchedAddresses().ToList();

            if (matchedAddresses.Count >= minimumMatchedCount)
            {
                lastMoveType = TileMoveType.Point;
                OnMoveMade(lastMoveType);
                ExecuteMatch(matchedAddresses);
            }
            else if (tileInfoIdPool.Count <= 0)
            {
                gameMode.GameOver();
            }
            else
            {
                lastMoveType = TileMoveType.MoveSuccess;
                OnMoveMade(lastMoveType);
            }
        }
    }

    public void ExecuteMatch(List<int> matchedAddresses)
    {
        if (matchedAddresses.Count < minimumMatchedCount)
        {
            return;
        }

        // Add to pool
        int gain = matchedAddresses.Count / 2;
        if (tileInfoIdPool.Count + gain > poolSize) gain = poolSize - tileInfoIdPool.Count;
        for (int i = 0; i < gain; i++)
        {
            float stored = tileInfoArr[1].probability;
            tileInfoArr[1].probability = 0;
            tileInfoIdPool.Add(GetRandomTileInfoId());
            tileInfoArr[1].probability = stored;
        }

        lastMatchedTilesCount = matchedAddresses.Count;

        onExecuteMatch?.Invoke(matchedAddresses);

        foreach (var address in matchedAddresses)
        {
            tiles[address].State = TileState.PendingDelete;
            tilesBeingDestroyed.Add(tiles[address]);
            tiles[address].OnMatched(lastMoveType);
        }
    }

    public void OnTileFinishedMatching(GameTile tile)
    {
        int gridAdress = tile.GetAddress();
        Array.Clear(tiles, gridAdress, 0);
        tiles[gridAdress] = CreateTile(GetRandomTileInfoId(), tile.transform.position, gridAdress);

        tilesBeingDestroyed.Remove(tile);
        tile.DestroyTile();

        if (tilesBeingDestroyed.Count == 0)
        {
            // Add score based on tile count.
            {
                int scoreMult = GetScoreMultiplierForMove(lastMoveType);
                gameMode.AddScore(lastMatchedTilesCount * scoreMult);
            }

            if (lastMoveType == TileMoveType.Bomb)
            {
                lastMoveType = TileMoveType.Combo;
                ++combos;
                OnMoveMade(lastMoveType);

                HashSet<int> explodedTileAddress = new();
                foreach (var bombTile in bombsBeingExplode)
                {
                    GetExplosionAddresses(bombTile, ref explodedTileAddress);
                }

                bombsBeingExplode.Clear();

                StartCoroutine(DelayExecuteMatch(explodedTileAddress.ToList()));
            }
            else
                // Start matching again with some delay
                StartCoroutine(DelayExecuteMatch());
        }
    }

    public GameTile GetTileFromGridAddress(int gridAddress)
    {
        return tiles[gridAddress];
    }

    public Vector3 GetWorldPosFromGridAddress(int gridAddress)
    {
        Vector3 center = transform.position;
        Vector3 outLocation = new(-(gridSize.x * 0.5f) * tileSize.x + (tileSize.x * 0.5f), -(gridSize.y * 0.5f) * tileSize.y + (tileSize.y * 0.5f), 0.0f);
        outLocation.x += tileSize.x * (float)(gridAddress % gridSize.x);
        outLocation.y += tileSize.y * (float)(gridAddress / gridSize.x);
        outLocation += center;
        outLocation.z = -gridAddress;

        return outLocation;
    }

    // Get the world location for a grid address relative to another grid address. Offset between addresses is measured in tiles.
    public Vector3 GetWorldPosFromGridAddressWithOffset(int gridAddress, Vector2Int gridOffset)
    {
        Vector3 outLocation = GetWorldPosFromGridAddress(gridAddress);
        outLocation.x += tileSize.x * (float)(gridOffset.x);
        outLocation.y += tileSize.y * (float)(gridOffset.y);
        return outLocation;
    }

    // return false if the returnGridAddress is off the grid and returnGridAddress = -1
    public bool GetGridAddressWithOffset(int initialGridAddress, Vector2Int gridOffset, ref int returnGridAddress)
    {
        int newAxisValue;
        bool isNotOffTheGrid = true;

        // Initialize to an invalid address.
        returnGridAddress = -1;

        // Check for going off the grid in the X direction.
        newAxisValue = (initialGridAddress % gridSize.x) + gridOffset.x;
        if (newAxisValue != Mathf.Clamp(newAxisValue, 0, (gridSize.x - 1)))
        {
            isNotOffTheGrid = false;
        }

        // Check for going off the grid in the Y direction.
        newAxisValue = (initialGridAddress / gridSize.x) + gridOffset.y;
        if (newAxisValue != Mathf.Clamp(newAxisValue, 0, (gridSize.y - 1)))
        {
            isNotOffTheGrid = false;
        }

        if (isNotOffTheGrid)
            returnGridAddress = (initialGridAddress + gridOffset.x + (gridOffset.y * gridSize.x));

        return isNotOffTheGrid;
    }

    public void GetExplosionAddresses(GameTile bomb, ref HashSet<int> explodedTileAddresses)
    {
        bomb.State = TileState.PendingDelete;
        int address = bomb.GetAddress();
        explodedTileAddresses.Add(address);
        Vector2Int address2D = Convert1DTo2DGridAddress(address);

        // Top
        var topLeft = new Vector2Int(address2D.x - 1, address2D.y + 1);
        int topLeft1D = Convert2DTo1DGridAddress(topLeft);
        if (!IsOffTheGrid(topLeft))
        {
            if (tiles[topLeft1D].abilities.CanExplodes && !explodedTileAddresses.Contains(topLeft1D)) GetExplosionAddresses(tiles[topLeft1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(topLeft1D);
        }

        var top = new Vector2Int(address2D.x, address2D.y + 1);
        int top1D = Convert2DTo1DGridAddress(top);
        if (!IsOffTheGrid(top))
        {
            if (tiles[top1D].abilities.CanExplodes && !explodedTileAddresses.Contains(top1D)) GetExplosionAddresses(tiles[top1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(top1D);
        }

        var topRight = new Vector2Int(address2D.x + 1, address2D.y + 1);
        int topRight1D = Convert2DTo1DGridAddress(topRight);
        if (!IsOffTheGrid(topRight))
        {
            if (tiles[topRight1D].abilities.CanExplodes && !explodedTileAddresses.Contains(topRight1D)) GetExplosionAddresses(tiles[topRight1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(topRight1D);
        }

        // Mid
        var midLeft = new Vector2Int(address2D.x - 1, address2D.y);
        int midLeft1D = Convert2DTo1DGridAddress(midLeft);
        if (!IsOffTheGrid(midLeft))
        {
            if (tiles[midLeft1D].abilities.CanExplodes && !explodedTileAddresses.Contains(midLeft1D)) GetExplosionAddresses(tiles[midLeft1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(midLeft1D);
        }

        var midRight = new Vector2Int(address2D.x + 1, address2D.y);
        int midRight1D = Convert2DTo1DGridAddress(midRight);
        if (!IsOffTheGrid(midRight))
        {
            if (tiles[midRight1D].abilities.CanExplodes && !explodedTileAddresses.Contains(midRight1D)) GetExplosionAddresses(tiles[midRight1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(midRight1D);
        }

        // Bottom
        var bottomLeft = new Vector2Int(address2D.x - 1, address2D.y - 1);
        int bottomLeft1D = Convert2DTo1DGridAddress(bottomLeft);
        if (!IsOffTheGrid(bottomLeft))
        {
            if (tiles[bottomLeft1D].abilities.CanExplodes && !explodedTileAddresses.Contains(bottomLeft1D)) GetExplosionAddresses(tiles[bottomLeft1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(bottomLeft1D);
        }

        var bottom = new Vector2Int(address2D.x, address2D.y - 1);
        int bottom1D = Convert2DTo1DGridAddress(bottom);
        if (!IsOffTheGrid(bottom))
        {
            if (tiles[bottom1D].abilities.CanExplodes && !explodedTileAddresses.Contains(bottom1D)) GetExplosionAddresses(tiles[bottom1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(bottom1D);
        }

        var bottomRight = new Vector2Int(address2D.x + 1, address2D.y - 1);
        int bottomRight1D = Convert2DTo1DGridAddress(bottomRight);
        if (!IsOffTheGrid(bottomRight))
        {
            if (tiles[bottomRight1D].abilities.CanExplodes && !explodedTileAddresses.Contains(bottomRight1D)!) GetExplosionAddresses(tiles[bottomRight1D], ref explodedTileAddresses);
            else explodedTileAddresses.Add(bottomRight1D);
        }
    }

    public bool AreAddressesNeighbours(int gridAddressA, int gridAddressB)
    {
        //const float farthestNeighbourDistance = 1.41421356237f; // Mathf.Sqrt(2)
        //if ((Mathf.Min(gridAddressA, gridAddressB) >= 0)
        //    && (Mathf.Max(gridAddressA, gridAddressB) < (gridSize.x * gridSize.y))
        //    && (gridAddressA != gridAddressB))
        //{
        //    var gridAddressA2D = Convert1DTo2DGridAddress(gridAddressA);
        //    var gridAddressB2D = Convert1DTo2DGridAddress(gridAddressB);
        //    return Vector2Int.Distance(gridAddressA2D, gridAddressB2D) <= farthestNeighbourDistance;
        //}

        Vector2Int gridAddressA2D = Convert1DTo2DGridAddress(gridAddressA);
        Vector2Int gridAddressB2D = Convert1DTo2DGridAddress(gridAddressB);

        Vector2Int top = new(gridAddressA2D.x, gridAddressA2D.y + 1);
        int top1D = Convert2DTo1DGridAddress(top);
        if (!IsOffTheGrid(top)) if (top1D == gridAddressB) return true;
        Vector2Int down = new(gridAddressA2D.x, gridAddressA2D.y - 1);
        int down1D = Convert2DTo1DGridAddress(down);
        if (!IsOffTheGrid(down)) if (down1D == gridAddressB) return true;
        Vector2Int left = new(gridAddressA2D.x - 1, gridAddressA2D.y);
        int left1D = Convert2DTo1DGridAddress(left);
        if (!IsOffTheGrid(left)) if (left1D == gridAddressB) return true;
        Vector2Int right = new(gridAddressA2D.x + 1, gridAddressA2D.y);
        int right1D = Convert2DTo1DGridAddress(right);
        if (!IsOffTheGrid(right)) if (right1D == gridAddressB) return true;

        return false;
    }

    public void SetGameMode(in GameMode gameMode)
    {
        this.gameMode = gameMode;
    }

    public int GetRandomTileInfoId()
    {
        float normalizingFactor = 0;
        foreach (var setting in tileInfoArr)
        {
            normalizingFactor += setting.probability;
        }
        float TestNumber = UnityEngine.Random.Range(0.0f, normalizingFactor);
        float CompareTo = 0;
        for (int id = 0; id != tileInfoArr.Length; id++)
        {
            CompareTo += tileInfoArr[id].probability;
            if (TestNumber <= CompareTo)
            {
                return id;
            }
        }

        return 0;
    }

    private GameTile CreateTile(int infoId, Vector3 worldPos, int gridAddress, bool playSpawnEffect = true)
    {
        GameObject prefab = tileInfoArr[infoId].tilePrefab;
        var tileGObj = Instantiate(prefab);
        tileGObj.transform.position = worldPos;
        //tileGObj.transform.localScale = new(tileSize.x / 0.32f, tileSize.y / 0.32f, 0);
        var tile = tileGObj.GetComponent<GameTile>();
        tile.Init(this, gridAddress, infoId, playSpawnEffect);

        return tile;
    }

    private HashSet<int> FindMatchedAddresses()
    {
        HashSet<int> matchedAddresses = new();

        foreach (var tile in tiles)
        {
            Vector2Int adress2D = Convert1DTo2DGridAddress(tile.GetAddress());

            Vector2Int top = new(adress2D.x + 1, adress2D.y);
            Vector2Int topRight = new(adress2D.x + 1, adress2D.y + 1);
            Vector2Int right = new(adress2D.x, adress2D.y + 1);
            if (IsOffTheGrid(top) || IsOffTheGrid(topRight) || IsOffTheGrid(right))
                continue;

            int top1D = Convert2DTo1DGridAddress(top);
            int topRight1D = Convert2DTo1DGridAddress(topRight);
            int right1D = Convert2DTo1DGridAddress(right);

            // Imovable tile can not match
            if ((tiles[top1D].InfoId == 0)
            && tiles[topRight1D].InfoId == 0
            && tiles[right1D].InfoId == 0
            && tile.InfoId == 0)
            {
                continue;
            }
            // TNT tile can not match
            if ((tiles[top1D].InfoId == 1)
            && tiles[topRight1D].InfoId == 1
            && tiles[right1D].InfoId == 1
            && tile.InfoId == 1)
            {
                continue;
            }

            if (tiles[top1D].InfoId == tile.InfoId
                && tiles[topRight1D].InfoId == tile.InfoId
                && tiles[right1D].InfoId == tile.InfoId)
            {
                matchedAddresses.Add(tile.GetAddress());
                matchedAddresses.Add(top1D);
                matchedAddresses.Add(topRight1D);
                matchedAddresses.Add(right1D);
            }
        }

        return matchedAddresses;
    }

    private bool IsOffTheGrid(Vector2Int inAddress)
    {
        return !(inAddress.x >= 0 && inAddress.y >= 0 && inAddress.x < gridSize.x && inAddress.y < gridSize.y);
    }

    // Play effects when a move is made. Use this to avoid spamming sounds on tiles.
    private void OnMoveMade(TileMoveType moveType)
    {
        // Play effect
        switch (moveType)
        {
            case TileMoveType.None:
                break;

            case TileMoveType.MoveFailure:
                break;

            case TileMoveType.MoveSuccess:
                if (++moveSuccessCount == imovablePoint)
                {
                    moveSuccessCount = 0;

                    MakeRandomTileBecomesImovable();
                }
                break;

            case TileMoveType.Point:
                moveSuccessCount = 0;
                break;

            case TileMoveType.Combo:

                break;

            case TileMoveType.Bomb:
                break;

            default:
                break;
        }
    }

    private void MakeRandomTileBecomesImovable()
    {
        int address = UnityEngine.Random.Range(0, tiles.Length);
        var tile = tiles[address];
        while (!tile.abilities.CanMove)
        {
            address = UnityEngine.Random.Range(0, tiles.Length);
            tile = tiles[address];
        }
        var address2D = Convert1DTo2DGridAddress(address);
        rowNotContainImovableTile.Remove(address2D.y);
        columnNotContainImovableTile.Remove(address2D.x);
        //Array.Clear(tiles, address, 1);
        //tiles[address] = CreateTile(0, tile.transform.position, tile.GetAddress());
        //tile.DestroyTile();
        tile.BecomesImovable();

        if (rowNotContainImovableTile.Count == 0 && columnNotContainImovableTile.Count == 0)
            gameMode.GameOver();
    }

    //private void Update()
    //{
    //    Debug.Log("s");
    //}

    private IEnumerator HandleInput(GameTile clickedTile)
    {
        Vector3 startClickPos = Input.mousePosition;
        Vector2Int swipeDir = Vector2Int.zero;
        float swipeLength = 0;
        GameTile newTile = CreateTile(tileInfoIdPool[0], Vector3.zero, -1, false);
        var newTileMesh = newTile.transform.Find("Tile Mesh");
        Vector3 newTileMeshLocalPosOrigin = newTileMesh.localPosition;
        newTileMesh.localPosition = newTileMesh.localPosition - Vector3.forward; // make this tile appear infront
        Animator animator = newTile.GetComponent<Animator>();
        animator.enabled = false;
        //newTile.gameObject.name = "new tile";
        bool swipeDirectionConfirmed = false;
        bool cancel = false;

        // yield
        while (true)
        {
            if (Input.GetMouseButtonUp(0))
            {
                cancel = true;
                lastMoveType = TileMoveType.None;
                OnMoveMade(lastMoveType);
                break;
            }

            swipeLength = Vector3.Distance(startClickPos, Input.mousePosition);

            float scalar = Mathf.Lerp(0, 1, swipeLength / confirmSwipeDirLength);
            newTile.transform.localScale = Vector3.one * scalar;

            if (swipeLength >= confirmMoveTileLength && swipeDirectionConfirmed)
            {
                movingTiles = GetMovingTiles(clickedTile, swipeDir);
                if (movingTiles.Count > 0)
                {
                    newTile.SetGridAddress(movingTiles[0].GetAddress());
                    newTile.StartMoving(Vector2Int.zero);

                    for (int i = 0; i < movingTiles.Count - 1; i++)
                    {
                        movingTiles[i].StartMoving(swipeDir);
                    }

                    Vector3 dest = GetWorldPosFromGridAddress(movingTiles[^1].GetAddress())
                        + new Vector3(tileSize.x * swipeDir.x, tileSize.y * swipeDir.y, 0);
                    movingTiles[^1].StartMoving(dest, 0);
                    movingTiles.Add(newTile);

                    tileInfoIdPool.Remove(tileInfoIdPool[0]);
                    newTile.transform.localScale = Vector3.one;
                }
                else
                {
                    lastMoveType = TileMoveType.MoveFailure;
                    OnMoveMade(lastMoveType);

                    cancel = true;
                }
                newTileMesh.localPosition = newTileMeshLocalPosOrigin;
                break;
            }
            else if (swipeLength >= confirmSwipeDirLength && !swipeDirectionConfirmed)
            {
                swipeDirectionConfirmed = true;
            }
            else
            {
                swipeDir = Vector2Int.FloorToInt(Input.mousePosition - startClickPos);
                if (Mathf.Abs(swipeDir.x) > Mathf.Abs(swipeDir.y))
                    swipeDir = new((int)Mathf.Sign(swipeDir.x), 0); // Move horizontal
                else
                    swipeDir = new(0, (int)Mathf.Sign(swipeDir.y)); // Move Verticle

                int clickedTileAddress = clickedTile.GetAddress();
                Vector2Int gridAddress2D = Convert1DTo2DGridAddress(clickedTileAddress);

                newTile.transform.position = GetWorldPosFromGridAddress(Convert2DTo1DGridAddress(GridEdgeIntersect(gridAddress2D, swipeDir * -1)))
                    + new Vector3(tileSize.x * -swipeDir.x, tileSize.y * -swipeDir.y, 0);
                swipeDirectionConfirmed = false;
            }

            yield return null;
        }

        animator.enabled = true;
        if (cancel) newTile.DestroyTile();
    }

    private List<GameTile> GetMovingTiles(GameTile clickedTile, Vector2Int swipeDir)
    {
        List<GameTile> tiles = new();

        Vector2Int startGridAddress2D = GridEdgeIntersect(Convert1DTo2DGridAddress(clickedTile.GetAddress()), swipeDir * -1);
        Vector2Int endGridAddress2D = GridEdgeIntersect(startGridAddress2D, swipeDir);
        Vector2Int gridAddress2D = startGridAddress2D;

        // while the 2d grid address is not off the grid
        while (gridAddress2D != endGridAddress2D + swipeDir)
        {
            GameTile gameTile = GetTileFromGridAddress(Convert2DTo1DGridAddress(gridAddress2D));
            if (gameTile.abilities.CanMove)
            {
                tiles.Add(gameTile);
            }
            else
            {
                tiles.Clear();
                break;
            }
            gridAddress2D += swipeDir;
        }

        return tiles;
    }

    private Vector2Int GridEdgeIntersect(Vector2Int point, Vector2Int direction)
    {
        if (direction == Vector2Int.up)
        {
            Vector2Int onNormal = (gridSize - Vector2Int.one) - Vector2Int.up * (gridSize.y - 1);
            Vector2Int v = point - (Vector2Int.up * (gridSize.y - 1));
            Vector3 projection = Vector3.Project((Vector3Int)v, (Vector3Int)onNormal);
            return Vector2Int.FloorToInt(projection) + Vector2Int.up * (gridSize.y - 1);
        }
        else if (direction == Vector2Int.down)
        {
            Vector2Int onNormal = (gridSize.x - 1) * Vector2Int.right;
            Vector2Int v = point;
            Vector3 projection = Vector3.Project((Vector3Int)v, (Vector3Int)onNormal);
            return Vector2Int.FloorToInt(projection);
        }
        else if (direction == Vector2Int.left)
        {
            Vector2Int onNormal = (gridSize.y - 1) * Vector2Int.up;
            Vector2Int v = point;
            Vector3 projection = Vector3.Project((Vector3Int)v, (Vector3Int)onNormal);
            return Vector2Int.FloorToInt(projection);
        }
        else if (direction == Vector2Int.right)
        {
            Vector2Int onNormal = (gridSize - Vector2Int.one) - Vector2Int.right * (gridSize.x - 1);
            Vector2Int v = point - (Vector2Int.right * (gridSize.x - 1));
            Vector3 projection = Vector3.Project((Vector3Int)v, (Vector3Int)onNormal);
            return Vector2Int.FloorToInt(projection) + Vector2Int.right * (gridSize.x - 1);
        }
        else
        {
            return Vector2Int.one * -1; ;
        }
    }

    private Vector2Int Convert1DTo2DGridAddress(int addressIn1D)
    {
        return new(addressIn1D % gridSize.x, addressIn1D / gridSize.x);
    }

    private int Convert2DTo1DGridAddress(Vector2Int addressIn2D)
    {
        return (addressIn2D.y * gridSize.y) + addressIn2D.x; ;
    }

    private int GetScoreMultiplierForMove(TileMoveType moveType)
    {
        return moveType switch
        {
            TileMoveType.None => 100,// Default value of 100 points per action.
            TileMoveType.MoveFailure => 100,// Default value of 100 points per action.
            TileMoveType.Point => 100,// Default value of 100 points per action.
            TileMoveType.Combo => 100,// Default value of 100 points per action.
            TileMoveType.Bomb => 100,// Default value of 100 points per action.
            _ => 100,
        };
    }

    private IEnumerator DelayExecuteMatch(List<int> matchedAddress = null)
    {
        isExecuteMatchDelay = true;
        yield return executeMatchDelay;

        matchedAddress ??= FindMatchedAddresses().ToList();

        if (matchedAddress.Count >= minimumMatchedCount)
        {
            ++combos;
            lastMoveType = TileMoveType.Combo;
            ExecuteMatch(matchedAddress);
        }
        else
        {
            combos = 1;
        }
        isExecuteMatchDelay = false;
    }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
}

[Serializable]
public struct TileInfo
{
    public GameObject tilePrefab;
    public float probability;
}

[Serializable]
public class OnExecuteMatch : UnityEvent<List<int>>
{
}

[Serializable]
public class OnPoolCountChange : UnityEvent<Queue<int>>
{
}