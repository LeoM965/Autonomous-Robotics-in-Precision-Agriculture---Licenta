using UnityEngine;
using UnityEditor;
public class FarmBotBuilder : EditorWindow
{
    [MenuItem("Tools/FarmBot Builder Window")]
    public static void ShowWindow()
    {
        GetWindow<FarmBotBuilder>("FarmBot").Show();
    }
    [MenuItem("Tools/Build FarmBot Now")]
    public static void QuickBuild()
    {
        Build();
    }
    void OnGUI()
    {
        GUILayout.Space(20);
        GUILayout.Label("FarmBot Genesis XL", new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        });
        GUILayout.Space(20);
        if (GUILayout.Button("BUILD FARMBOT", GUILayout.Height(50)))
        {
            Build();
            Close();
        }
    }
    public static void Build()
    {
        string matPath = "Assets/Materials/FarmBot";
        EnsureFolder(matPath);
        GameObject root = new GameObject("FarmBot");
        float groundY = GetGroundHeight(Vector3.zero);
        root.transform.position = new Vector3(0f, groundY, 0f);
        Material wood = CreateMaterial("Wood", new Color(0.72f, 0.52f, 0.32f), 0f, 0.15f, matPath);
        Material woodDark = CreateMaterial("WoodDark", new Color(0.50f, 0.35f, 0.20f), 0f, 0.1f, matPath);
        Material soilMat = CreateMaterial("Soil", new Color(0.35f, 0.22f, 0.12f), 0f, 0.05f, matPath);
        Material alu = CreateMaterial("Alu", new Color(0.88f, 0.88f, 0.90f), 0.85f, 0.75f, matPath);
        Material aluDark = CreateMaterial("AluDark", new Color(0.45f, 0.45f, 0.50f), 0.7f, 0.55f, matPath);
        Material black = CreateMaterial("Black", new Color(0.08f, 0.08f, 0.10f), 0.1f, 0.3f, matPath);
        Material chrome = CreateMaterial("Chrome", new Color(0.95f, 0.95f, 0.97f), 1f, 0.92f, matPath);
        Material gold = CreateMaterial("Gold", new Color(0.90f, 0.72f, 0.18f), 0.95f, 0.85f, matPath);
        Material screen = CreateMaterial("Screen", new Color(0.15f, 0.50f, 0.88f), 0f, 0.92f, matPath);
        Material water = CreateMaterial("Water", new Color(0.25f, 0.55f, 0.82f), 0.2f, 0.8f, matPath);
        float bedLength = 6f;
        float bedWidth = 2.5f;
        float bedHeight = 0.6f;
        float wallThickness = 0.12f;
        GameObject bed = new GameObject("Bed");
        bed.transform.SetParent(root.transform);
        bed.transform.localPosition = Vector3.zero;
        CreateBox("Floor", bed.transform,
            new Vector3(0, wallThickness * 0.5f, 0),
            new Vector3(bedLength - wallThickness * 2, wallThickness, bedWidth - wallThickness * 2),
            woodDark);
        CreateBox("WallFront", bed.transform,
            new Vector3(0, bedHeight * 0.5f, bedWidth * 0.5f - wallThickness * 0.5f),
            new Vector3(bedLength, bedHeight, wallThickness),
            wood);
        CreateBox("WallBack", bed.transform,
            new Vector3(0, bedHeight * 0.5f, -bedWidth * 0.5f + wallThickness * 0.5f),
            new Vector3(bedLength, bedHeight, wallThickness),
            wood);
        CreateBox("WallLeft", bed.transform,
            new Vector3(-bedLength * 0.5f + wallThickness * 0.5f, bedHeight * 0.5f, 0),
            new Vector3(wallThickness, bedHeight, bedWidth - wallThickness * 2),
            wood);
        CreateBox("WallRight", bed.transform,
            new Vector3(bedLength * 0.5f - wallThickness * 0.5f, bedHeight * 0.5f, 0),
            new Vector3(wallThickness, bedHeight, bedWidth - wallThickness * 2),
            wood);
        CreateBox("Soil", bed.transform,
            new Vector3(0, bedHeight - 0.08f, 0),
            new Vector3(bedLength - wallThickness * 3, 0.16f, bedWidth - wallThickness * 3),
            soilMat);
        float postSize = 0.15f;
        CreateBox("PostFL", bed.transform,
            new Vector3(-bedLength * 0.5f + postSize * 0.5f, bedHeight * 0.5f, bedWidth * 0.5f - postSize * 0.5f),
            new Vector3(postSize, bedHeight, postSize), woodDark);
        CreateBox("PostFR", bed.transform,
            new Vector3(bedLength * 0.5f - postSize * 0.5f, bedHeight * 0.5f, bedWidth * 0.5f - postSize * 0.5f),
            new Vector3(postSize, bedHeight, postSize), woodDark);
        CreateBox("PostBL", bed.transform,
            new Vector3(-bedLength * 0.5f + postSize * 0.5f, bedHeight * 0.5f, -bedWidth * 0.5f + postSize * 0.5f),
            new Vector3(postSize, bedHeight, postSize), woodDark);
        CreateBox("PostBR", bed.transform,
            new Vector3(bedLength * 0.5f - postSize * 0.5f, bedHeight * 0.5f, -bedWidth * 0.5f + postSize * 0.5f),
            new Vector3(postSize, bedHeight, postSize), woodDark);
        float railY = bedHeight;
        float railZFront = bedWidth * 0.5f - wallThickness * 0.4f;
        float railZBack = -bedWidth * 0.5f + wallThickness * 0.4f;
        CreateBox("RailFront", bed.transform,
            new Vector3(0, railY + 0.04f, railZFront),
            new Vector3(bedLength, 0.08f, 0.14f), alu);
        CreateBox("RailBack", bed.transform,
            new Vector3(0, railY + 0.04f, railZBack),
            new Vector3(bedLength, 0.08f, 0.14f), alu);
        float gantryY = railY + 0.12f;
        float columnHeight = 1.6f;
        float gantrySpan = bedWidth + 0.1f;
        GameObject gantry = new GameObject("Gantry_System");
        gantry.transform.SetParent(root.transform);
        gantry.transform.localPosition = new Vector3(-0.8f, gantryY, 0);
        GameObject columnLeft = new GameObject("ColumnLeft");
        columnLeft.transform.SetParent(gantry.transform);
        columnLeft.transform.localPosition = new Vector3(0, 0, -gantrySpan * 0.5f);
        CreateBox("ColumnLeftBase", columnLeft.transform,
            new Vector3(0, -0.04f, 0.08f),
            new Vector3(0.18f, 0.08f, 0.1f), aluDark);
        CreateBox("ColumnLeftMain", columnLeft.transform,
            new Vector3(0, columnHeight * 0.5f, 0),
            new Vector3(0.1f, columnHeight, 0.1f), alu);
        CreateBox("ColumnLeftMotor", columnLeft.transform,
            new Vector3(0.08f, 0.18f, 0.03f),
            new Vector3(0.1f, 0.12f, 0.08f), black);
        GameObject columnRight = new GameObject("ColumnRight");
        columnRight.transform.SetParent(gantry.transform);
        columnRight.transform.localPosition = new Vector3(0, 0, gantrySpan * 0.5f);
        CreateBox("ColumnRightBase", columnRight.transform,
            new Vector3(0, -0.04f, -0.08f),
            new Vector3(0.18f, 0.08f, 0.1f), aluDark);
        CreateBox("ColumnRightMain", columnRight.transform,
            new Vector3(0, columnHeight * 0.5f, 0),
            new Vector3(0.1f, columnHeight, 0.1f), alu);
        CreateBox("ColumnRightMotor", columnRight.transform,
            new Vector3(0.08f, 0.18f, -0.03f),
            new Vector3(0.1f, 0.12f, 0.08f), black);
        CreateBox("CrossBeam", gantry.transform,
            new Vector3(0, columnHeight, 0),
            new Vector3(0.1f, 0.1f, gantrySpan), alu);
        CreateBox("CrossBeamTop", gantry.transform,
            new Vector3(0, columnHeight + 0.07f, 0),
            new Vector3(0.12f, 0.04f, gantrySpan), alu);
        GameObject xCarriage = new GameObject("X_Carriage");
        xCarriage.transform.SetParent(gantry.transform);
        xCarriage.transform.localPosition = new Vector3(0, columnHeight - 0.12f, 0.18f);
        CreateBox("XCarriagePlate", xCarriage.transform,
            new Vector3(0, 0, 0),
            new Vector3(0.16f, 0.16f, 0.12f), aluDark);
        CreateBox("XCarriageMotor", xCarriage.transform,
            new Vector3(-0.1f, 0.03f, 0),
            new Vector3(0.08f, 0.1f, 0.08f), black);
        GameObject zAxis = new GameObject("Z_Axis");
        zAxis.transform.SetParent(xCarriage.transform);
        zAxis.transform.localPosition = new Vector3(0, -0.14f, 0);
        CreateBox("ZAxisColumn", zAxis.transform,
            new Vector3(0, -0.38f, 0),
            new Vector3(0.08f, 0.8f, 0.08f), alu);
        CreateBox("ZAxisMotor", zAxis.transform,
            new Vector3(0, 0.05f, 0),
            new Vector3(0.1f, 0.12f, 0.1f), black);
        CreateBox("ZAxisCarriage", zAxis.transform,
            new Vector3(0.06f, -0.58f, 0),
            new Vector3(0.06f, 0.12f, 0.08f), aluDark);
        CreateCylinder("ZAxisScrew", zAxis.transform,
            new Vector3(0, -0.38f, 0),
            new Vector3(0.02f, 0.4f, 0.02f), chrome);
        GameObject toolHead = new GameObject("Tool_Head");
        toolHead.transform.SetParent(zAxis.transform);
        toolHead.transform.localPosition = new Vector3(0, -0.8f, 0);
        CreateCylinder("UTMBase", toolHead.transform,
            new Vector3(0, 0, 0),
            new Vector3(0.14f, 0.03f, 0.14f), aluDark);
        CreateCylinder("UTMRing", toolHead.transform,
            new Vector3(0, -0.05f, 0),
            new Vector3(0.11f, 0.018f, 0.11f), alu);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            float radius = 0.045f;
            CreateCylinder("Pin" + i, toolHead.transform,
                new Vector3(Mathf.Cos(angle) * radius, -0.025f, Mathf.Sin(angle) * radius),
                new Vector3(0.007f, 0.014f, 0.007f), gold);
        }
        CreateCylinder("Nozzle", toolHead.transform,
            new Vector3(0, -0.1f, 0),
            new Vector3(0.05f, 0.04f, 0.05f), black);
        CreateCylinder("NozzleTip", toolHead.transform,
            new Vector3(0, -0.15f, 0),
            new Vector3(0.018f, 0.025f, 0.018f), chrome);
        GameObject camera = new GameObject("Camera");
        camera.transform.SetParent(zAxis.transform);
        camera.transform.localPosition = new Vector3(-0.1f, -0.48f, 0);
        CreateBox("CameraBracket", camera.transform,
            new Vector3(0.04f, 0.04f, 0),
            new Vector3(0.08f, 0.025f, 0.05f), aluDark);
        CreateBox("CameraBody", camera.transform,
            new Vector3(0, 0, 0),
            new Vector3(0.08f, 0.1f, 0.05f), black);
        CreateCylinder("CameraLens", camera.transform,
            new Vector3(0, 0, 0.03f),
            new Vector3(0.04f, 0.015f, 0.04f), chrome);
        GameObject tablet = new GameObject("Tablet");
        tablet.transform.SetParent(columnLeft.transform);
        tablet.transform.localPosition = new Vector3(-0.12f, columnHeight * 0.5f, 0.1f);
        CreateBox("TabletArm", tablet.transform,
            new Vector3(-0.04f, 0, -0.025f),
            new Vector3(0.1f, 0.025f, 0.025f), black);
        CreateBox("TabletBracket", tablet.transform,
            new Vector3(-0.1f, 0, 0),
            new Vector3(0.018f, 0.14f, 0.018f), black);
        CreateBox("TabletFrame", tablet.transform,
            new Vector3(-0.12f, 0.06f, 0),
            new Vector3(0.015f, 0.2f, 0.14f), black);
        CreateBox("TabletScreen", tablet.transform,
            new Vector3(-0.13f, 0.06f, 0),
            new Vector3(0.005f, 0.17f, 0.12f), screen);
        GameObject toolBays = new GameObject("ToolBays");
        toolBays.transform.SetParent(bed.transform);
        toolBays.transform.localPosition = new Vector3(-bedLength * 0.5f + wallThickness, bedHeight, 0);
        CreateBox("ToolBayBracket", toolBays.transform,
            new Vector3(-0.03f, 0.04f, 0),
            new Vector3(0.06f, 0.08f, 0.55f), aluDark);
        CreateBox("ToolBaySupport1", toolBays.transform,
            new Vector3(-0.03f, -0.015f, 0.2f),
            new Vector3(0.05f, 0.03f, 0.05f), aluDark);
        CreateBox("ToolBaySupport2", toolBays.transform,
            new Vector3(-0.03f, -0.015f, 0),
            new Vector3(0.05f, 0.03f, 0.05f), aluDark);
        CreateBox("ToolBaySupport3", toolBays.transform,
            new Vector3(-0.03f, -0.015f, -0.2f),
            new Vector3(0.05f, 0.03f, 0.05f), aluDark);
        for (int i = 0; i < 3; i++)
        {
            float z = -0.16f + i * 0.16f;
            CreateBox("ToolBay" + i, toolBays.transform,
                new Vector3(-0.03f, 0.1f, z),
                new Vector3(0.07f, 0.07f, 0.06f), aluDark);
            CreateCylinder("ToolMagnet" + i, toolBays.transform,
                new Vector3(-0.03f, 0.06f, z),
                new Vector3(0.04f, 0.008f, 0.04f), chrome);
        }
        GameObject electronics = new GameObject("Electronics");
        electronics.transform.SetParent(root.transform);
        electronics.transform.localPosition = new Vector3(bedLength * 0.5f + 0.5f, 0, -bedWidth * 0.5f - 0.35f);
        CreateBox("ElecBase", electronics.transform,
            new Vector3(0, 0.02f, 0),
            new Vector3(0.16f, 0.04f, 0.14f), aluDark);
        CreateBox("ElecPost", electronics.transform,
            new Vector3(0, 0.32f, 0),
            new Vector3(0.06f, 0.64f, 0.06f), alu);
        CreateBox("ElecBox", electronics.transform,
            new Vector3(0, 0.7f, 0),
            new Vector3(0.3f, 0.24f, 0.18f), wood);
        CreateBox("ElecPanel", electronics.transform,
            new Vector3(0, 0.7f, 0.092f),
            new Vector3(0.27f, 0.21f, 0.006f), aluDark);
        CreateCylinder("Antenna", electronics.transform,
            new Vector3(0.1f, 0.95f, 0),
            new Vector3(0.015f, 0.14f, 0.015f), black);
        CreateSphere("AntennaTop", electronics.transform,
            new Vector3(0.1f, 1.1f, 0),
            new Vector3(0.025f, 0.02f, 0.025f), black);
        GameObject waterTank = new GameObject("WaterTank");
        waterTank.transform.SetParent(root.transform);
        waterTank.transform.localPosition = new Vector3(bedLength * 0.5f + 0.5f, 0, bedWidth * 0.5f + 0.35f);
        CreateBox("WaterBase", waterTank.transform,
            new Vector3(0, 0.02f, 0),
            new Vector3(0.16f, 0.04f, 0.14f), aluDark);
        CreateBox("WaterPost", waterTank.transform,
            new Vector3(0, 0.28f, 0),
            new Vector3(0.06f, 0.56f, 0.06f), alu);
        CreateCylinder("Tank", waterTank.transform,
            new Vector3(0, 0.7f, 0),
            new Vector3(0.24f, 0.2f, 0.24f), water);
        CreateCylinder("TankTop", waterTank.transform,
            new Vector3(0, 0.92f, 0),
            new Vector3(0.18f, 0.025f, 0.18f), aluDark);
        CreateCylinder("TankCap", waterTank.transform,
            new Vector3(0, 0.96f, 0),
            new Vector3(0.08f, 0.025f, 0.08f), black);
        CreateCylinder("TankValve", waterTank.transform,
            new Vector3(0.14f, 0.58f, 0),
            new Vector3(0.035f, 0.03f, 0.035f), chrome);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 150f;
        rb.isKinematic = true;
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, bedHeight * 0.5f, 0);
        col.size = new Vector3(bedLength + 0.4f, bedHeight + 0.15f, bedWidth + 0.4f);
        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/FarmBot.prefab");
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("FarmBot created at Y = " + groundY);
    }
    static float GetGroundHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            return terrain.SampleHeight(position) + terrain.transform.position.y;
        }
        RaycastHit hit;
        Vector3 rayStart = new Vector3(position.x, 1000f, position.z);
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 2000f))
        {
            return hit.point.y;
        }
        return 0f;
    }
    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
    static Material CreateMaterial(string name, Color color, float metallic, float smoothness, string path)
    {
        string fullPath = path + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
        bool isNew = (mat == null);
        if (isNew)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        mat.enableInstancing = true;
        if (isNew)
        {
            AssetDatabase.CreateAsset(mat, fullPath);
        }
        else
        {
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }
    static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static GameObject CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
}
