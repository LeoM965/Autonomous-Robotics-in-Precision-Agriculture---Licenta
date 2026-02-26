using UnityEngine;
using System.Collections.Generic;

public class MultiRobotSpawner : MonoBehaviour
{
    [Header("Robot Prefabs")]
    public GameObject robotPrefab1;
    public GameObject robotPrefab2;
    public GameObject robotPrefab3;

    [Header("Spawn Configuration")]
    [SerializeField] private SpawnConfig config = new SpawnConfig();

    [Header("References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private RobotCamera robotCamera;
    [SerializeField] private Transform container;

    [Header("Road Detection")]
    [SerializeField] private TerrainLayer roadLayer;

    private List<GameObject> spawnedRobots = new List<GameObject>();
    private List<FenceZone> validZones = new List<FenceZone>();
    private SpawnValidator validator;
    private SpawnPositionFinder positionFinder;

    public List<GameObject> GetRobots()
    {
        return spawnedRobots;
    }

    public int RobotsPerType
    {
        get { return config.countPerType; }
    }

    private void Start()
    {
        Initialize();
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

    private void CollectValidZones()
    {
        FenceGenerator fenceGen = FindFirstObjectByType<FenceGenerator>();
        if (fenceGen == null || fenceGen.zones == null)
            return;
        for (int i = 0; i < fenceGen.zones.Length; i++)
        {
            FenceZone zone = fenceGen.zones[i];
            validZones.Add(zone);
        }
    }

    private void SpawnAllRobots()
    {
        if (robotPrefab1 != null)
            SpawnInZones(robotPrefab1);
        if (robotPrefab2 != null)
            SpawnInZones(robotPrefab2);
        if (robotPrefab3 != null)
            SpawnInZones(robotPrefab3);
    }

    private void SpawnInZones(GameObject prefab)
    {
        if (validZones.Count == 0)
            return;
        for (int i = 0; i < config.countPerType; i++)
        {
            int zoneIndex = i % validZones.Count;
            FenceZone zone = validZones[zoneIndex];
            Vector3 position = positionFinder.FindInZone(zone, config.spacing);
            SpawnRobot(prefab, position);
        }
    }

    private void SpawnRobot(GameObject prefab, Vector3 position)
    {
        position = TerrainHelper.GetPosition(position.x, position.z, config.heightOffset);
        Quaternion rotation = SpawnHelper.RandomYRotation();
        GameObject robot = Instantiate(prefab, position, rotation, container);
        RobotMovement movement = robot.GetComponent<RobotMovement>();
        if (movement != null)
            movement.SetTerrain(terrain);
        spawnedRobots.Add(robot);
        positionFinder.MarkUsed(position);
    }

    private void SetupCamera()
    {
        if (robotCamera == null)
            robotCamera = FindFirstObjectByType<RobotCamera>();
        if (robotCamera == null || spawnedRobots.Count == 0)
            return;
        robotCamera.targets.Clear();
        for (int i = 0; i < spawnedRobots.Count; i++)
        {
            robotCamera.targets.Add(spawnedRobots[i].transform);
        }
        robotCamera.target = spawnedRobots[0].transform;
    }
}
