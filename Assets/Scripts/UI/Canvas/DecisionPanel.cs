using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AI.Analytics;
using AI.Models.Decisions;
using Robots.Capabilities.Flight;

namespace UI.Canvas
{
    public class DecisionPanel : InteractivePanel
    {
        private TextMeshProUGUI parcelTxt, chosenTxt, statsTxt;
        private RectTransform[] barFills = new RectTransform[5];
        private Image[] barImages = new Image[5];
        private TextMeshProUGUI[] barTexts = new TextMeshProUGUI[5];
        private TextMeshProUGUI[] altLabels = new TextMeshProUGUI[3];
        private TextMeshProUGUI[] altValues = new TextMeshProUGUI[3];

        protected override void OnInitialize()
        {
            if (DecisionTracker.Instance == null)
                new GameObject("DecisionTracker").AddComponent<DecisionTracker>();

            if (!visuals) return;

            parcelTxt = GetText("Visuals/Row_Parcel/Val");
            chosenTxt = GetText("Visuals/Row_Chosen/Val");
            statsTxt = visuals.transform.Find("Stats")?.GetComponent<TextMeshProUGUI>();

            string[] ids = { "PH", "H", "N", "P", "K" };
            for (int i = 0; i < 5; i++)
            {
                var fill = CanvasHelper.GetFill(visuals.transform, "Bar_" + ids[i] + "/BG/Fill");
                barFills[i] = fill;
                barImages[i] = fill?.GetComponent<Image>();
                barTexts[i] = CanvasHelper.GetText(visuals.transform, "Bar_" + ids[i] + "/V");
            }

            for (int i = 1; i <= 3; i++)
            {
                altLabels[i - 1] = GetText($"Visuals/Row_Alt{i}/L");
                altValues[i - 1] = GetText($"Visuals/Row_Alt{i}/Val");
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
            RefreshGlobalStats(d);
        }

        private void RefreshAlternatives(DecisionRecord d)
        {
            for (int i = 1; i <= 3; i++)
            {
                var lText = altLabels[i - 1];
                var vText = altValues[i - 1];
                if (lText == null || vText == null) continue;

                if (d.alternatives != null && d.alternatives.Count > i)
                {
                    var alt = d.alternatives[i];
                    lText.text = alt.name.ToUpper();
                    vText.text = $"{alt.score:F1}";
                    lText.color = vText.color = (i == 1) ? CanvasHelper.Warning : CanvasHelper.Subtitle;
                }
                else
                    lText.text = vText.text = "-";
            }
        }

        private void RefreshSoilFactors(DecisionRecord d)
        {
            if (d.factors == null) return;

            float[] fs = { d.factors.phScore, d.factors.humidityScore, d.factors.nitrogenScore, d.factors.phosphorusScore, d.factors.potassiumScore };

            for (int i = 0; i < 5; i++)
            {
                float val = Mathf.Clamp(fs[i], 0, 100);

                if (barFills[i])
                {
                    float targetW = (val / 100f) * 110f;
                    float smoothW = Mathf.Lerp(barFills[i].sizeDelta.x, targetW, Time.deltaTime * 6f);
                    barFills[i].sizeDelta = new Vector2(smoothW, 8);
                }

                if (barImages[i])
                {
                    barImages[i].color = val < 40f
                        ? Color.Lerp(CanvasHelper.Bad, CanvasHelper.Warning, val / 40f)
                        : Color.Lerp(CanvasHelper.Warning, CanvasHelper.Good, (val - 40f) / 60f);
                }

                if (barTexts[i]) barTexts[i].text = $"{val:F0}%";
            }
        }

        private void RefreshGlobalStats(DecisionRecord d)
        {
            if (DecisionTracker.Instance == null || !statsTxt) return;
            int total = DecisionTracker.Instance.GetTotalDecisions(selectedTarget);
            float avg = DecisionTracker.Instance.GetAverageScore(selectedTarget);
            Color nvColor = d.netValue >= 0 ? CanvasHelper.Good : CanvasHelper.Bad;
            statsTxt.text = $"Decizii: {total} | Scor: {avg:F1} | <color=#{ColorUtility.ToHtmlStringRGB(nvColor)}>NetValue: {d.netValue:F1}</color>";
        }

        protected override Transform FindTarget(Transform t)
        {
            var op = t.GetComponentInParent<RobotOperator>();
            if (op != null) return op.transform;

            var flight = t.GetComponentInParent<AgroBotFlight>();
            return flight != null ? flight.transform : null;
        }
    }
}
