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

        // Iesirile (rezultatele calculului)
        public NativeArray<float> outConsumedNitrogen;
        public NativeArray<float> outGrowthDelta;

        public void Execute(int i)
        {
            // Calculam un multiplicator de azot (clampat intre 0 si 1)
            float nMultiplier = 1f;
            float optN = optimalNs[i];
            
            if (optN > 0f)
            {
                nMultiplier = Mathf.Clamp(sensorNitrogens[i] / optN, 0f, 1f);
            }

            // Multiplicatorul de temperatura cardinala (per cultura)
            float tempMult = tempMultipliers[i];

            // Calculam cata resursa va consuma si cat va creste
            outConsumedNitrogen[i] = consumeRates[i] * deltaHours;
            outGrowthDelta[i] = deltaHours * weatherMultiplier * nMultiplier * tempMult;
        }
    }
}
