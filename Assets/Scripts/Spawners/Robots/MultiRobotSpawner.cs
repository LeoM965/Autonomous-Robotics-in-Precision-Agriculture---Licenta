using UnityEngine;
using System.Collections.Generic;

public class MultiRobotSpawner : MonoBehaviour
{
    public static MultiRobotSpawner Instance;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    [Header("Robot Prefabs")]
    public List<GameObject> robotPrefabs = new List<GameObject>();

    [Header("Spawn Configuration")]
    [SerializeField] private SpawnConfig config = new SpawnConfig();

    [Header("References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private RobotCamera robotCamera;
    [SerializeField] private Transform container;

    [Header("Road Detection")]
    [SerializeField] private TerrainLayer roadLayer;

    // Flat list for external access (MiniMap, Camera, etc.)
    private List<GameObject> spawnedRobots = new List<GameObject>();

    // Per-type, per-zone tracking: robotGrid[typeIdx][zoneIdx] = list of robots
    private List<GameObject>[][] robotGrid;

    private List<FenceZone> validZones = new List<FenceZone>();
    private SpawnValidator validator;
    private SpawnPositionFinder positionFinder;

    public List<GameObject> GetRobots() => spawnedRobots;

    private void OnEnable() => Settings.SimulationSettings.OnSettingsChanged += ApplyChanges;
    private void OnDisable() => Settings.SimulationSettings.OnSettingsChanged -= ApplyChanges;

    private void Start()
    {
        Initialize();
        InitializeRobotSettings();
        SpawnAllRobots();
        SetupCamera();
    }

    private void Initialize()
    {
        if (container == null)
            container = new GameObject("Robots").transform;
        validator = new SpawnValidator(terrain, roadLayer, config.minRoadWeight);
        positionFinder = new SpawnPositionFinder(validator, config.spacing, config.maxAttempts);
        CollectValidZones();
    }

    private void InitializeRobotSettings()
    {
        string[] typeNames = new string[robotPrefabs.Count];
        for (int i = 0; i < robotPrefabs.Count; i++)
            typeNames[i] = robotPrefabs[i] != null ? robotPrefabs[i].name : $"Robot {i}";
        Settings.SimulationSettings.InitRobotCounts(robotPrefabs.Count, validZones.Count, typeNames);
        InitGrid();
    }

    private void InitGrid()
    {
        int types = robotPrefabs.Count;
        int zones = validZones.Count;
        robotGrid = new List<GameObject>[types][];
        for (int t = 0; t < types; t++)
        {
            robotGrid[t] = new List<GameObject>[zones];
            for (int z = 0; z < zones; z++)
                robotGrid[t][z] = new List<GameObject>();
        }
    }

    private void ApplyChanges()
    {
        if (robotGrid == null) return;

        bool changed = false;
        for (int t = 0; t < robotPrefabs.Count; t++)
        {
            if (robotPrefabs[t] == null) continue;
            for (int z = 0; z < validZones.Count; z++)
            {
                int desired = Settings.SimulationSettings.GetCountForTypeZone(t, z);
                int current = robotGrid[t][z].Count;

                if (desired > current)
                {
                    // Spawn only the new ones
                    for (int i = 0; i < desired - current; i++)
                    {
                        Vector3 pos = positionFinder.FindInZone(validZones[z], config.spacing);
                        GameObject robot = SpawnRobot(robotPrefabs[t], pos);
                        robotGrid[t][z].Add(robot);
                    }
                    changed = true;
                }
                else if (desired < current)
                {
                    // Remove only the excess (from the end)
                    for (int i = current - 1; i >= desired; i--)
                    {
                        GameObject robot = robotGrid[t][z][i];
                        robotGrid[t][z].RemoveAt(i);
                        spawnedRobots.Remove(robot);
                        if (robot != null) Destroy(robot);
                    }
                    changed = true;
                }
            }
        }

        if (changed) SetupCamera();
    }

    private void CollectValidZones()
    {
        FenceGenerator fenceGen = FenceGenerator.Instance;
        if (fenceGen == null) fenceGen = FindFirstObjectByType<FenceGenerator>();
        if (fenceGen == null || fenceGen.zones == null) return;
        for (int i = 0; i < fenceGen.zones.Length; i++)
            validZones.Add(fenceGen.zones[i]);
    }

    private void SpawnAllRobots()
    {
        for (int t = 0; t < robotPrefabs.Count; t++)
        {
            if (robotPrefabs[t] == null) continue;
            for (int z = 0; z < validZones.Count; z++)
            {
                int count = Settings.SimulationSettings.GetCountForTypeZone(t, z);
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = positionFinder.FindInZone(validZones[z], config.spacing);
                    GameObject robot = SpawnRobot(robotPrefabs[t], pos);
                    robotGrid[t][z].Add(robot);
                }
            }
        }
    }

    private GameObject SpawnRobot(GameObject prefab, Vector3 position)
    {
        position = TerrainHelper.GetPosition(position.x, position.z, config.heightOffset);
        Quaternion rotation = SpawnHelper.RandomYRotation();
        GameObject robot = Instantiate(prefab, position, rotation, container);
        RobotMovement movement = robot.GetComponent<RobotMovement>();
        if (movement != null)
            movement.SetTerrain(terrain);

        if (Economics.Managers.RobotEconomicsManager.Instance != null)
            Economics.Managers.RobotEconomicsManager.Instance.RegisterRobot(robot.transform);

        spawnedRobots.Add(robot);
        positionFinder.MarkUsed(position);
        return robot;
    }

    private void SetupCamera()
    {
        if (robotCamera == null)
            robotCamera = FindFirstObjectByType<RobotCamera>();
        if (robotCamera == null || spawnedRobots.Count == 0)
            return;
        robotCamera.targets.Clear();
        for (int i = 0; i < spawnedRobots.Count; i++)
            robotCamera.targets.Add(spawnedRobots[i].transform);
        robotCamera.target = spawnedRobots[0].transform;
    }
}
