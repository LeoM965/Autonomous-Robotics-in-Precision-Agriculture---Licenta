using System.Collections;
using UnityEngine;
using Robots.Models;
using Robots.Movement.Interfaces;
using Sensors.Components;
using Settings;

namespace Robots.Capabilities.Flight
{
    [SelectionBase]
    public class AgroBotFlight : MonoBehaviour, IRobotMovement
    {
        [Header("Settings")]
        [SerializeField] private FlightSettings settings = new FlightSettings();
        [SerializeField] private Transform flightBody;

        private DroneMotor motor;
        private FlightNavigation navigation;
        private TreatmentSystem treatment;
        private RobotEnergy energy;
        private RobotEnergyManager energyManager;

        private FlightState state = FlightState.Initializing;
        private float treatmentTimer;
        private float idleRescanTimer;
        private Vector3? manualTarget;

        public bool HasTarget => manualTarget.HasValue || navigation?.CurrentTarget != null;
        public bool HasArrived => motor.HasReached(manualTarget ?? GetTargetPosition(navigation.CurrentTarget));

        public void SetTarget(Vector3 target) => manualTarget = target;
        public void Stop() { manualTarget = null; }

        public void IgnoreCollisionWith(Collider target, bool ignore)
        {
            var myCol = GetComponent<Collider>();
            if (myCol != null && target != null) Physics.IgnoreCollision(myCol, target, ignore);
        }

        private void Awake()
        {
            motor = gameObject.AddComponent<DroneMotor>();
            navigation = new FlightNavigation();
            treatment = new TreatmentSystem(transform, navigation);
            energy = GetComponent<RobotEnergy>() ?? gameObject.AddComponent<RobotEnergy>();
            energyManager = new RobotEnergyManager(transform, energy, this);

            if (flightBody == null) flightBody = transform;

            if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(2, 1, 2);
            }
        }

        private void OnEnable()
        {
            SimulationSettings.OnSettingsChanged += UpdateFromSettings;
        }

        private void OnDisable()
        {
            SimulationSettings.OnSettingsChanged -= UpdateFromSettings;
        }

        private void Start()
        {
            UpdateFromSettings();
            StartCoroutine(InitializationRoutine());
        }

        private void UpdateFromSettings()
        {
            var data = RobotDataLoader.FindByName(name);
            if (data != null)
            {
                settings.speed = data.maxSpeed;
            }
        }

        private void Update()
        {
            if (state == FlightState.Initializing || navigation == null || motor == null) return;
            energyManager.Update();
            if (energyManager.IsHeadingToCharger) state = FlightState.Charging;
            ExecuteStateLogic();
        }

        private IEnumerator InitializationRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            navigation.SetupRegion(transform);
            motor.Initialize(flightBody, settings, navigation.Region);
            if (navigation.HasTargets)
            {
                navigation.SelectNextTarget();
                state = FlightState.Navigating;
            }
            else StartCoroutine(InitializationRoutine());
        }

        private void ExecuteStateLogic()
        {
            energy.SetWorking(state == FlightState.HoveringAtTarget);
            switch (state)
            {
                case FlightState.Charging: HandleChargingState(); break;
                case FlightState.Navigating: HandleNavigationState(); break;
                case FlightState.HoveringAtTarget: HandleTreatmentState(); break;
                case FlightState.Idle: HandleIdleState(); break;
            }
        }

        private void HandleChargingState()
        {
            if (manualTarget.HasValue) 
            {
                Vector3 target = manualTarget.Value + new Vector3(0, 0, -3f);
                motor.UpdateMovement(target, true);
            }
            else if (energy.IsCharging)
            {
                motor.UpdateMovement(transform.position, false); // Stay put ONLY when actually charging
            }
            // If heading to charger, do nothing and let EnergyManager/Motor handle it

            if (!energy.IsCharging && !energyManager.IsHeadingToCharger)
            {
                manualTarget = null;
                navigation.SelectNextTarget();
                state = FlightState.Navigating;
            }
        }

        private void HandleNavigationState()
        {
            Vector3 target = GetTargetPosition(navigation.CurrentTarget);
            motor.UpdateMovement(target, true);
            if (motor.HasReached(target))
            {
                treatmentTimer = settings.waitTimePerParcel;
                state = FlightState.HoveringAtTarget;
            }
        }

        private void HandleTreatmentState()
        {
            if (navigation.CurrentTarget == null) { AnalyzeNextTask(); return; }
            motor.UpdateMovement(GetTargetPosition(navigation.CurrentTarget), false);
            treatment.ProcessTreatment(navigation.CurrentTarget, ref treatmentTimer);
            if (treatmentTimer <= 0) AnalyzeNextTask();
        }

        private void AnalyzeNextTask()
        {
            var next = navigation.SelectNextTarget();
            if (next == null)
            {
                // Nicio parcela nu mai are nevoie de tratament
                idleRescanTimer = 5f;
                state = FlightState.Idle;
                return;
            }

            float dist = Vector3.Distance(flightBody.position, next.transform.position);
            if (energyManager.CheckBattery(dist, settings.waitTimePerParcel))
            {
                state = FlightState.Navigating;
            }
            else state = FlightState.Charging;
        }

        private void HandleIdleState()
        {
            // Drona planeaza pe loc si re-verifica periodic daca vreo parcela are nevoie de tratament
            motor.UpdateMovement(flightBody.position, false);
            idleRescanTimer -= Time.deltaTime;
            if (idleRescanTimer <= 0f)
            {
                navigation.RefreshParcels();
                var next = navigation.SelectNextTarget();
                if (next != null)
                {
                    state = FlightState.Navigating;
                }
                else
                {
                    idleRescanTimer = 5f;
                }
            }
        }

        private Vector3 GetTargetPosition(EnvironmentalSensor target) => GetTargetPosition(target != null ? target.transform.position : flightBody.position);
        private Vector3 GetTargetPosition(Vector3 raw) { var p = raw; p.y = settings.altitude; return p; }

        public string GetStatus()
        {
            if (state == FlightState.Initializing) return "Sisteme în pornire...";
            if (navigation?.CurrentTarget == null) return "Scanare câmp...";
            return state switch
            {
                FlightState.Navigating => "Zbor spre " + navigation.CurrentTarget.name,
                FlightState.HoveringAtTarget => "Tratare sol în desfășurare pe " + navigation.CurrentTarget.name,
                FlightState.Charging => "Se deplasează la încărcare...",
                FlightState.Idle => "Idle - Nicio parcelă nu necesită tratament",
                _ => "Idle"
            };
        }
    }
}
