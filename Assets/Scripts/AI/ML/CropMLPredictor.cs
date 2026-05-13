using UnityEngine;
using System.Collections.Generic;

namespace AI.ML
{
    /// <summary>
    /// Runtime C# interpreter for a Decision Tree classifier exported as JSON
    /// from sklearn (train_crop_model.py → MLCropModel.json).
    /// Loads the tree structure at startup and performs O(depth) inference per prediction.
    /// </summary>
    public static class CropMLPredictor
    {
        private static TreeModel model;
        private static bool loadAttempted;

        // ── JSON-serializable model structure ──
        [System.Serializable]
        private class TreeModel
        {
            public string[] features;
            public CategoricalMappings categorical_mappings;
            public string[] classes;
            public TreeNode tree;
        }

        [System.Serializable]
        private class CategoricalMappings
        {
            public string[] soil_type;
            public string[] season;
            public string[] weather;
        }

        [System.Serializable]
        private class TreeNode
        {
            public string type;       // "split" or "leaf"
            public string feature;    // split feature name
            public float threshold;   // split threshold
            public TreeNode left;     // <= threshold
            public TreeNode right;    // > threshold
            public string @class;     // leaf: predicted class name
            public float conf;        // leaf: confidence 0-1
        }

        // ── Public API ──

        public static bool IsLoaded => model != null;

        /// <summary>
        /// Loads the model from Resources/MLCropModel.json. Safe to call multiple times.
        /// </summary>
        public static bool Load()
        {
            if (model != null) return true;
            if (loadAttempted) return false;

            loadAttempted = true;
            TextAsset json = Resources.Load<TextAsset>("MLCropModel");
            if (json == null)
            {
                Debug.LogWarning("[CropMLPredictor] MLCropModel.json nu a fost gasit in Resources/. Ruleaza train_crop_model.py.");
                return false;
            }

            model = JsonUtility.FromJson<TreeModel>(json.text);
            if (model?.tree == null)
            {
                Debug.LogWarning("[CropMLPredictor] Parsarea modelului ML a esuat.");
                model = null;
                return false;
            }

            Debug.Log($"[CropMLPredictor] Model ML incarcat: {model.classes.Length} clase, {model.features.Length} features.");
            return true;
        }

        /// <summary>
        /// Predicts the best crop variety for the given environmental conditions.
        /// Returns null if model is not loaded or prediction fails.
        /// </summary>
        public static PredictionResult Predict(float pH, float moisture, float nitrogen,
            float phosphorus, float potassium, float quality, float temperature,
            string soilType, string season, string weather)
        {
            if (!Load()) return null;

            // Build feature vector in the same order as model.features
            float[] features = new float[model.features.Length];
            for (int i = 0; i < model.features.Length; i++)
            {
                features[i] = model.features[i] switch
                {
                    "pH" => pH,
                    "moisture" => moisture,
                    "nitrogen" => nitrogen,
                    "phosphorus" => phosphorus,
                    "potassium" => potassium,
                    "quality" => quality,
                    "temperature" => temperature,
                    "soil_type" => EncodeCategorical(model.categorical_mappings.soil_type, soilType),
                    "season" => EncodeCategorical(model.categorical_mappings.season, season),
                    "weather" => EncodeCategorical(model.categorical_mappings.weather, weather),
                    _ => 0f
                };
            }

            // Traverse tree
            TreeNode node = model.tree;
            while (node != null && node.type == "split")
            {
                int featureIdx = GetFeatureIndex(node.feature);
                if (featureIdx < 0) return null;

                float value = features[featureIdx];
                node = value <= node.threshold ? node.left : node.right;
            }

            if (node == null || node.type != "leaf")
                return null;

            return new PredictionResult
            {
                variety = node.@class,
                confidence = node.conf
            };
        }

        /// <summary>
        /// Convenience overload using sensor data directly.
        /// </summary>
        public static PredictionResult Predict(Sensors.Components.EnvironmentalSensor parcel)
        {
            if (parcel?.composition == null) return null;

            string soilType = parcel.detectedType.ToString();
            string season = TimeManager.Instance != null
                ? TimeManager.Instance.GetCurrentSeason().ToString()
                : "Spring";
            string weather = Weather.Components.WeatherSystem.Instance != null
                ? Weather.Components.WeatherSystem.Instance.CurrentWeather.ToString()
                : "Sunny";
            float temperature = Weather.Components.WeatherSystem.Instance != null
                ? Weather.Components.WeatherSystem.Instance.CurrentTemperature
                : 20f;

            return Predict(
                parcel.soilPH, parcel.soilMoisture,
                parcel.nitrogen, parcel.phosphorus, parcel.potassium,
                parcel.soilQuality, temperature,
                soilType, season, weather
            );
        }

        // ── Internal helpers ──

        private static float EncodeCategorical(string[] categories, string value)
        {
            if (categories == null || string.IsNullOrEmpty(value)) return -1f;
            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] == value) return i;
            }
            return -1f; // Unknown category
        }

        private static int GetFeatureIndex(string featureName)
        {
            if (model?.features == null) return -1;
            for (int i = 0; i < model.features.Length; i++)
            {
                if (model.features[i] == featureName) return i;
            }
            return -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            model = null;
            loadAttempted = false;
        }
    }

    public class PredictionResult
    {
        public string variety;
        public float confidence;
    }
}
