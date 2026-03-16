namespace Settings
{
    public static class SimulationSettings
    {
        public static int PlantsPerRow = 4;
        public static int SelectedCropIndex = -1;
        public static float[] SeedCosts;
        public static float[] YieldWeights;
        public static float[] MarketPrices;
        public static float EnergyPrice = 0.20f;
        public static float MinQualityToPlant = 30f;

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
