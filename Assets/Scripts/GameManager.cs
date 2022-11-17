using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public Level0Manger LevelManager => levelMgr;

    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private GameObject[] levelPrefabs;

    private GameObject currentLevel;
    private PlayerController playerController;
    private int currentLevelId = 0;
    private bool isGameActive = false;
    private Level0Manger levelMgr;

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

    public void StartLevel(int id)
    {
        if (currentLevel != null)
        {
            PlayerPrefs.SetString("CellStates", "");
            PlayerPrefs.SetString("PoolState", "");
            DestroyImmediate(currentLevel); // This work like a charm
            //Destroy(currentLevel);        // This doesn't work
            //Destroy(currentLevel, 0);     // Does not work also
        }
        currentLevelId = id;
        currentLevel = Instantiate(levelPrefabs[currentLevelId]);
        levelMgr = currentLevel.GetComponent<Level0Manger>();
        currentLevel.transform.SetParent(transform);
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    private void Start()
    {
        playerController = Instantiate(playerControllerPrefab).GetComponent<PlayerController>();
        StartLevel(0);
        isGameActive = true;
    }

    private void OnDestroy()
    {
        if (currentLevel != null)
            currentLevel.SaveState();
    }
}