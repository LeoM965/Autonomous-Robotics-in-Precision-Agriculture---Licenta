using UnityEngine;
using TMPro;
using AI.Analytics;
using AI.Models.Decisions;
using Robots.Capabilities.Flight;

namespace UI.Canvas
{
    public class DecisionPanel : InteractivePanel
    {
        private TextMeshProUGUI parcelTxt, chosenTxt, statsTxt;
        private Transform[] bars = new Transform[5];
        private TextMeshProUGUI[] barTexts = new TextMeshProUGUI[5];

        protected override void OnInitialize()
        {
            if (DecisionTracker.Instance == null)
            {
                new GameObject("DecisionTracker").AddComponent<DecisionTracker>();
            }

            
            if (visuals)
            {
                parcelTxt = GetText("Visuals/Row_Parcel/Val"); 
                chosenTxt = GetText("Visuals/Row_Chosen/Val"); 
                statsTxt = visuals.transform.Find("Stats")?.GetComponent<TextMeshProUGUI>();
                
                string[] ids = { "PH", "H", "N", "P", "K" };
                for (int i = 0; i < 5; i++)
                {
                    bars[i] = CanvasHelper.GetFill(visuals.transform, "Bar_" + ids[i] + "/BG/Fill");
                    barTexts[i] = CanvasHelper.GetText(visuals.transform, "Bar_" + ids[i] + "/V");
                }
            }
        }

        protected override void OnRefresh()
        {
            var d = DecisionTracker.Instance?.GetLastDecision(selectedTarget);
            if (d == null) 
            { 
                if (statsTxt) statsTxt.text = "Aștept decizie..."; 
                return; 
            }

            if (parcelTxt) parcelTxt.text = d.parcelName;
            if (chosenTxt) chosenTxt.text = d.chosenOption;
            
            RefreshAlternatives(d);
            RefreshSoilFactors(d);
            RefreshGlobalStats();
        }

        private void RefreshAlternatives(DecisionRecord d)
        {
            for (int i = 1; i <= 3; i++)
            {
                var lText = GetText($"Visuals/Row_Alt{i}/L");
                var vText = GetText($"Visuals/Row_Alt{i}/Val");
                if (lText == null || vText == null) continue;

                if (d.alternatives != null && d.alternatives.Count > i)
                {
                    var alt = d.alternatives[i];
                    lText.text = alt.name.ToUpper();
                    vText.text = $"{alt.score:F1}";
                    lText.color = vText.color = (i == 1) ? CanvasHelper.Warning : CanvasHelper.Subtitle;
                }
                else
                {
                    lText.text = vText.text = "-";
                }
            }
        }

        private void RefreshSoilFactors(DecisionRecord d)
        {
            if (d.factors == null) return;
            float[] fs = { d.factors.phScore, d.factors.humidityScore, d.factors.nitrogenScore, d.factors.phosphorusScore, d.factors.potassiumScore };
            for (int i = 0; i < 5; i++)
            {
                float val = Mathf.Clamp(fs[i], 0, 100);
                if (bars[i]) bars[i].GetComponent<RectTransform>().sizeDelta = new Vector2((val / 100f) * 110f, 8);
                if (barTexts[i]) barTexts[i].text = $"{val:F0}%";
            }
        }

        private void RefreshGlobalStats()
        {
            if (DecisionTracker.Instance != null && statsTxt) 
            {
                float avg = DecisionTracker.Instance.GetAverageScore(selectedTarget);
                statsTxt.text = $"Decizii: {DecisionTracker.Instance.GetTotalDecisions(selectedTarget)} | Scor: {avg:F1}";
            }
        }

        protected override Transform FindTarget(Transform t)
        {
            var op = t.GetComponentInParent<RobotOperator>();
            if (op != null) return op.transform;
            
            var flight = t.GetComponentInParent<AgroBotFlight>();
            if (flight != null) return flight.transform;

            return null;
        }


    }
}
