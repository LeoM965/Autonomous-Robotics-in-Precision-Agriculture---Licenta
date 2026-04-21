using UnityEngine;

public class MiniMap : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] MiniMapConfig config = new MiniMapConfig();
    [SerializeField] UITheme theme;
    [SerializeField] MapColors mapColors;

    private Terrain terrain;
    private MapData data;
    private GUIStyle headerStyle;
    private float pulse;
    private RobotCamera cachedRobotCam;

    private void Start()
    {
        terrain = Terrain.activeTerrain;
        if (terrain == null)
            return;
        data = new MapData();
        data.Initialize(terrain);
        Invoke(nameof(LoadData), 0.5f);
    }

    private void OnEnable()
    {
        Settings.SimulationSettings.OnSettingsChanged += LoadData;
    }

    private void OnDisable()
    {
        Settings.SimulationSettings.OnSettingsChanged -= LoadData;
    }

    private void LoadData()
    {
        if (data == null) return;
        MultiRobotSpawner spawner = FindFirstObjectByType<MultiRobotSpawner>();
        data.LoadRobots(spawner);
        FenceGenerator fence = FindFirstObjectByType<FenceGenerator>();
        data.LoadZones(fence);
        if (BuildingSpawner.Instance != null)
            data.LoadBuildings(BuildingSpawner.Instance);

        // Reset selection if invalid
        if (data.selectedRobotIndex >= data.robots.Count)
            data.selectedRobotIndex = data.robots.Count > 0 ? 0 : -1;
    }

    private void Update()
    {
        pulse += Time.deltaTime;
    }

    private void OnGUI()
    {
        if (terrain == null || data == null)
            return;
        InitializeStyles();
        Rect mapRect = config.GetMapRect();
        Rect headerRect = config.GetHeaderRect(mapRect);
        Event e = Event.current;
        DrawBackground(mapRect, headerRect);
        DrawContent(mapRect, e);
        DrawBorders(mapRect, headerRect);
    }

    private void InitializeStyles()
    {
        if (headerStyle != null)
            return;
        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 11;
        headerStyle.normal.textColor = Color.white;
    }

    private void DrawBackground(Rect mapRect, Rect headerRect)
    {
        MapHelper.DrawShadow(mapRect, 4);
        MapHelper.DrawBox(mapRect, mapColors.backgroundColor);
        MapHelper.DrawBox(headerRect, mapColors.headerBackgroundColor);
        GUI.Label(headerRect, "MINIMAP", headerStyle);
    }

    private void DrawContent(Rect mapRect, Event guiEvent)
    {
        DrawZones(mapRect);
        DrawGrid(mapRect);
        DrawBuildings(mapRect, guiEvent);
        DrawRobots(mapRect, guiEvent);
    }

    private void DrawBorders(Rect mapRect, Rect headerRect)
    {
        MapHelper.DrawBorder(mapRect, mapColors.borderColor, config.borderWidth);
        MapHelper.DrawBorder(headerRect, mapColors.borderColor, 2);
    }

    private void DrawGrid(Rect mapRect)
    {
        GUI.color = mapColors.gridColor;
        float centerX = mapRect.x + mapRect.width * 0.5f - 1;
        GUI.DrawTexture(new Rect(centerX, mapRect.y, 2, mapRect.height), MapHelper.WhiteTexture);
        for (int i = 1; i <= config.gridLines; i++)
        {
            float yPos = mapRect.y + mapRect.height * i / (config.gridLines + 1);
            GUI.DrawTexture(new Rect(mapRect.x, yPos, mapRect.width, 1), MapHelper.WhiteTexture);
        }
        GUI.color = Color.white;
    }

    private void DrawZones(Rect mapRect)
    {
        if (data.zones == null)
            return;
        for (int i = 0; i < data.zones.Length; i++)
        {
            if (data.zones[i] == null)
                continue;
            Color zoneColor = mapColors.GetZoneColor(i);
            MiniMapRenderer.DrawZone(mapRect, data.zones[i].startXZ, data.zones[i].endXZ, data.inverseX, data.inverseZ, zoneColor);
        }
    }

    private void DrawBuildings(Rect mapRect, Event guiEvent)
    {
        for (int i = 0; i < data.buildings.Count; i++)
        {
            MiniMapRenderer.DrawBuilding(mapRect, data.buildings[i], data.terrainPosition, data.inverseX, data.inverseZ, mapColors.buildingColor, guiEvent);
        }
    }

    private void DrawRobots(Rect mapRect, Event guiEvent)
    {
        for (int i = 0; i < data.robots.Count; i++)
        {
            if (data.robots[i] == null)
                continue;
            Color robotColor = mapColors.GetRobotColor(data.robots[i].name);
            bool isSelected = (i == data.selectedRobotIndex);
            bool clicked = MiniMapRenderer.DrawRobot(
                mapRect,
                data.robots[i].position,
                data.terrainPosition,
                data.inverseX,
                data.inverseZ,
                robotColor,
                config.robotSize,
                isSelected,
                pulse,
                guiEvent
            );
            if (clicked)
                SelectRobot(i);
        }
    }

    private void SelectRobot(int index)
    {
        data.selectedRobotIndex = index;
        if (cachedRobotCam == null)
            cachedRobotCam = FindFirstObjectByType<RobotCamera>();
        if (cachedRobotCam != null)
            cachedRobotCam.target = data.robots[index];
    }
}
