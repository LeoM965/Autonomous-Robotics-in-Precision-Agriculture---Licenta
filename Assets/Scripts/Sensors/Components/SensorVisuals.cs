using UnityEngine;
using Sensors.Models;

namespace Sensors.Components
{
    public class SensorVisuals : MonoBehaviour
    {
        [Header("Status Colors")]
        [SerializeField] private Color excellentColor = new Color(0.1f, 0.9f, 0.2f);
        [SerializeField] private Color optimalColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color suboptimalColor = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private Color deficientColor = new Color(0.9f, 0.4f, 0.1f);
        [SerializeField] private Color criticalColor = new Color(0.9f, 0.1f, 0.1f);

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
