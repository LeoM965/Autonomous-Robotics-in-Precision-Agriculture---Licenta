using UnityEngine;
using System.Collections.Generic;
using Sensors.Components;
using AI.Core;
using Settings;

public class CropHarvester : RobotOperator
{
    [SerializeField] private HarvestConfig config = new HarvestConfig();
    private HarvesterOperation operation;

    protected override void Start()
    {
        var db = CropLoader.Load();
        operation = new HarvesterOperation(transform, movement, energy, config, db);
        Invoke(nameof(Initialize), 5f);
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
    protected override bool IsWorking() => operation.IsHarvesting;
    protected override float GetArriveDistance() => config.arriveDistance;

    protected override void OnArrivedAtParcel(EnvironmentalSensor parcel) => operation.StartHarvesting(parcel);

    protected override void OnAllParcelsComplete()
    {
        state = OperatorState.Idle;
        idleTimer = config.rescanInterval;
    }

    protected override void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer > 0f) return;

        if (SimulationSettings.UseCentralizedScheduling) RequestTask();
        else ScanSequentially();
    }

    protected override string GetWorkingStatus() => $"Harvesting {operation?.CropIndex}/{operation?.TotalCrops}";
    protected override string GetIdleStatus() => idleTimer > 0 ? $"Scanning ({idleTimer:F0}s)" : "Idle";

    private void RequestTask()
    {
        if (TaskManager.Instance == null) 
        {
            Debug.LogError("[CropHarvester] Eroare Critica: Nu exista niciun TaskManager in scena! Robotul nu poate primi sarcini!");
            return;
        }

        var task = TaskManager.Instance.GetNextTask<HarvestTask>(transform.position, false);
        if (task != null)
        {
            var sensor = task.Target.GetComponent<EnvironmentalSensor>();
            if (sensor != null)
            {
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
        parcels = ParcelHelper.GetParcelsInZone(zone, config.minSoilQuality);
        parcels.RemoveAll(p => !HasHarvestableCrops(p));
        DistributeRoundRobin(zone);

        parcelIndex = 0;
        if (parcels.Count > 0) MoveToNextParcel();
        else OnAllParcelsComplete();
    }

    private void DistributeRoundRobin(FenceZone zone)
    {
        var peers = new List<CropHarvester>(FindObjectsByType<CropHarvester>(FindObjectsSortMode.None));
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

    private static bool HasHarvestableCrops(EnvironmentalSensor p)
    {
        foreach (var c in p.activeCrops)
            if (c != null && c.IsHarvestable) return true;
        return false;
    }

    public int TotalHarvested => operation?.TotalHarvested ?? 0;
    public bool IsHarvesting => state == OperatorState.Working;
    
    protected override void AbortOperation()
    {
        if (operation != null && operation.IsHarvesting)
        {
            operation.Abort();
        }
    }
}
