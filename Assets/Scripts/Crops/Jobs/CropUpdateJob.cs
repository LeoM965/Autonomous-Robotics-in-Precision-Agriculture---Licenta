using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Crops.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    public struct CropUpdateJob : IJobParallelFor
    {
        public float deltaHours;
        public float weatherMultiplier;

        [ReadOnly] public NativeArray<float> consumeRates;
        [ReadOnly] public NativeArray<float> optimalNs;
        [ReadOnly] public NativeArray<float> sensorNitrogens;
        [ReadOnly] public NativeArray<float> tempMultipliers;

        // P & K inputs
        [ReadOnly] public NativeArray<float> optimalPs;
        [ReadOnly] public NativeArray<float> optimalKs;
        [ReadOnly] public NativeArray<float> sensorPhosphorus;
        [ReadOnly] public NativeArray<float> sensorPotassium;

        // Iesirile (rezultatele calculului)
        public NativeArray<float> outConsumedNitrogen;
        public NativeArray<float> outConsumedPhosphorus;
        public NativeArray<float> outConsumedPotassium;
        public NativeArray<float> outGrowthDelta;

        public void Execute(int i)
        {
            // Calculam un multiplicator per nutrient (clampat intre 0 si 1)
            float nMult = 1f, pMult = 1f, kMult = 1f;
            float optN = optimalNs[i];
            float optP = optimalPs[i];
            float optK = optimalKs[i];
            
            if (optN > 0f) nMult = Mathf.Clamp(sensorNitrogens[i] / optN, 0f, 1f);
            if (optP > 0f) pMult = Mathf.Clamp(sensorPhosphorus[i] / optP, 0f, 1f);
            if (optK > 0f) kMult = Mathf.Clamp(sensorPotassium[i] / optK, 0f, 1f);

            // Nutrient multiplier = worst satisfaction (limiting factor)
            float nutrientMult = Mathf.Min(nMult, Mathf.Min(pMult, kMult));

            // Multiplicatorul de temperatura cardinala (per cultura)
            float tempMult = tempMultipliers[i];

            // Calculam cata resursa va consuma si cat va creste
            float nRate = consumeRates[i];
            outConsumedNitrogen[i] = nRate * deltaHours;
            outConsumedPhosphorus[i] = nRate * 0.5f * deltaHours;  // P = 50% of N rate
            outConsumedPotassium[i] = nRate * 0.3f * deltaHours;   // K = 30% of N rate
            outGrowthDelta[i] = deltaHours * weatherMultiplier * nutrientMult * tempMult;
        }
    }
}
