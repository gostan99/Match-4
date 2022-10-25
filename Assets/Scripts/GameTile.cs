using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class GameTile : MonoBehaviour
{
    public TileState State { get; set; }
    public int InfoId => infoId;
    public TileAbilities abilities;
    public bool isPoolDisplay = false;

    [SerializeField] private int infoId;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float brightnessTarget;
    [SerializeField] private GameObject[] fx;
    [SerializeField] private GameObject tileMesh;
    [SerializeField] private GameObject tileMeshClone;

    private GameGrid grid;
    private int address;
    private int landingAddress;
    private Coroutine movingCoroutine;
    private BoxCollider boxCollider;
    private Animator matchAnim;
    private Transform wickSparcles;
    private float matchAnimLength;
    private Material tileMeshCloneMaterial;

    public void Init(GameGrid grid, int address, int infoId, bool playSpawnEffect = true)
    {
        this.grid = grid;
        this.address = address;
        this.infoId = infoId;
        boxCollider = GetComponent<BoxCollider>();
        matchAnim = GetComponent<Animator>();
        tileMeshCloneMaterial = tileMeshClone.GetComponent<Renderer>().material;
        tileMeshClone.SetActive(false);

        if (abilities.CanExplodes)
        {
            wickSparcles = transform.Find("Wick Sparcles FX");
            wickSparcles.gameObject.SetActive(false);
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
        StartCoroutine(WaitFor(0.3f, () => PlayMatchEffect(() => grid.OnTileFinishedMatching(this))));
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
    public void StartMoving(Vector3 dest, float scaleTarget = 1)
    {
        State = TileState.Moving;
        landingAddress = -1;
        movingCoroutine = StartCoroutine(Moving(dest, scaleTarget));
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

    public void BecomesImovable()
    {
        tileMesh.SetActive(false);
        tileMeshClone.SetActive(true);
        abilities.CanMove = true;
        StartCoroutine(BecomesImovableFX());
    }

    public void DestroyTile()
    {
        boxCollider.enabled = false;
        tileMesh.SetActive(false);
        tileMeshClone.SetActive(false);
        transform.Find("Wick Sparcles FX")?.gameObject.SetActive(false);
        if (abilities.CanExplodes)
            grid.onExecuteMatch.RemoveListener(OnExecuteMatch);
        Destroy(gameObject, 2f);
    }

    private IEnumerator BecomesImovableFX()
    {
        float o_brightness;
        Color.RGBToHSV(tileMeshCloneMaterial.color, out float h, out float s, out o_brightness);

        float time = 0.3f;
        float timer = 0;
        float brightness;

        while (timer < time)
        {
            brightness = Mathf.Lerp(o_brightness, brightnessTarget, timer / time);
            tileMeshCloneMaterial.SetColor("_BaseColor", Color.HSVToRGB(h, s, brightness));

            timer += Time.deltaTime;
            yield return null;
        }
        tileMeshCloneMaterial.SetColor("_BaseColor", Color.HSVToRGB(h, s, brightnessTarget));
    }

    private void OnExecuteMatch(List<int> matchedAddress)
    {
        foreach (var address in matchedAddress)
        {
            if (State == TileState.PendingDelete) break;
            if (grid.AreAddressesNeighbours(this.address, address))
            {
                wickSparcles.gameObject.SetActive(true);
                grid.lastMoveType = TileMoveType.Bomb;
                grid.bombsBeingExplode.Add(this);
                break;
            }
        }
    }

    private void PlayMatchEffect(Action callback)
    {
        matchAnim.Play("StartAnimation");
        if (wickSparcles is not null) wickSparcles.gameObject.SetActive(true);
        StartCoroutine(WaitFor(1, callback));
    }

    private void PlaySpawnEffect()
    {
        //StartCoroutine(WaitFor(1));
        StartCoroutine(SpawnEffect(.3f));
    }

    private IEnumerator SpawnEffect(float time)
    {
        float timer = 0;
        float scalar = 0;
        while (scalar < 1)
        {
            scalar = timer / time;
            transform.localScale = Vector3.one * scalar;
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one;

        foreach (var f in fx)
        {
            f.SetActive(true);
        }
        matchAnim.enabled = true;
    }

    private void PlayStartMovingEffect()
    {
        StartCoroutine(WaitFor(1.0f));
    }

    private void PlayStopMovingEffect()
    {
        StartCoroutine(WaitFor(1.0f));
    }

    private void PlayDestroyEffect(Action callback)
    {
        StartCoroutine(WaitFor(.0f, callback));
    }

    private IEnumerator Moving(Vector3 dest, float scaleTarget = 1)
    {
        Vector3 start = transform.position;
        Vector3 dir = (dest - transform.position).normalized;
        Vector3 scaleOrigin = transform.Find("Tile Mesh").localScale;
        float totalDistance = Vector3.Distance(start, dest);
        if (scaleTarget != 1) matchAnim.enabled = false;
        while (true)
        {
            transform.position += moveSpeed * Time.deltaTime * dir;
            if (scaleTarget != 1)
            {
                float scalar = Mathf.Lerp(1, scaleTarget, Vector3.Distance(transform.position, start) / totalDistance);
                transform.localScale = scaleOrigin * scalar;
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

    private IEnumerator WaitFor(float time, Action callback = null)
    {
        float timer = 0;
        while (timer <= time)
        {
            timer += Time.deltaTime;
            yield return null;
        }

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
    public bool CanMove { get => !preventMoving; set { preventMoving = value; } }
    public bool CanExplodes { get => canExplodes; private set { } }

    [SerializeField] private bool preventMoving;
    [SerializeField] private bool canExplodes;
}