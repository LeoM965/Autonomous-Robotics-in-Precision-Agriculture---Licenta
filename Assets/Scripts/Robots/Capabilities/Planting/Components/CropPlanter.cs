using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using AI.Core;
using Settings;

public class CropPlanter : RobotOperator
{
    [SerializeField] private PlantingConfig config = new PlantingConfig();
    private PlanterOperation operation;

    [Header("Inventory")]
    public int maxSeeds = 150;
    public int currentSeeds = 150;

    protected override void Start()
    {
        CropDatabase cropDB = CropLoader.Load();
        operation = new PlanterOperation(this, transform, movement, energy, config, cropDB);
        Invoke(nameof(Initialize), 3f);
    }

    protected override void Update()
    {
        base.Update();

        // Sincronizăm capacitatea maximă cu setările globale din simulator
        maxSeeds = SimulationSettings.MaxSeedsCapacity;
        currentSeeds = Mathf.Min(currentSeeds, maxSeeds);

        // Când se încarcă bateria la bază (andocat), refacem și stocul de semințe
        if (energy != null && energy.IsCharging)
        {
            currentSeeds = maxSeeds;
        }
    }

    public void RefuelSeeds()
    {
        if (energyManager != null)
        {
            energyManager.ForceGoToCharger();
        }
    }

    private void OnEnable()  => SimulationSettings.OnSettingsChanged += OnSettingsChanged;
    protected override void OnDisable()
    {
        base.OnDisable();
        SimulationSettings.OnSettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        if (state == OperatorState.Idle || state == OperatorState.MovingToParcel)
        {
            state = OperatorState.Idle;
            idleTimer = 0f;
        }
    }

    private void Initialize()
    {
        if (SimulationSettings.UseCentralizedScheduling) RequestTask();
        else ScanSequentially();
    }

    protected override void UpdateOperation() => operation.Update();
    protected override bool IsWorking() => operation.IsPlanting;
    protected override float GetArriveDistance() => config.arriveDistance;

    protected override void OnArrivedAtParcel(EnvironmentalSensor parcel) => operation.StartPlanting(parcel);

    protected override void OnAllParcelsComplete()
    {
        movement.Stop();
        state = OperatorState.Idle;
        idleTimer = config.rescanInterval;
        movement.ClearTarget();
    }

    protected override void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer > 0f) return;

        if (SimulationSettings.UseCentralizedScheduling) RequestTask();
        else ScanSequentially();
    }

    private void RequestTask()
    {
        if (TaskManager.Instance == null) return;

        var task = TaskManager.Instance.GetNextTask<PlantingTask>(transform.position, false);
        if (task != null)
        {
            var sensor = task.Target.GetComponent<EnvironmentalSensor>();
            if (sensor != null && !PlantingPositionGenerator.IsParcelFullyPlanted(sensor, config))
            {
                operation.SetTaskValue(task.NetValue);
                parcels.Clear();
                parcels.Add(sensor);
                parcelIndex = 0;
                MoveToNextParcel();
                return;
            }
        }
        OnAllParcelsComplete();
    }

    private void ScanSequentially()
    {
        FenceZone zone = ZoneHelper.GetZoneAt(transform.position);
        var rawParcels = ParcelHelper.GetParcelsInZone(zone, config.minSoilQuality);
        
        parcels = new List<EnvironmentalSensor>();
        var db = CropLoader.Load();
        
        foreach (var p in rawParcels)
        {
            if (PlantingPositionGenerator.IsParcelFullyPlanted(p, config)) continue;
            
            // Only add parcels that can actually be planted right now (avoids infinite looping)
            var crop = CropSelector.SelectBestCrop(db, p, transform, 1, 0f, false);
            if (crop != null) parcels.Add(p);
        }

        DistributeRoundRobin(zone);

        parcelIndex = 0;
        if (parcels.Count > 0) MoveToNextParcel();
        else OnAllParcelsComplete();
    }

    private void DistributeRoundRobin(FenceZone zone)
    {
        var peers = new List<CropPlanter>(FindObjectsByType<CropPlanter>(FindObjectsSortMode.None));
        peers.RemoveAll(r => ZoneHelper.GetZoneAt(r.transform.position) != zone);
        peers.Sort((a, b) => a.name.CompareTo(b.name));

        int myIndex = peers.IndexOf(this);
        int total = peers.Count;
        if (myIndex < 0 || total <= 1) return;

        var subset = new List<EnvironmentalSensor>();
        for (int i = myIndex; i < parcels.Count; i += total)
            subset.Add(parcels[i]);
        parcels = subset;
    }

    protected override string GetWorkingStatus() => $"Planting ({currentSeeds}/{maxSeeds} sem.)";
    protected override string GetIdleStatus() => idleTimer > 0 ? $"Scanning ({idleTimer:F0}s, {currentSeeds} sem.)" : $"Scanning ({currentSeeds} sem.)";

    public bool IsPlanting => state == OperatorState.Working;
    public int PlantsPlaced => operation?.TotalPlantsPlaced ?? 0;
    public float TotalSeedCost => operation?.TotalCost ?? 0;
    
    protected override void AbortOperation()
    {
        if (operation != null && operation.IsPlanting)
        {
            operation.Abort();
        }
    }
}