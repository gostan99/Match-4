using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTile : MonoBehaviour
{
    public TileState State { get; set; }
    public int InfoId => infoId;
    public TileAbilities abilities;
    public bool isPoolDisplay = false;

    [SerializeField] private int infoId;
    [SerializeField] private float moveSpeed;

    private GameGrid grid;
    private int address;
    private int landingAddress;
    private Coroutine movingCoroutine;
    private SpriteRenderer spriteRenderer;
    private BoxCollider boxCollider;

    public void Init(GameGrid grid, int address, int infoId, bool playSpawnEffect = true)
    {
        this.grid = grid;
        this.address = address;
        this.infoId = infoId;
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider>();

        if (abilities.CanExplodes)
        {
            grid.onExecuteMatch.AddListener(OnExecuteMatch);
        }

        if (playSpawnEffect)
            PlaySpawnEffect();
    }

    public void SetGridAddress(int gridAddress)
    {
        this.address = gridAddress;
    }

    public int GetAddress()
    {
        return address;
    }

    public void OnMatched(TileMoveType moveType)
    {
        PlayMatchEffect(() => grid.OnTileFinishedMatching(this));
    }

    // When player click on this tile
    public void OnSelect()
    {
        grid.OnTileWasSelected(this);
    }

    public void PlaySelectionEffect(bool turnEffectOn)
    {
    }

    public void StartMoving(Vector2Int gridOffset)
    {
        State = TileState.Moving;
        grid.GetGridAddressWithOffset(GetAddress(), gridOffset, ref landingAddress);
        Vector3 dest = grid.GetWorldPosFromGridAddress(landingAddress);
        movingCoroutine = StartCoroutine(Moving(dest));
        PlayStartMovingEffect();
    }

    // use this to destroy this tile after finish moving
    public void StartMoving(Vector3 dest, bool withFadeEffect)
    {
        State = TileState.Moving;
        landingAddress = -1;
        movingCoroutine = StartCoroutine(Moving(dest, withFadeEffect));
        PlayStartMovingEffect();
    }

    public void CancelMoving()
    {
        if (movingCoroutine is not null)
        {
            StopCoroutine(movingCoroutine);
            StartMoving(Vector2Int.zero);
        };
    }

    public void DestroyTile()
    {
        boxCollider.enabled = false;
        if (abilities.CanExplodes)
            grid.onExecuteMatch.RemoveListener(OnExecuteMatch);
        Destroy(gameObject);
    }

    public SpriteRenderer GetSpriteRenderer()
    {
        return spriteRenderer;
    }

    private void OnExecuteMatch(List<int> matchedAddress)
    {
        foreach (var address in matchedAddress)
        {
            if (State == TileState.PendingDelete) break;
            if (grid.AreAddressesNeighbours(this.address, address))
            {
                grid.lastMoveType = TileMoveType.Bomb;
                grid.bombsBeingExplode.Add(this);
                break;
            }
        }
    }

    private void PlayMatchEffect(Action callback)
    {
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.g, .3f);
        StartCoroutine(FakeEffect("Match Effect", 1.0f, callback));
    }

    private void PlaySpawnEffect()
    {
        StartCoroutine(FakeEffect("Spawn Effect", 1.0f));
    }

    private void PlayStartMovingEffect()
    {
        StartCoroutine(FakeEffect("Start Moving Effect", 1.0f));
    }

    private void PlayStopMovingEffect()
    {
        StartCoroutine(FakeEffect("Stop Moving Effect", 1.0f));
    }

    private void PlayDestroyEffect(Action callback)
    {
        StartCoroutine(FakeEffect("Destroy Effect", .0f, callback));
    }

    private IEnumerator Moving(Vector3 dest, bool withFadeEffect = false)
    {
        Vector3 start = transform.position;
        Vector3 dir = (dest - transform.position).normalized;
        float totalDistance = Vector3.Distance(start, dest);
        while (true)
        {
            transform.position += moveSpeed * Time.deltaTime * dir;
            if (withFadeEffect)
            {
                float alpha = Mathf.Lerp(1, 0, Vector3.Distance(transform.position, start) / totalDistance);
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.g, alpha);
            }
            if (Vector3.Dot(dest - transform.position, dir) < 0)
            {
                transform.position = dest;
                break;
            }
            yield return null;
        }
        movingCoroutine = null;
        if (!isPoolDisplay) FinishMoving();
    }

    private IEnumerator FakeEffect(string effect, float time, Action callback = null)
    {
        //float timer = 0;
        //while (timer <= time)
        //{
        //    float alpha = Mathf.Lerp(1, 0, timer / time);
        //    spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.g, alpha);

        //    timer += Time.deltaTime;
        //    yield return null;
        //}

        yield return new WaitForSeconds(time);

        callback?.Invoke();
    }

    private void FinishMoving()
    {
        State = TileState.Normal;
        PlayStopMovingEffect();
        grid.OnTileFinishedMoving(this, landingAddress);
    }
}

public enum TileState
{
    Normal,
    Moving,
    PendingDelete
}

public enum TileMoveType
{
    None,
    MoveFailure,
    MoveSuccess,
    Point,
    Combo,
    Bomb
}

[Serializable]
public struct TileAbilities
{
    public bool CanMove { get => !preventMoving; private set { } }
    public bool CanExplodes { get => canExplodes; private set { } }

    [SerializeField] private bool preventMoving;
    [SerializeField] private bool canExplodes;
}