using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.SceneManagement;

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

    // P & K buffers
    private Unity.Collections.NativeArray<float> optimalPs;
    private Unity.Collections.NativeArray<float> optimalKs;
    private Unity.Collections.NativeArray<float> sensorPs;
    private Unity.Collections.NativeArray<float> sensorKs;
    private Unity.Collections.NativeArray<float> outPhosphorus;
    private Unity.Collections.NativeArray<float> outPotassium;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        CropSelector.CropSelected -= OnCropSelected;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DisposeBuffers();
    }

    private void OnDestroy() => DisposeBuffers();

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeCrops.Clear();
        cropIndices.Clear();
        lastProcessTime = -1f;
    }

    private void DisposeBuffers()
    {
        if (consumeRates.IsCreated) consumeRates.Dispose();
        if (optimalNs.IsCreated) optimalNs.Dispose();
        if (sensorNs.IsCreated) sensorNs.Dispose();
        if (tempMults.IsCreated) tempMults.Dispose();
        if (outNitrogen.IsCreated) outNitrogen.Dispose();
        if (outGrowth.IsCreated) outGrowth.Dispose();
        if (optimalPs.IsCreated) optimalPs.Dispose();
        if (optimalKs.IsCreated) optimalKs.Dispose();
        if (sensorPs.IsCreated) sensorPs.Dispose();
        if (sensorKs.IsCreated) sensorKs.Dispose();
        if (outPhosphorus.IsCreated) outPhosphorus.Dispose();
        if (outPotassium.IsCreated) outPotassium.Dispose();
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
        optimalPs = new Unity.Collections.NativeArray<float>(newSize, alloc);
        optimalKs = new Unity.Collections.NativeArray<float>(newSize, alloc);
        sensorPs = new Unity.Collections.NativeArray<float>(newSize, alloc);
        sensorKs = new Unity.Collections.NativeArray<float>(newSize, alloc);
        outPhosphorus = new Unity.Collections.NativeArray<float>(newSize, alloc);
        outPotassium = new Unity.Collections.NativeArray<float>(newSize, alloc);
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

    /// <summary>Sincronizează timpul de referință la load (evită delta uriașe).</summary>
    public void SyncProcessTime(float simHours) => lastProcessTime = simHours;

    private void Update()
    {
        if (TimeManager.Instance == null) return;

        float currentSimHours = TimeManager.Instance.TotalSimulatedHours;
        
        float deltaHours = lastProcessTime >= 0 ? currentSimHours - lastProcessTime : 0f;
        lastProcessTime = currentSimHours;

        int count = activeCrops.Count;
        if (count == 0 || deltaHours <= 0) return;

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

                    // P & K: optimal values from crop data, sensor values from parcel
                    float optN = optimalNs[i];
                    var cropData = crop.CachedCropData;
                    optimalPs[i] = cropData?.requirements?.phosphorus?.optimal ?? (optN * 0.5f);
                    optimalKs[i] = cropData?.requirements?.potassium?.optimal ?? (optN * 0.3f);
                    sensorPs[i] = crop.ParentSensor != null ? crop.ParentSensor.phosphorus : 0f;
                    sensorKs[i] = crop.ParentSensor != null ? crop.ParentSensor.potassium : 0f;

                    // Combined environment multiplier: temperature × moisture × pH
                    float envMult = crop.GetTemperatureMultiplier(currentTemp, db);

                    if (crop.ParentSensor != null && cropData != null)
                    {
                        float optMoist = cropData.requirements?.humidity?.optimal ?? 60f;
                        float maxMoist = cropData.requirements?.humidity?.max ?? 90f;
                        envMult *= RangeMult(crop.ParentSensor.soilMoisture, optMoist * 0.4f, optMoist, maxMoist);

                        float optPH = cropData.requirements?.pH?.optimal ?? 6.5f;
                        float minPH = cropData.requirements?.pH?.min ?? 5.0f;
                        float maxPH = cropData.requirements?.pH?.max ?? 8.0f;
                        envMult *= RangeMult(crop.ParentSensor.soilPH, minPH, optPH, maxPH);
                    }

                    tempMults[i] = envMult;
                }
                else
                {
                    consumeRates[i] = 0f;
                    optimalNs[i] = 0f;
                    sensorNs[i] = 0f;
                    optimalPs[i] = 0f;
                    optimalKs[i] = 0f;
                    sensorPs[i] = 0f;
                    sensorKs[i] = 0f;
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
                optimalPs = optimalPs,
                optimalKs = optimalKs,
                sensorPhosphorus = sensorPs,
                sensorPotassium = sensorKs,
                tempMultipliers = tempMults,
                outConsumedNitrogen = outNitrogen,
                outConsumedPhosphorus = outPhosphorus,
                outConsumedPotassium = outPotassium,
                outGrowthDelta = outGrowth
            };

            job.Schedule(count, 64).Complete();

            // 4. Aplicam rezultatele vizual + consumul NPK
            for (int i = 0; i < count; i++)
            {
                if (activeCrops[i] != null && (outGrowth[i] > 0 || outNitrogen[i] > 0))
                    activeCrops[i].ApplyJobResults(outGrowth[i], outNitrogen[i], outPhosphorus[i], outPotassium[i]);
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

    /// <summary>Cardinal piecewise multiplier matching CropGrowth.CalculateRangeMultiplier.</summary>
    private static float RangeMult(float value, float min, float optimal, float max)
    {
        if (value <= min || value >= max) return 0.1f;
        if (value <= optimal)
            return Mathf.Lerp(0.1f, 1f, (value - min) / (optimal - min));
        return Mathf.Lerp(1f, 0.1f, (value - optimal) / (max - optimal));
    }
}
