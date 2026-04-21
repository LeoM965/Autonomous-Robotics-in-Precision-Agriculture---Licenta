using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;

public class CropManager : MonoBehaviour
{
    public static CropManager Instance { get; private set; }

    [Header("Optimization (Job System)")]
    [Tooltip("Ruleaza calculele agricole cu Burst Compiler pe toate plantele simultan.")]
    [SerializeField] private bool useJobSystem = true;
    
    private readonly List<CropGrowth> activeCrops = new List<CropGrowth>(4096);
    private readonly Dictionary<CropGrowth, int> cropIndices = new Dictionary<CropGrowth, int>();

    // Persistent Buffers for Job System
    private Unity.Collections.NativeArray<float> consumeRates;
    private Unity.Collections.NativeArray<float> optimalNs;
    private Unity.Collections.NativeArray<float> sensorNs;
    private Unity.Collections.NativeArray<float> tempMults;
    private Unity.Collections.NativeArray<float> outNitrogen;
    private Unity.Collections.NativeArray<float> outGrowth;

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
        DisposeBuffers();
    }

    private void OnDestroy() => DisposeBuffers();

    private void DisposeBuffers()
    {
        if (consumeRates.IsCreated) consumeRates.Dispose();
        if (optimalNs.IsCreated) optimalNs.Dispose();
        if (sensorNs.IsCreated) sensorNs.Dispose();
        if (tempMults.IsCreated) tempMults.Dispose();
        if (outNitrogen.IsCreated) outNitrogen.Dispose();
        if (outGrowth.IsCreated) outGrowth.Dispose();
    }

    private void EnsureBufferCapacity(int count)
    {
        if (consumeRates.IsCreated && consumeRates.Length >= count) return;

        DisposeBuffers();
        int newSize = Mathf.Max(count, 1024);
        var alloc = Unity.Collections.Allocator.Persistent;

        consumeRates = new Unity.Collections.NativeArray<float>(newSize, alloc);
        optimalNs = new Unity.Collections.NativeArray<float>(newSize, alloc);
        sensorNs = new Unity.Collections.NativeArray<float>(newSize, alloc);
        tempMults = new Unity.Collections.NativeArray<float>(newSize, alloc);
        outNitrogen = new Unity.Collections.NativeArray<float>(newSize, alloc);
        outGrowth = new Unity.Collections.NativeArray<float>(newSize, alloc);
    }

    private void OnCropSelected(Transform robot, CropData crop, float score, 
        List<AI.Models.Decisions.DecisionAlternative> alternatives, 
        Sensors.Models.SoilComposition soil, string parcelName, int plantCount, float schedulingValue)
    {
        if (AI.Analytics.DecisionTracker.Instance == null) return;

        float unitProfit = crop != null 
            ? (score / 100f) * crop.yieldValueEUR - crop.seedCostEUR 
            : 0f;

        var record = new AI.Analytics.DecisionRecord
        {
            decisionType = "Selectie Cultura",
            chosenOption = crop != null ? crop.name : "Niciuna",
            chosenScore = score,
            netValue = unitProfit * plantCount,
            schedulingValue = schedulingValue,
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
        
    }

    private float lastProcessTime = -1f;

    private void Update()
    {
        int count = activeCrops.Count;
        if (count == 0 || TimeManager.Instance == null) return;

        float currentSimHours = TimeManager.Instance.TotalSimulatedHours;
        
        float deltaHours = lastProcessTime >= 0 ? currentSimHours - lastProcessTime : 0f;
        lastProcessTime = currentSimHours;

        if (deltaHours <= 0) return;

        float weatherMult = Weather.Components.WeatherSystem.Instance != null 
                          ? Weather.Components.WeatherSystem.Instance.GetCropGrowthMultiplier() : 1f;

        if (useJobSystem && count > 0)
        {
            EnsureBufferCapacity(count);

            // Temperatura curenta (o citim o singura data, nu per-planta)
            float currentTemp = Weather.Components.WeatherSystem.Instance != null
                ? Weather.Components.WeatherSystem.Instance.CurrentTemperature : 20f;
            var db = CropLoader.Load();

            // 2. Colectam datele (Main Thread gathering - can be a bottleneck for 10k+)
            for (int i = 0; i < count; i++)
            {
                var crop = activeCrops[i];
                if (crop != null && !crop.IsBeingHarvested && crop.Progress < 1f)
                {
                    consumeRates[i] = crop.GetNitrogenConsumptionRate();
                    optimalNs[i] = crop.GetOptimalNitrogen();
                    sensorNs[i] = crop.ParentSensor != null ? crop.ParentSensor.nitrogen : 0f;

                    // Temperatura cardinala per cultura: CropData.GetTemperatureMultiplier()
                    tempMults[i] = crop.GetTemperatureMultiplier(currentTemp, db);
                }
                else
                {
                    consumeRates[i] = 0f;
                    optimalNs[i] = 0f;
                    sensorNs[i] = 0f;
                    tempMults[i] = 1f;
                }
            }

            // 3. Rulam Job-ul Paralel
            var job = new Crops.Jobs.CropUpdateJob
            {
                deltaHours = deltaHours,
                weatherMultiplier = weatherMult,
                consumeRates = consumeRates,
                optimalNs = optimalNs,
                sensorNitrogens = sensorNs,
                tempMultipliers = tempMults,
                outConsumedNitrogen = outNitrogen,
                outGrowthDelta = outGrowth
            };

            job.Schedule(count, 64).Complete();

            // 4. Aplicam rezultatele vizual
            for (int i = 0; i < count; i++)
            {
                if (activeCrops[i] != null && (outGrowth[i] > 0 || outNitrogen[i] > 0))
                    activeCrops[i].ApplyJobResults(outGrowth[i], outNitrogen[i]);
            }
        }
        else
        {
            // Punctul in care pica FPS-urile daca nu se foloseste Job-ul (fallback iterativ)
            for (int i = 0; i < count; i++)
            {
                if (activeCrops[i] != null) 
                    activeCrops[i].ProcessGrowth(deltaHours, weatherMult);
            }
        }
    }
}
