namespace Settings
{
    public static class SimulationSettings
    {
        public static System.Action OnSettingsChanged;
        public static int PlantsPerRow = 4;
        public static int SelectedCropIndex = -1;
        public static float[] SeedCosts;
        public static float[] YieldWeights;
        public static float[] MarketPrices;
        public static float EnergyPrice = 0.20f;
        public static float MinQualityToPlant = 30f;

        // Per-type, per-zone robot counts: RobotCounts[typeIndex][zoneIndex]
        public static int[][] RobotCounts;
        public static int RobotTypeCount;
        public static int ZoneCount;
        public static string[] RobotTypeNames;

        public static void InitRobotCounts(int types, int zones, string[] typeNames, int defaultCount = 1)
        {
            RobotTypeCount = types;
            ZoneCount = zones;
            RobotTypeNames = typeNames;
            if (RobotCounts != null && RobotCounts.Length == types
                && RobotCounts[0] != null && RobotCounts[0].Length == zones)
                return; // Already initialized with correct dimensions
            RobotCounts = new int[types][];
            for (int t = 0; t < types; t++)
            {
                RobotCounts[t] = new int[zones];
                for (int z = 0; z < zones; z++)
                    RobotCounts[t][z] = defaultCount;
            }
        }

        public static int GetCountForTypeZone(int typeIdx, int zoneIdx)
        {
            if (RobotCounts == null || typeIdx < 0 || typeIdx >= RobotCounts.Length) return 0;
            if (zoneIdx < 0 || zoneIdx >= RobotCounts[typeIdx].Length) return 0;
            return RobotCounts[typeIdx][zoneIdx];
        }

        public static void SetCountForTypeZone(int typeIdx, int zoneIdx, int count)
        {
            if (RobotCounts == null || typeIdx < 0 || typeIdx >= RobotCounts.Length) return;
            if (zoneIdx < 0 || zoneIdx >= RobotCounts[typeIdx].Length) return;
            RobotCounts[typeIdx][zoneIdx] = count;
        }

        // Per-crop NPK requirements (Buffered ranges)
        public static float[] N_Min, N_Opt, N_Max;
        public static float[] P_Min, P_Opt, P_Max;
        public static float[] K_Min, K_Opt, K_Max;
        public static bool IsInitialized => SeedCosts != null && SeedCosts.Length > 0;

        public static void InitFromDatabase(CropDatabase db)
        {
            if (db?.crops == null) return;
            if (db.settings != null)
            {
                MinQualityToPlant = db.settings.minQualityToPlant;
            }

            int n = db.crops.Length;
            SeedCosts = new float[n];
            YieldWeights = new float[n];
            MarketPrices = new float[n];

            N_Min = new float[n]; N_Opt = new float[n]; N_Max = new float[n];
            P_Min = new float[n]; P_Opt = new float[n]; P_Max = new float[n];
            K_Min = new float[n]; K_Opt = new float[n]; K_Max = new float[n];

            for (int i = 0; i < n; i++)
            {
                var crop = db.crops[i];
                SeedCosts[i] = crop.seedCostEUR;
                YieldWeights[i] = crop.yieldWeightKg;
                MarketPrices[i] = crop.marketPricePerKg;

                if (crop.requirements != null)
                {
                    N_Min[i] = crop.requirements.nitrogen.min;
                    N_Opt[i] = crop.requirements.nitrogen.optimal;
                    N_Max[i] = crop.requirements.nitrogen.max;
                    
                    P_Min[i] = crop.requirements.phosphorus.min;
                    P_Opt[i] = crop.requirements.phosphorus.optimal;
                    P_Max[i] = crop.requirements.phosphorus.max;
                    
                    K_Min[i] = crop.requirements.potassium.min;
                    K_Opt[i] = crop.requirements.potassium.optimal;
                    K_Max[i] = crop.requirements.potassium.max;
                }
            }
        }
    }
}
