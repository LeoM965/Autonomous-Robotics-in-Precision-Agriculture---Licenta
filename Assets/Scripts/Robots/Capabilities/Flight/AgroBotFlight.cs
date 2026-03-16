using UnityEngine;
using System.Collections;
using Sensors.Components;
using Robots.Models;

namespace Robots.Capabilities.Flight
{
    public class AgroBotFlight : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FlightSettings settings = new FlightSettings();
        [SerializeField] private Transform flightBody;

        private DroneMotor motor;
        private FlightNavigation navigation;
        private TreatmentSystem treatment;
        
        private FlightState state = FlightState.Initializing;
        private float treatmentTimer;
        private Vector3 lastPosition;
        private OperationRegion region;

        private void Awake()
        {
            motor = gameObject.AddComponent<DroneMotor>();
            navigation = new FlightNavigation();
            treatment = new TreatmentSystem(transform);
            
            if (flightBody == null) flightBody = transform;
            
            // Interaction: Auto-add collider if missing
            if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(2, 1, 2);
            }
        }

        private void Start()
        {
            lastPosition = flightBody.position;
            StartCoroutine(InitializationRoutine());
        }

        private IEnumerator InitializationRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            
            SetupRegion();
            navigation.Initialize(region);
            motor.Initialize(flightBody, settings, region);

            if (navigation.HasTargets)
            {
                navigation.SelectNextTarget();
                state = FlightState.Navigating;
            }
            else StartCoroutine(InitializationRoutine());
        }

        private void SetupRegion()
        {
            var fence = FindFirstObjectByType<FenceGenerator>();
            if (fence != null && fence.zones != null && fence.zones.Length > 0)
                region = OperationRegion.FromZone(GetNearestZone(fence.zones));
            else
                region = new OperationRegion(new Rect(0, 0, 1000, 1000));
        }

        private FenceZone GetNearestZone(FenceZone[] zones)
        {
            float minSqrDist = float.MaxValue;
            int bestIndex = 0;
            Vector3 pos = transform.position;
            for (int i = 0; i < zones.Length; i++)
            {
                Vector2 center = (zones[i].startXZ + zones[i].endXZ) * 0.5f;
                float sqrDist = (pos.x - center.x) * (pos.x - center.x) + (pos.z - center.y) * (pos.z - center.y);
                if (sqrDist < minSqrDist) { minSqrDist = sqrDist; bestIndex = i; }
            }
            return zones[bestIndex];
        }

        private void Update()
        {
            if (state == FlightState.Initializing || navigation == null || motor == null || settings == null) return;

            TrackTelemetery();
            
            EnvironmentalSensor target = navigation.CurrentTarget;
            Vector3 targetPos = (target != null && target.transform != null) ? target.transform.position : flightBody.position;
            targetPos.y = settings.altitude;

            bool isMoving = state == FlightState.Navigating;
            motor.UpdateMovement(targetPos, isMoving);

            if (state == FlightState.Navigating && motor.HasReached(targetPos))
            {
                treatmentTimer = settings.waitTimePerParcel;
                state = FlightState.HoveringAtTarget;
            }
            else if (state == FlightState.HoveringAtTarget)
            {
                if (target == null)
                {
                    navigation.SelectNextTarget();
                    state = FlightState.Navigating;
                    return;
                }

                bool needsNutrients = target.nitrogen < 100f; // Fallback
                var data = CropLoader.Load()?.Get(target.plantedVarietyName);
                if (data?.requirements?.nitrogen != null)
                {
                    needsNutrients = target.nitrogen < data.requirements.nitrogen.optimal;
                }

                if (needsNutrients)
                {
                    treatment.ApplyTreatment(target);
                    treatmentTimer -= Time.deltaTime;
                }
                else
                {
                    treatmentTimer = 0; // Skip if nitrogen is already sufficient
                }

                if (treatmentTimer <= 0)
                {
                    navigation.SelectNextTarget();
                    state = FlightState.Navigating;
                }
            }
        }

        private void TrackTelemetery()
        {
            if (flightBody == null) return;
            float distMoved = Vector3.Distance(flightBody.position, lastPosition);
            if (distMoved > 0.001f && TimeManager.Instance != null)
                TimeManager.Instance.AddDistanceTraveled(distMoved);
            lastPosition = flightBody.position;
        }

        public string GetStatus()
        {
            if (state == FlightState.Initializing || navigation == null) return "Sisteme în pornire...";
            if (navigation.CurrentTarget == null) return "Scanare...";

            string parcelName = navigation.CurrentTarget.name;
            return state switch
            {
                FlightState.Navigating => $"Zbor spre {parcelName}",
                FlightState.HoveringAtTarget => $"Tratare sol activă pe {parcelName} ({(treatmentTimer / settings.waitTimePerParcel * 100f):F0}%)",
                _ => "Idle"
            };
        }
    }
}
