using UnityEngine;
using Sensors.Models;

namespace Sensors.Components
{
    public class SensorVisuals : MonoBehaviour
    {
        [Header("Status Colors")]
        [SerializeField] private Color excellentColor = new Color(0.18f, 0.42f, 0.12f);  // Rich dark fertile green
        [SerializeField] private Color optimalColor = new Color(0.28f, 0.52f, 0.18f);    // Healthy green-brown
        [SerializeField] private Color suboptimalColor = new Color(0.55f, 0.48f, 0.22f);  // Dry earth yellow-brown
        [SerializeField] private Color deficientColor = new Color(0.6f, 0.35f, 0.15f);    // Depleted brown-orange
        [SerializeField] private Color criticalColor = new Color(0.5f, 0.22f, 0.1f);      // Cracked dry red-brown

        private MeshRenderer[] renderers;
        private MaterialPropertyBlock propBlock;
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private void Awake()
        {
            renderers = GetComponentsInChildren<MeshRenderer>(true);
            propBlock = new MaterialPropertyBlock();
        }

        public void Refresh(SoilAnalysis analysis)
        {
            if (renderers == null) return;
            Color targetColor = GetStatusColor(analysis.health);
            ApplyColor(targetColor);
        }

        private Color GetStatusColor(SoilHealthStatus health) => health switch
        {
            SoilHealthStatus.Excellent => excellentColor,
            SoilHealthStatus.Optimal => optimalColor,
            SoilHealthStatus.Suboptimal => suboptimalColor,
            SoilHealthStatus.Deficient => deficientColor,
            SoilHealthStatus.Critical => criticalColor,
            _ => Color.white
        };

        private void ApplyColor(Color color)
        {
            if (renderers == null) return;
            propBlock.SetColor(ColorProp, color);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].SetPropertyBlock(propBlock);
            }
        }
    }
}
