using UnityEngine;
public class MiniMap : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MapConfig config = new MapConfig();
    private Terrain terrain;
    private MapData data;
    private GUIStyle headerStyle;
    private float pulse;
    private void Start()
    {
        terrain = Terrain.activeTerrain;
        if (terrain == null)
            return;
        data = new MapData();
        data.Initialize(terrain);
        Invoke(nameof(LoadData), 0.5f);
    }
    private void LoadData()
    {
        MultiRobotSpawner spawner = FindFirstObjectByType<MultiRobotSpawner>();
        data.LoadRobots(spawner);
        FenceGenerator fence = FindFirstObjectByType<FenceGenerator>();
        data.LoadZones(fence);
        if (BuildingSpawner.Instance != null)
            data.LoadBuildings(BuildingSpawner.Instance);
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
        MapHelper.DrawBox(mapRect, MapColors.Background);
        MapHelper.DrawBox(headerRect, MapColors.HeaderBackground);
        GUI.Label(headerRect, "MINIMAP", headerStyle);
    }
    private void DrawContent(Rect mapRect, Event e)
    {
        DrawZones(mapRect);
        DrawGrid(mapRect);
        DrawBuildings(mapRect, e);
        DrawRobots(mapRect, e);
    }
    private void DrawBorders(Rect mapRect, Rect headerRect)
    {
        MapHelper.DrawBorder(mapRect, MapColors.Border, config.borderWidth);
        MapHelper.DrawBorder(headerRect, MapColors.Border, 2);
    }
    private void DrawGrid(Rect m)
    {
        GUI.color = MapColors.Grid;
        float centerX = m.x + m.width * 0.5f - 1;
        GUI.DrawTexture(new Rect(centerX, m.y, 2, m.height), MapHelper.Tex);
        for (int i = 1; i <= config.gridLines; i++)
        {
            float yPos = m.y + m.height * i / (config.gridLines + 1);
            GUI.DrawTexture(new Rect(m.x, yPos, m.width, 1), MapHelper.Tex);
        }
        GUI.color = Color.white;
    }
    private void DrawZones(Rect m)
    {
        if (data.zones == null)
            return;
        for (int i = 0; i < data.zones.Length; i++)
        {
            if (data.zones[i] == null)
                continue;
            Color zoneColor = MapColors.GetZoneColor(i);
            MiniMapRenderer.DrawZone(m, data.zones[i].startXZ, data.zones[i].endXZ, data.inverseX, data.inverseZ, zoneColor);
        }
    }
    private void DrawBuildings(Rect m, Event e)
    {
        for (int i = 0; i < data.buildings.Count; i++)
        {
            MiniMapRenderer.DrawBuilding(m, data.buildings[i], data.terrainPosition, data.inverseX, data.inverseZ, MapColors.Building, e);
        }
    }
    private void DrawRobots(Rect m, Event e)
    {
        for (int i = 0; i < data.robots.Count; i++)
        {
            if (data.robots[i] == null)
                continue;
            Color robotColor = MapColors.GetRobotColor(data.robots[i].name);
            bool isSelected = (i == data.selectedRobotIndex);
            bool clicked = MiniMapRenderer.DrawRobot(
                m,
                data.robots[i].position,
                data.terrainPosition,
                data.inverseX,
                data.inverseZ,
                robotColor,
                config.robotSize,
                isSelected,
                pulse,
                e
            );
            if (clicked)
                SelectRobot(i);
        }
    }
    private void SelectRobot(int index)
    {
        data.selectedRobotIndex = index;
        RobotCamera cam = FindFirstObjectByType<RobotCamera>();
        if (cam != null)
            cam.target = data.robots[index];
    }
}
