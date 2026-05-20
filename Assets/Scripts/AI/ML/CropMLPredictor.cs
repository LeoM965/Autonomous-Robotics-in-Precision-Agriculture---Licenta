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
            TextAsset jsonAsset = Resources.Load<TextAsset>("MLCropModel");
            if (jsonAsset == null)
            {
                Debug.LogWarning("[CropMLPredictor] MLCropModel.json nu a fost gasit in Resources/. Ruleaza train_crop_model.py.");
                return false;
            }

            model = ParseModelManual(jsonAsset.text);
            if (model?.tree == null)
            {
                Debug.LogWarning("[CropMLPredictor] Parsarea modelului ML a esuat.");
                model = null;
                return false;
            }

            Debug.Log($"[CropMLPredictor] Model ML incarcat: {model.classes.Length} clase, {model.features.Length} features.");
            return true;
        }

        private static TreeModel ParseModelManual(string json)
        {
            try
            {
                var m = new TreeModel();
                m.features = ExtractStringArray(json, "\"features\": [", "]");
                m.classes = ExtractStringArray(json, "\"classes\": [", "]");
                
                m.categorical_mappings = new CategoricalMappings();
                m.categorical_mappings.soil_type = ExtractStringArray(json, "\"soil_type\": [", "]");
                m.categorical_mappings.season = ExtractStringArray(json, "\"season\": [", "]");
                m.categorical_mappings.weather = ExtractStringArray(json, "\"weather\": [", "]");

                int treeStart = json.IndexOf("\"tree\": {");
                if (treeStart >= 0)
                {
                    int pos = treeStart + 8;
                    m.tree = ParseNode(json, ref pos);
                }
                return m;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CropMLPredictor] Eroare la parsare manuala: " + e.Message);
                return null;
            }
        }

        private static string[] ExtractStringArray(string json, string startMarker, string endMarker)
        {
            int start = json.IndexOf(startMarker);
            if (start < 0) return new string[0];
            start += startMarker.Length;
            int end = json.IndexOf(endMarker, start);
            if (end < 0) return new string[0];
            string content = json.Substring(start, end - start);
            var parts = content.Split(',');
            var list = new List<string>();
            foreach (var p in parts)
            {
                string clean = p.Trim().Trim('"', '\n', '\r');
                if (!string.IsNullOrEmpty(clean)) list.Add(clean);
            }
            return list.ToArray();
        }

        private static TreeNode ParseNode(string json, ref int pos)
        {
            // Avanseaza pana la '{'
            while (pos < json.Length && json[pos] != '{') pos++;
            pos++; // sarim peste '{'

            TreeNode node = new TreeNode();
            int openBraces = 1;

            while (pos < json.Length && openBraces > 0)
            {
                SkipWhitespace(json, ref pos);
                if (json[pos] == '}') { openBraces--; pos++; continue; }
                if (json[pos] == ',') { pos++; continue; }

                string key = ReadString(json, ref pos);
                SkipWhitespace(json, ref pos);
                if (json[pos] == ':') pos++;
                SkipWhitespace(json, ref pos);

                if (key == "type") node.type = ReadString(json, ref pos);
                else if (key == "feature") node.feature = ReadString(json, ref pos);
                else if (key == "class") node.@class = ReadString(json, ref pos);
                else if (key == "threshold") node.threshold = ReadFloat(json, ref pos);
                else if (key == "conf") node.conf = ReadFloat(json, ref pos);
                else if (key == "left") node.left = ParseNode(json, ref pos);
                else if (key == "right") node.right = ParseNode(json, ref pos);
                else SkipValue(json, ref pos);
            }
            return node;
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
        }

        private static string ReadString(string json, ref int pos)
        {
            if (json[pos] == '"') pos++;
            int start = pos;
            while (pos < json.Length && json[pos] != '"') pos++;
            string val = json.Substring(start, pos - start);
            if (pos < json.Length && json[pos] == '"') pos++;
            return val;
        }

        private static float ReadFloat(string json, ref int pos)
        {
            int start = pos;
            while (pos < json.Length && (char.IsDigit(json[pos]) || json[pos] == '.' || json[pos] == '-' || json[pos] == 'e' || json[pos] == 'E' || json[pos] == '+')) pos++;
            string val = json.Substring(start, pos - start);
            float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result);
            return result;
        }

        private static void SkipValue(string json, ref int pos)
        {
            if (json[pos] == '{')
            {
                int braces = 0;
                do {
                    if (json[pos] == '{') braces++;
                    else if (json[pos] == '}') braces--;
                    pos++;
                } while (pos < json.Length && braces > 0);
            }
            else if (json[pos] == '[')
            {
                int brackets = 0;
                do {
                    if (json[pos] == '[') brackets++;
                    else if (json[pos] == ']') brackets--;
                    pos++;
                } while (pos < json.Length && brackets > 0);
            }
            else if (json[pos] == '"') { ReadString(json, ref pos); }
            else { while (pos < json.Length && json[pos] != ',' && json[pos] != '}' && json[pos] != ']') pos++; }
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
