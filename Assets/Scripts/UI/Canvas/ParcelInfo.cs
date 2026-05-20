using UnityEngine;
using Sensors.Components;
using TMPro;

namespace UI.Canvas
{
    public class ParcelInfo : InteractivePanel
    {
        private TextMeshProUGUI idTxt, qualityTxt, moistureTxt, phTxt, nTxt, pTxt, kTxt, cropTxt, stageTxt, progressTxt;

        protected override void OnInitialize()
        {
            idTxt = GetText("Visuals/Row_Name/Val");
            qualityTxt = GetText("Visuals/Row_Quality/Val");
            moistureTxt = GetText("Visuals/Row_Moisture/Val");
            phTxt = GetText("Visuals/Row_PH/Val");
            nTxt = GetText("Visuals/Row_N/Val");
            pTxt = GetText("Visuals/Row_P/Val");
            kTxt = GetText("Visuals/Row_K/Val");
            cropTxt = GetText("Visuals/Row_Crops/Val");
            stageTxt = GetText("Visuals/Row_Stage/Val");
            progressTxt = GetText("Visuals/Row_Progress/Val");
        }

        protected override void OnRefresh()
        {
            var sel = selectedTarget.GetComponent<EnvironmentalSensor>();
            if (sel == null) return;

            if (idTxt) idTxt.text = sel.name;
            if (qualityTxt) qualityTxt.text = $"{sel.soilQuality:F1}%";
            if (moistureTxt) moistureTxt.text = $"{sel.soilMoisture:F1}%";
            bool isEmpty = string.IsNullOrEmpty(sel.plantedVarietyName) || sel.plantedVarietyName == "None";
            var cropData = isEmpty ? null : CropLoader.Load()?.Get(sel.plantedVarietyName);

            if (phTxt) 
            {
                if (isEmpty || cropData?.requirements?.pH == null)
                {
                    phTxt.text = $"{sel.soilPH:F1}";
                }
                else
                {
                    float optPH = cropData.requirements.pH.optimal;
                    float diff = Mathf.Abs(sel.soilPH - optPH);
                    if (diff <= 0.2f)
                        phTxt.text = $"<color=#5FD878>{sel.soilPH:F1}</color> / {optPH:F1}";
                    else if (diff <= 0.5f)
                        phTxt.text = $"<color=#E8C44A>{sel.soilPH:F1}</color> / {optPH:F1}";
                    else
                        phTxt.text = $"<color=#E85555>{sel.soilPH:F1}</color> / {optPH:F1}";
                }
            }

            // NPK: show current value + deficit vs optimal
            FormatNutrient(nTxt, sel.nitrogen, cropData?.requirements?.nitrogen?.optimal ?? 0f, isEmpty);
            FormatNutrient(pTxt, sel.phosphorus, cropData?.requirements?.phosphorus?.optimal ?? 0f, isEmpty);
            FormatNutrient(kTxt, sel.potassium, cropData?.requirements?.potassium?.optimal ?? 0f, isEmpty);

            if (isEmpty)
            {
                var prediction = AI.ML.CropMLPredictor.Predict(sel);
                if (prediction != null)
                {
                    if (cropTxt) cropTxt.text = $"<color=#E8C44A>ML: {prediction.variety}</color>";
                    if (stageTxt) stageTxt.text = $"<color=#E8C44A>Încredere: {(prediction.confidence * 100f):F0}%</color>";
                }
                else
                {
                    if (cropTxt) cropTxt.text = "Niciuna";
                    if (stageTxt) stageTxt.text = "-";
                }
                if (progressTxt) progressTxt.text = "-";
            }
            else
            {
                if (cropTxt) cropTxt.text = sel.plantedVarietyName;
                if (stageTxt) stageTxt.text = GetStage(sel.currentGrowthStage);
                if (progressTxt) progressTxt.text = $"{sel.growthProgress:F1}%";
            }
        }

        private void FormatNutrient(TextMeshProUGUI txt, float current, float optimal, bool noCrop)
        {
            if (txt == null) return;

            if (noCrop || optimal <= 0f)
            {
                txt.text = $"{current:F1}";
                return;
            }

            float pct = (current / optimal) * 100f;
            string diffStr = $" ({pct:F0}%)";

            // Color: green = surplus, yellow = marginal (<95%), red = critical (<60%)
            if (pct >= 95f)
                txt.text = $"<color=#5FD878>{current:F1}</color> / {optimal:F0}{diffStr}";
            else if (pct >= 60f)
                txt.text = $"<color=#E8C44A>{current:F1}</color> / {optimal:F0}{diffStr}";
            else
                txt.text = $"<color=#E85555>{current:F1}</color> / {optimal:F0}{diffStr}";
        }

        protected override Transform FindTarget(Transform t)
        {
            var s = t.GetComponentInParent<EnvironmentalSensor>();
            return s != null ? s.transform : null;
        }

        private string GetStage(CropStage s) => s switch
        {
            CropStage.Seed => "Sămânță",
            CropStage.Seedling => "Răsad",
            CropStage.Growing => "Crestere",
            CropStage.Mature => "Matur",
            _ => "N/A"
        };


    }
}
