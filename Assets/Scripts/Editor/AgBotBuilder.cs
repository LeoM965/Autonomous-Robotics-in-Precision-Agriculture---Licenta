using UnityEngine;
using UnityEditor;
using System.Reflection;
public class AgBotBuilder : EditorWindow
{
    [MenuItem("Tools/AgBot Builder Window")]
    public static void ShowWindow() => GetWindow<AgBotBuilder>("AgBot Builder").Show();
    [MenuItem("Tools/Build AgBot Now")]
    public static void QuickBuild() => Build();
    void OnGUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("AgBot 2.055 W4", new GUIStyle(GUI.skin.label)
        { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        GUILayout.Label("Autonomous Agricultural Robot", new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(20);
        if (GUILayout.Button("BUILD AGBOT", GUILayout.Height(50)))
        {
            Build();
            Close();
        }
    }
    public static void Build()
    {
        string matPath = "Assets/Materials/AgBot";
        EnsureFolder(matPath);
        GameObject root = new GameObject("AgBot");
        float groundY = GetGroundHeight(Vector3.zero);
        root.transform.position = new Vector3(0f, groundY, 0f);
        Material bodyBlack = Mat("BodyBlack", new Color(0.08f, 0.08f, 0.10f), 0.25f, 0.40f, matPath);
        Material bodyGrey = Mat("BodyGrey", new Color(0.35f, 0.35f, 0.38f), 0.30f, 0.45f, matPath);
        Material panelSilver = Mat("PanelSilver", new Color(0.72f, 0.72f, 0.76f), 0.55f, 0.60f, matPath);
        Material accentGreen = Mat("AccentGreen", new Color(0.15f, 0.85f, 0.30f), 0.60f, 0.70f, matPath);
        Material rubber = Mat("Rubber", new Color(0.06f, 0.06f, 0.06f), 0.0f, 0.08f, matPath);
        Material chrome = Mat("Chrome", new Color(0.88f, 0.88f, 0.92f), 0.92f, 0.88f, matPath);
        Material glassBlack = Mat("GlassBlack", new Color(0.02f, 0.04f, 0.06f), 0.85f, 0.95f, matPath);
        Material lightWhite = MatEmissive("LightWhite", new Color(1f, 0.98f, 0.92f), matPath);
        Material lightRed = MatEmissive("LightRed", new Color(1f, 0.15f, 0.08f), matPath);
        Material lightOrange = MatEmissive("LightOrange", new Color(1f, 0.65f, 0.0f), matPath);
        Material hazardYellow = Mat("HazardYellow", new Color(1f, 0.85f, 0.0f), 0.15f, 0.35f, matPath);
        float scaleFactor = 0.85f;
        float wheelRadius = 0.48f * scaleFactor;
        float wheelWidth = 0.36f * scaleFactor;
        float trackWidth = 0.82f * scaleFactor;
        float wheelBase = 0.92f * scaleFactor;
        float baseY = wheelRadius;
        GameObject wheels = new GameObject("Wheels");
        wheels.transform.SetParent(root.transform);
        wheels.transform.localPosition = Vector3.zero;
        CreateWheelAgBot("Wheel_FL", wheels.transform, V(-trackWidth, baseY, wheelBase), wheelRadius, wheelWidth, rubber, accentGreen, bodyBlack);
        CreateWheelAgBot("Wheel_FR", wheels.transform, V(trackWidth, baseY, wheelBase), wheelRadius, wheelWidth, rubber, accentGreen, bodyBlack);
        CreateWheelAgBot("Wheel_BL", wheels.transform, V(-trackWidth, baseY, -wheelBase), wheelRadius, wheelWidth, rubber, accentGreen, bodyBlack);
        CreateWheelAgBot("Wheel_BR", wheels.transform, V(trackWidth, baseY, -wheelBase), wheelRadius, wheelWidth, rubber, accentGreen, bodyBlack);
        GameObject chassis = new GameObject("Chassis");
        chassis.transform.SetParent(root.transform);
        chassis.transform.localPosition = Vector3.zero;
        float chassisY = baseY + 0.35f;
        CreateCube("Frame_Main", chassis.transform, V(0, chassisY, 0), V(1.0f, 0.35f, 2.3f), bodyBlack);
        CreateCube("Axle_Front", chassis.transform, V(0, baseY + 0.1f, wheelBase), V(trackWidth * 2 - 0.2f, 0.12f, 0.15f), bodyBlack);
        CreateCube("Axle_Rear", chassis.transform, V(0, baseY + 0.1f, -wheelBase), V(trackWidth * 2 - 0.2f, 0.12f, 0.15f), bodyBlack);
        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        float bodyY = chassisY + 0.25f;
        CreateCube("Hull_Main", body.transform, V(0, bodyY, 0), V(1.25f, 0.50f, 1.9f), bodyBlack);
        CreateCube("Hull_Top", body.transform, V(0, bodyY + 0.30f, -0.1f), V(1.15f, 0.10f, 1.6f), bodyBlack);
        CreateCube("Panel_L", body.transform, V(-0.64f, bodyY, 0), V(0.04f, 0.42f, 1.3f), panelSilver);
        CreateCube("Panel_R", body.transform, V(0.64f, bodyY, 0), V(0.04f, 0.42f, 1.3f), panelSilver);
        CreateCube("Stripe_L", body.transform, V(-0.665f, bodyY + 0.08f, -0.05f), V(0.012f, 0.06f, 1.1f), accentGreen);
        CreateCube("Stripe_R", body.transform, V(0.665f, bodyY + 0.08f, -0.05f), V(0.012f, 0.06f, 1.1f), accentGreen);
        GameObject frontHood = new GameObject("FrontHood");
        frontHood.transform.SetParent(body.transform);
        frontHood.transform.localPosition = V(0, bodyY + 0.25f, 0.95f);
        frontHood.transform.localRotation = Quaternion.Euler(18, 0, 0);
        CreateCube("Hood_Panel", frontHood.transform, V(0, 0, 0.22f), V(1.1f, 0.06f, 0.45f), bodyBlack);
        CreateCube("Grille_Frame", body.transform, V(0, bodyY - 0.08f, 0.96f), V(0.95f, 0.35f, 0.04f), bodyGrey);
        CreateCube("Grille_Mesh", body.transform, V(0, bodyY - 0.08f, 0.975f), V(0.85f, 0.30f, 0.02f), bodyBlack);
        GameObject rearHood = new GameObject("RearHood");
        rearHood.transform.SetParent(body.transform);
        rearHood.transform.localPosition = V(0, bodyY + 0.25f, -0.95f);
        rearHood.transform.localRotation = Quaternion.Euler(-12, 0, 0);
        CreateCube("Hood_Panel", rearHood.transform, V(0, 0, -0.22f), V(1.1f, 0.06f, 0.45f), bodyBlack);
        float fenderY = baseY + wheelRadius * 0.6f;
        CreateFender("Fender_FL", chassis.transform, V(-trackWidth, fenderY, wheelBase), wheelWidth, wheelRadius, bodyBlack, true);
        CreateFender("Fender_FR", chassis.transform, V(trackWidth, fenderY, wheelBase), wheelWidth, wheelRadius, bodyBlack, false);
        CreateFender("Fender_BL", chassis.transform, V(-trackWidth, fenderY, -wheelBase), wheelWidth, wheelRadius, bodyBlack, true);
        CreateFender("Fender_BR", chassis.transform, V(trackWidth, fenderY, -wheelBase), wheelWidth, wheelRadius, bodyBlack, false);
        CreateCube("FenderLink_FL", chassis.transform, V(-trackWidth + 0.12f, (fenderY + chassisY) / 2f, wheelBase * 0.5f), V(0.04f, fenderY - chassisY + 0.1f, 0.06f), bodyBlack);
        CreateCube("FenderLink_FR", chassis.transform, V(trackWidth - 0.12f, (fenderY + chassisY) / 2f, wheelBase * 0.5f), V(0.04f, fenderY - chassisY + 0.1f, 0.06f), bodyBlack);
        CreateCube("FenderLink_BL", chassis.transform, V(-trackWidth + 0.12f, (fenderY + chassisY) / 2f, -wheelBase * 0.5f), V(0.04f, fenderY - chassisY + 0.1f, 0.06f), bodyBlack);
        CreateCube("FenderLink_BR", chassis.transform, V(trackWidth - 0.12f, (fenderY + chassisY) / 2f, -wheelBase * 0.5f), V(0.04f, fenderY - chassisY + 0.1f, 0.06f), bodyBlack);
        GameObject frontMount = new GameObject("FrontMount");
        frontMount.transform.SetParent(root.transform);
        frontMount.transform.localPosition = V(0, baseY + 0.18f, wheelBase + 0.65f);
        CreateCube("Arm_L", chassis.transform, V(-0.28f * scaleFactor, chassisY - 0.05f, wheelBase + 0.25f * scaleFactor), V(0.06f, 0.12f, 0.48f * scaleFactor), bodyBlack);
        CreateCube("Arm_R", chassis.transform, V(0.28f * scaleFactor, chassisY - 0.05f, wheelBase + 0.25f * scaleFactor), V(0.06f, 0.12f, 0.48f * scaleFactor), bodyBlack);
        CreateCube("Bumper", frontMount.transform, V(0, 0, 0.08f), V(1.65f, 0.22f, 0.18f), bodyGrey);
        CreateCube("Bumper_Skid", frontMount.transform, V(0, -0.08f, 0.12f), V(1.5f, 0.06f, 0.10f), bodyBlack);
        CreateCube("Hazard_Stripe", frontMount.transform, V(0, 0.02f, 0.18f), V(1.45f, 0.07f, 0.01f), hazardYellow);
        GameObject rearHitch = new GameObject("RearHitch");
        rearHitch.transform.SetParent(root.transform);
        rearHitch.transform.localPosition = V(0, baseY + 0.15f, -wheelBase - 0.45f);
        CreateCube("Link_Top", chassis.transform, V(0, chassisY, -wheelBase - 0.15f * scaleFactor), V(0.04f, 0.04f, 0.36f * scaleFactor), chrome);
        CreateCube("Link_L", chassis.transform, V(-0.19f * scaleFactor, chassisY - 0.12f, -wheelBase - 0.17f * scaleFactor), V(0.035f, 0.035f, 0.42f * scaleFactor), chrome);
        CreateCube("Link_R", chassis.transform, V(0.19f * scaleFactor, chassisY - 0.12f, -wheelBase - 0.17f * scaleFactor), V(0.035f, 0.035f, 0.42f * scaleFactor), chrome);
        CreateCube("Crossbar", rearHitch.transform, V(0, 0, 0), V(0.48f * scaleFactor, 0.04f, 0.04f), bodyBlack);
        GameObject sensors = new GameObject("Sensors");
        sensors.transform.SetParent(root.transform);
        sensors.transform.localPosition = Vector3.zero;
        float sensorY = bodyY + 0.40f;
        CreateCylinder("GPS_Mast", sensors.transform, V(0.38f, sensorY + 0.18f, -0.25f), V(0.035f, 0.35f, 0.035f), chrome);
        CreateCylinder("GPS_Dome", sensors.transform, V(0.38f, sensorY + 0.38f, -0.25f), V(0.10f, 0.025f, 0.10f), bodyGrey);
        CreateCube("Lidar_Mount", sensors.transform, V(0, sensorY + 0.05f, 0.78f), V(0.16f, 0.10f, 0.10f), bodyBlack);
        CreateCylinder("Lidar_Lens", sensors.transform, V(0, sensorY + 0.05f, 0.84f), V(0.055f, 0.04f, 0.055f), glassBlack, Quaternion.Euler(90, 0, 0));
        CreateCube("Cam_Mount", sensors.transform, V(0, sensorY + 0.12f, 0.65f), V(0.22f, 0.06f, 0.05f), bodyBlack);
        CreateCylinder("Cam_L", sensors.transform, V(-0.07f, sensorY + 0.12f, 0.68f), V(0.025f, 0.015f, 0.025f), glassBlack, Quaternion.Euler(90, 0, 0));
        CreateCylinder("Cam_R", sensors.transform, V(0.07f, sensorY + 0.12f, 0.68f), V(0.025f, 0.015f, 0.025f), glassBlack, Quaternion.Euler(90, 0, 0));
        CreateCylinder("Beacon_Base", sensors.transform, V(-0.38f, sensorY + 0.05f, -0.25f), V(0.05f, 0.08f, 0.05f), bodyBlack);
        CreateCylinder("Beacon_Light", sensors.transform, V(-0.38f, sensorY + 0.12f, -0.25f), V(0.042f, 0.05f, 0.042f), lightOrange);
        CreateCube("Head_L_Mount", body.transform, V(-0.42f, bodyY + 0.12f, 0.97f), V(0.16f, 0.05f, 0.03f), bodyBlack);
        CreateCube("Head_L", body.transform, V(-0.42f, bodyY + 0.12f, 0.985f), V(0.14f, 0.035f, 0.01f), lightWhite);
        CreateCube("Head_R_Mount", body.transform, V(0.42f, bodyY + 0.12f, 0.97f), V(0.16f, 0.05f, 0.03f), bodyBlack);
        CreateCube("Head_R", body.transform, V(0.42f, bodyY + 0.12f, 0.985f), V(0.14f, 0.035f, 0.01f), lightWhite);
        CreateCube("WorkLight", body.transform, V(0, bodyY + 0.42f, 0.55f), V(0.22f, 0.05f, 0.04f), lightWhite);
        CreateCube("Tail_Strip", body.transform, V(0, bodyY, -0.97f), V(0.72f, 0.04f, 0.015f), lightRed);
        CreateCube("Logo_Front", body.transform, V(0, bodyY + 0.08f, 0.98f), V(0.18f, 0.07f, 0.01f), accentGreen);
        CreateCube("Logo_L", body.transform, V(-0.68f, bodyY, 0.28f), V(0.01f, 0.10f, 0.22f), accentGreen);
        CreateCube("Logo_R", body.transform, V(0.68f, bodyY, 0.28f), V(0.01f, 0.10f, 0.22f), accentGreen);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 2200f;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = V(0, baseY + 0.50f, 0);
        col.size = V(2.0f, 1.2f, 2.8f);
        RobotMovement mov = root.AddComponent<RobotMovement>();
        Transform[] wheelTransforms = {
            wheels.transform.Find("Wheel_FL"),
            wheels.transform.Find("Wheel_FR"),
            wheels.transform.Find("Wheel_BL"),
            wheels.transform.Find("Wheel_BR")
        };
        var field = typeof(RobotMovement).GetField("wheels", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(mov, wheelTransforms);
        root.AddComponent<RobotEnergy>();
        root.AddComponent<BatteryBarUI>();
        root.AddComponent<CropPlanter>();
        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/AgBot.prefab");
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("<color=#00FF88><b>✓ AgBot 2.055 W4 BUILD COMPLETE</b></color>\n" +
                  "• 4 Wheels with GREEN rims\n" +
                  "• CropPlanter attached\n" +
                  "• RobotMovement attached");
    }
    static void CreateWheelAgBot(string name, Transform parent, Vector3 pos, float radius, float width, Material tire, Material rim, Material hub)
    {
        GameObject wheel = new GameObject(name);
        wheel.transform.SetParent(parent);
        wheel.transform.localPosition = pos;
        GameObject tireObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tireObj.name = "Tire";
        tireObj.transform.SetParent(wheel.transform);
        tireObj.transform.localPosition = Vector3.zero;
        tireObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
        tireObj.transform.localScale = new Vector3(radius * 2, width * 0.5f, radius * 2);
        tireObj.GetComponent<MeshRenderer>().sharedMaterial = tire;
        Object.DestroyImmediate(tireObj.GetComponent<Collider>());
        GameObject rimObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimObj.name = "Rim";
        rimObj.transform.SetParent(wheel.transform);
        rimObj.transform.localPosition = Vector3.zero;
        rimObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
        rimObj.transform.localScale = new Vector3(radius * 1.1f, width * 0.52f, radius * 1.1f);
        rimObj.GetComponent<MeshRenderer>().sharedMaterial = rim;
        Object.DestroyImmediate(rimObj.GetComponent<Collider>());
        GameObject hubObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hubObj.name = "Hub";
        hubObj.transform.SetParent(wheel.transform);
        hubObj.transform.localPosition = Vector3.zero;
        hubObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
        hubObj.transform.localScale = new Vector3(radius * 0.35f, width * 0.55f, radius * 0.35f);
        hubObj.GetComponent<MeshRenderer>().sharedMaterial = hub;
        Object.DestroyImmediate(hubObj.GetComponent<Collider>());
        int treadCount = 16;
        for (int i = 0; i < treadCount; i++)
        {
            float angle = i * (360f / treadCount);
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            GameObject tread = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tread.name = $"Tread_{i}";
            tread.transform.SetParent(wheel.transform);
            tread.transform.localPosition = new Vector3(0, y, z);
            tread.transform.localRotation = Quaternion.Euler(angle, 0, 0);
            tread.transform.localScale = new Vector3(width * 0.45f, 0.035f, 0.10f);
            tread.GetComponent<MeshRenderer>().sharedMaterial = tire;
            Object.DestroyImmediate(tread.GetComponent<Collider>());
        }
    }
    static void CreateFender(string name, Transform parent, Vector3 pos, float width, float radius, Material mat, bool leftSide)
    {
        GameObject fender = new GameObject(name);
        fender.transform.SetParent(parent);
        fender.transform.localPosition = pos;
        float archH = radius * 0.55f;
        CreateCube("Arch", fender.transform, V(0, archH, 0), V(width + 0.08f, 0.035f, radius * 1.6f), mat);
        float strutX = leftSide ? 0.18f : -0.18f;
        CreateCube("Strut", fender.transform, V(strutX, archH / 2, 0), V(0.035f, archH, 0.18f), mat);
        CreateCube("Lip_F", fender.transform, V(0, archH - 0.08f, radius * 0.70f), V(width + 0.06f, 0.12f, 0.03f), mat);
        CreateCube("Lip_R", fender.transform, V(0, archH - 0.08f, -radius * 0.70f), V(width + 0.06f, 0.12f, 0.03f), mat);
    }
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static float GetGroundHeight(Vector3 pos)
    {
        Terrain t = Terrain.activeTerrain;
        if (t != null) return t.SampleHeight(pos) + t.transform.position.y;
        if (Physics.Raycast(new Vector3(pos.x, 1000f, pos.z), Vector3.down, out RaycastHit h, 2000f)) return h.point.y;
        return 0f;
    }
    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
    static Material Mat(string name, Color col, float metallic, float smoothness, string path)
    {
        string fullPath = path + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, fullPath);
        }
        m.SetColor("_BaseColor", col);
        m.SetColor("_Color", col);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }
    static Material MatEmissive(string name, Color col, string path)
    {
        string fullPath = path + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, fullPath);
        }
        m.SetColor("_BaseColor", col);
        m.SetColor("_Color", col);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", 0f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", col * 2.5f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }
    static GameObject CreateCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static GameObject CreateCylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        if (rot.HasValue) obj.transform.localRotation = rot.Value;
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
}
