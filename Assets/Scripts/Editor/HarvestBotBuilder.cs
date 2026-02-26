using UnityEngine;
using UnityEditor;
using System.Reflection;

public class HarvestBotBuilder : EditorWindow
{
    [MenuItem("Tools/HarvestBot Builder Window")]
    public static void ShowWindow() => GetWindow<HarvestBotBuilder>("HarvestBot").Show();

    [MenuItem("Tools/Build HarvestBot Now")]
    public static void QuickBuild() => Build();

    void OnGUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("Fruit Harvesting Robot", new GUIStyle(GUI.skin.label)
        { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(20);
        if (GUILayout.Button("BUILD HARVESTBOT", GUILayout.Height(50)))
        {
            Build();
            Close();
        }
    }

    public static void Build()
    {
        // Delete any existing HarvestBot from scene
        foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (old != null && old.parent == null && old.name == "HarvestBot")
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }
        }

        string matPath = "Assets/Materials/HarvestBot";
        EnsureFolder(matPath);

        // Materials
        Material blue      = Mat("URBlue",      new Color(0.30f, 0.58f, 0.78f), 0.4f,  0.7f,  matPath);
        Material silver     = Mat("Silver",      new Color(0.78f, 0.78f, 0.82f), 0.85f, 0.75f, matPath);
        Material darkSilver = Mat("DarkSilver",  new Color(0.45f, 0.45f, 0.50f), 0.7f,  0.6f,  matPath);
        Material black      = Mat("Black",       new Color(0.08f, 0.08f, 0.10f), 0.1f,  0.2f,  matPath);
        Material gray       = Mat("Gray",        new Color(0.35f, 0.35f, 0.38f), 0.2f,  0.35f, matPath);
        Material orange     = Mat("Orange",      new Color(0.95f, 0.45f, 0.08f), 0.2f,  0.5f,  matPath);
        Material rubber     = Mat("Rubber",      new Color(0.10f, 0.10f, 0.10f), 0f,    0.05f, matPath);
        Material screenGreen= Mat("ScreenGreen", new Color(0.1f,  0.7f,  0.2f),  0f,    0.9f,  matPath);

        // Root
        GameObject root = new GameObject("HarvestBot");
        Undo.RegisterCreatedObjectUndo(root, "Build HarvestBot");
        root.transform.position = new Vector3(5f, GetGroundHeight(new Vector3(5, 0, 5)), 5f);
        root.transform.localScale = Vector3.one * 2.5f;

        // ─────────────── BASE ───────────────
        GameObject baseGO = Child("Base", root.transform, V3(0, 0, 0));

        CreateCube("MainBody",   baseGO.transform, V3(0, 0.18f, 0),       V3(0.55f, 0.28f, 0.42f), gray);
        CreateCube("TopPlate",   baseGO.transform, V3(0, 0.33f, 0),       V3(0.57f, 0.02f, 0.44f), darkSilver);
        CreateCube("ControlBox", baseGO.transform, V3(-0.14f, 0.42f, 0),  V3(0.22f, 0.14f, 0.24f), gray);
        CreateCube("Screen",     baseGO.transform, V3(-0.14f, 0.44f, 0.123f), V3(0.14f, 0.08f, 0.005f), screenGreen);

        // Wheels
        CreateWheel("WFL", baseGO.transform, V3(-0.24f, 0.055f,  0.16f), rubber, darkSilver);
        CreateWheel("WFR", baseGO.transform, V3( 0.24f, 0.055f,  0.16f), rubber, darkSilver);
        CreateWheel("WBL", baseGO.transform, V3(-0.24f, 0.055f, -0.16f), rubber, darkSilver);
        CreateWheel("WBR", baseGO.transform, V3( 0.24f, 0.055f, -0.16f), rubber, darkSilver);

        // ─────────────── ARM MOUNTING ───────────────
        // Arm platform sits on top of the base
        float armX = 0.10f;
        CreateCylinder("ArmPlatform",   baseGO.transform, V3(armX, 0.36f, 0),  V3(0.13f, 0.025f, 0.13f), darkSilver);
        CreateCylinder("J1_Housing",    baseGO.transform, V3(armX, 0.385f, 0), V3(0.11f, 0.06f, 0.11f),  blue);
        CreateCylinder("ShoulderBlock", baseGO.transform, V3(armX, 0.445f, 0), V3(0.09f, 0.04f, 0.09f),  silver);
        CreateCylinder("J2_Motor",      baseGO.transform, V3(armX, 0.495f, 0), V3(0.10f, 0.065f, 0.10f), blue, Quaternion.Euler(90, 0, 0));

        // ─────────────── UPPER ARM ───────────────
        // Pivot at J2 motor position, tilted -20 degrees
        GameObject upperArm = Child("UpperArm", baseGO.transform, V3(armX, 0.495f, 0));
        upperArm.transform.localRotation = Quaternion.Euler(0, 0, -20f);

        CreateCylinder("UpperArm_Bottom", upperArm.transform, V3(0, 0.01f, 0),  V3(0.08f, 0.02f, 0.08f),   darkSilver);
        CreateCube    ("UpperArm_Link",   upperArm.transform, V3(0, 0.20f, 0),  V3(0.07f, 0.38f, 0.07f),   silver);
        CreateCylinder("UpperArm_Top",    upperArm.transform, V3(0, 0.39f, 0),  V3(0.08f, 0.02f, 0.08f),   darkSilver);
        CreateCylinder("J3_Motor",        upperArm.transform, V3(0, 0.41f, 0),  V3(0.085f, 0.055f, 0.085f),blue, Quaternion.Euler(90, 0, 0));

        // ─────────────── FOREARM ───────────────
        // Pivot at J3 motor, tilted 70 degrees
        GameObject forearm = Child("Forearm", upperArm.transform, V3(0, 0.41f, 0));
        forearm.transform.localRotation = Quaternion.Euler(0, 0, 70f);

        CreateCylinder("Forearm_Bottom", forearm.transform, V3(0, 0.015f, 0), V3(0.065f, 0.02f, 0.065f), darkSilver);
        CreateCube    ("Forearm_Link",   forearm.transform, V3(0, 0.155f, 0), V3(0.055f, 0.28f, 0.055f), silver);
        CreateCylinder("Forearm_Top",    forearm.transform, V3(0, 0.295f, 0), V3(0.065f, 0.02f, 0.065f), darkSilver);
        CreateCylinder("J4_Motor",       forearm.transform, V3(0, 0.315f, 0), V3(0.06f, 0.04f, 0.06f),   blue, Quaternion.Euler(90, 0, 0));

        // ─────────────── WRIST ───────────────
        GameObject wrist = Child("Wrist", forearm.transform, V3(0, 0.335f, 0));

        CreateCylinder("Wrist_Bottom",  wrist.transform, V3(0, 0.01f, 0),  V3(0.055f, 0.015f, 0.055f), darkSilver);
        CreateCube    ("Wrist_Link",    wrist.transform, V3(0, 0.045f, 0), V3(0.045f, 0.07f, 0.045f),  silver);
        CreateCylinder("Wrist_Top",     wrist.transform, V3(0, 0.08f, 0),  V3(0.055f, 0.015f, 0.055f), darkSilver);
        CreateCylinder("J5_Motor",      wrist.transform, V3(0, 0.095f, 0), V3(0.052f, 0.035f, 0.052f), blue);
        CreateCube    ("FinalLink",     wrist.transform, V3(0, 0.14f, 0),  V3(0.04f, 0.05f, 0.04f),    silver);
        CreateCylinder("FinalLink_Top", wrist.transform, V3(0, 0.165f, 0), V3(0.048f, 0.012f, 0.048f), darkSilver);
        CreateCylinder("J6_Flange",     wrist.transform, V3(0, 0.18f, 0),  V3(0.045f, 0.015f, 0.045f), blue);

        // ─────────────── GRIPPER ───────────────
        GameObject gripper = Child("Gripper", wrist.transform, V3(0, 0.195f, 0));

        CreateCylinder("Gripper_Base", gripper.transform, V3(0, 0.005f, 0),        V3(0.04f, 0.01f, 0.04f),   darkSilver);
        CreateCube    ("Gripper_Body", gripper.transform, V3(0, 0.035f, 0),        V3(0.05f, 0.04f, 0.028f),  gray);
        CreateCube    ("Finger_L",     gripper.transform, V3(-0.018f, 0.075f, 0),  V3(0.012f, 0.06f, 0.016f), silver);
        CreateCube    ("Finger_R",     gripper.transform, V3( 0.018f, 0.075f, 0),  V3(0.012f, 0.06f, 0.016f), silver);
        CreateCube    ("Tip_L",        gripper.transform, V3(-0.018f, 0.11f, 0),   V3(0.015f, 0.015f, 0.02f), rubber);
        CreateCube    ("Tip_R",        gripper.transform, V3( 0.018f, 0.11f, 0),   V3(0.015f, 0.015f, 0.02f), rubber);
        CreateCylinder("Suction_Tube", gripper.transform, V3(0, 0.06f, 0.016f),    V3(0.012f, 0.018f, 0.012f),darkSilver);
        CreateCylinder("Suction_Cup",  gripper.transform, V3(0, 0.09f, 0.016f),    V3(0.028f, 0.012f, 0.028f),orange);
        CreateCube    ("Camera_Mount", gripper.transform, V3(0.032f, 0.035f, 0),   V3(0.012f, 0.012f, 0.012f),darkSilver);
        CreateCube    ("Camera_Body",  gripper.transform, V3(0.048f, 0.035f, 0),   V3(0.022f, 0.016f, 0.016f),black);

        // ─────────────── PHYSICS ───────────────
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 2000f;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 2.0f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;

        BoxCollider col = root.AddComponent<BoxCollider>();
        // Adjusted to match AgBot clearance: AgBot bottom is ~0.28, HarvestBot was 0.125
        // Raising center to 0.45 local (1.125 world) with size 0.5 (1.25 world) -> bottom at 0.5 world
        col.center = V3(0, 0.45f, 0); 
        col.size = V3(0.6f, 0.5f, 0.45f);

        // ─────────────── COMPONENTS ───────────────
        RobotMovement mov = root.AddComponent<RobotMovement>();
        mov.speed = 5f;
        mov.rotationSpeed = 120f;
        mov.heightSpeed = 40f;
        // Set wheels via reflection
        Transform[] wheelTransforms = {
            baseGO.transform.Find("WFL"),
            baseGO.transform.Find("WFR"),
            baseGO.transform.Find("WBL"),
            baseGO.transform.Find("WBR")
        };
        var field = typeof(RobotMovement).GetField("wheels", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(mov, wheelTransforms);

        root.AddComponent<RobotEnergy>();
        root.AddComponent<BatteryBarUI>();
        root.AddComponent<CropHarvester>();

        // ─────────────── SAVE ───────────────
        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/HarvestBot.prefab");
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("<color=#FF8800><b>✓ HarvestBot BUILD COMPLETE</b></color>\n" +
                  "• RobotMovement + RobotEnergy + CropHarvester attached");
    }

    // ═══════════════ HELPER METHODS ═══════════════

    static GameObject Child(string name, Transform parent, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    static void CreateWheel(string name, Transform parent, Vector3 pos, Material tire, Material hub)
    {
        GameObject wheel = new GameObject(name);
        wheel.transform.SetParent(parent);
        wheel.transform.localPosition = pos;
        wheel.transform.localRotation = Quaternion.identity;
        wheel.transform.localScale = Vector3.one;

        GameObject tireObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tireObj.name = "Tire";
        tireObj.transform.SetParent(wheel.transform);
        tireObj.transform.localPosition = Vector3.zero;
        tireObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        tireObj.transform.localScale = new Vector3(0.11f, 0.025f, 0.11f);
        tireObj.GetComponent<MeshRenderer>().sharedMaterial = tire;
        Object.DestroyImmediate(tireObj.GetComponent<Collider>());

        GameObject hubObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hubObj.name = "Hub";
        hubObj.transform.SetParent(wheel.transform);
        hubObj.transform.localPosition = Vector3.zero;
        hubObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        hubObj.transform.localScale = new Vector3(0.05f, 0.03f, 0.05f);
        hubObj.GetComponent<MeshRenderer>().sharedMaterial = hub;
        Object.DestroyImmediate(hubObj.GetComponent<Collider>());
    }

    static Vector3 V3(float x, float y, float z) => new Vector3(x, y, z);

    static float GetGroundHeight(Vector3 pos)
    {
        Terrain t = Terrain.activeTerrain;
        if (t != null) return t.SampleHeight(pos) + t.transform.position.y;
        if (Physics.Raycast(new Vector3(pos.x, 1000f, pos.z), Vector3.down, out RaycastHit h, 2000f)) return h.point.y;
        return 0f;
    }

    static void EnsureFolder(string path)
    {
        string[] p = path.Split('/');
        string c = p[0];
        for (int i = 1; i < p.Length; i++)
        {
            string n = c + "/" + p[i];
            if (!AssetDatabase.IsValidFolder(n)) AssetDatabase.CreateFolder(c, p[i]);
            c = n;
        }
    }

    static Material Mat(string name, Color col, float met, float smo, string path)
    {
        string fp = path + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(fp);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, fp);
        }
        m.SetColor("_BaseColor", col);
        m.SetColor("_Color", col);
        m.SetFloat("_Metallic", met);
        m.SetFloat("_Smoothness", smo);
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
        obj.transform.localRotation = Quaternion.identity;
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
        obj.transform.localRotation = rot.HasValue ? rot.Value : Quaternion.identity;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
}
