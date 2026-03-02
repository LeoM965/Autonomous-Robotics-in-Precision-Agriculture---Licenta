using UnityEngine;
using System.Collections.Generic;

public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    [Header("Optimization")]
    [SerializeField] private int updatesPerFrame = 512;
    
    private readonly List<CropGrowth> activeCrops = new List<CropGrowth>(4096);
    private readonly Dictionary<CropGrowth, int> cropIndices = new Dictionary<CropGrowth, int>();
    private int lastUpdateIndex;
    private float lastSimHours = -1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        CropSelector.CropSelected += OnCropSelected;
    }

    private void OnDisable()
    {
        CropSelector.CropSelected -= OnCropSelected;
    }

    private void OnCropSelected(Transform robot, CropData crop, float score, 
        List<AI.Models.Decisions.DecisionAlternative> alternatives, 
        Sensors.Models.SoilComposition soil, string parcelName)
    {
        if (AI.Analytics.DecisionTracker.Instance == null) return;

        var record = new AI.Analytics.DecisionRecord
        {
            decisionType = "Selectie Cultura",
            chosenOption = crop != null ? crop.name : "Niciuna",
            chosenScore = score,
            alternatives = alternatives,
            factors = crop?.requirements?.BuildFactors(soil) ?? new AI.Models.Decisions.DecisionFactors(),
            parcelName = parcelName
        };

        AI.Analytics.DecisionTracker.Instance.RecordDecision(robot, record);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CropManager");
            go.AddComponent<CropManager>();
        }
    }

    public void RegisterCrop(CropGrowth crop)
    {
        if (crop == null || cropIndices.ContainsKey(crop)) return;
        cropIndices[crop] = activeCrops.Count;
        activeCrops.Add(crop);
    }

    public void UnregisterCrop(CropGrowth crop)
    {
        if (crop == null || !cropIndices.TryGetValue(crop, out int index)) return;

        int lastIndex = activeCrops.Count - 1;
        if (index < lastIndex)
        {
            CropGrowth lastCrop = activeCrops[lastIndex];
            activeCrops[index] = lastCrop;
            cropIndices[lastCrop] = index;
        }

        activeCrops.RemoveAt(lastIndex);
        cropIndices.Remove(crop);
        
        if (lastUpdateIndex >= activeCrops.Count) 
            lastUpdateIndex = 0;
    }

    private void Update()
    {
        int count = activeCrops.Count;
        if (count == 0 || TimeManager.Instance == null) return;

        // Track simulated time from TimeManager (in hours)
        float currentSimHours = (TimeManager.Instance.currentDay - 1) * 24f + TimeManager.Instance.timeOfDay;
        
        if (lastSimHours < 0f)
        {
            lastSimHours = currentSimHours;
            return;
        }

        float deltaHours = currentSimHours - lastSimHours;
        if (deltaHours <= 0f) { lastSimHours = currentSimHours; return; }
        
        lastSimHours = currentSimHours;

        int slice = Mathf.Min(count, updatesPerFrame);
        
        for (int i = 0; i < slice; i++)
        {
            lastUpdateIndex = (lastUpdateIndex + 1) % count;
            var crop = activeCrops[lastUpdateIndex];
            if (crop != null) crop.ManualUpdate(deltaHours);
        }
    }
}
