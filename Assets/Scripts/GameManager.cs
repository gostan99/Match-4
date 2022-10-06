using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject playerControllerPrefab;
    [SerializeField] private GameObject[] levelPrefabs;

    private GameObject currentLevel;
    private PlayerController playerController;
    private int currentLevelId = 0;
    private bool isGameActive = false;

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
            Destroy(currentLevel);
        currentLevelId = id;
        currentLevel = Instantiate(levelPrefabs[currentLevelId]);
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
}