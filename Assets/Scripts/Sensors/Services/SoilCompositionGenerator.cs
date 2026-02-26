using UnityEngine;
using Sensors.Models;

namespace Sensors.Services
{
    public static class SoilCompositionGenerator
    {
        public static SoilComposition Generate(AgroSoilType type) => type switch
        {
            AgroSoilType.HighlyFertile => GenerateFertile(),
            AgroSoilType.Waterlogged => GenerateWet(),
            AgroSoilType.SandyArid => GenerateSandy(),
            AgroSoilType.ChemicallyImbalanced => GenerateImbalanced(),
            _ => GenerateStandard()
        };

        private static SoilComposition GenerateStandard() => new SoilComposition
        {
            moisture = Random.Range(35f, 55f),
            pH = Random.Range(6.2f, 7.0f),
            nitrogen = Random.Range(60f, 100f),
            phosphorus = Random.Range(18f, 30f),
            potassium = Random.Range(150f, 220f)
        };

        private static SoilComposition GenerateFertile() => new SoilComposition
        {
            moisture = Random.Range(50f, 65f),
            pH = Random.Range(6.5f, 7.2f),
            nitrogen = Random.Range(120f, 180f),
            phosphorus = Random.Range(40f, 70f),
            potassium = Random.Range(240f, 320f)
        };

        private static SoilComposition GenerateWet() => new SoilComposition
        {
            moisture = Random.Range(75f, 95f),
            pH = Random.Range(5.8f, 6.4f),
            nitrogen = Random.Range(80f, 120f),
            phosphorus = Random.Range(20f, 40f),
            potassium = Random.Range(120f, 180f)
        };

        private static SoilComposition GenerateSandy() => new SoilComposition
        {
            moisture = Random.Range(10f, 25f),
            pH = Random.Range(6.8f, 7.8f),
            nitrogen = Random.Range(25f, 50f),
            phosphorus = Random.Range(10f, 20f),
            potassium = Random.Range(80f, 130f)
        };

        private static SoilComposition GenerateImbalanced() => new SoilComposition
        {
            moisture = Random.Range(20f, 40f),
            pH = Random.Range(4.5f, 5.2f),
            nitrogen = Random.Range(20f, 45f),
            phosphorus = Random.Range(5f, 15f),
            potassium = Random.Range(50f, 90f)
        };
    }
}
