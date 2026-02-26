using UnityEngine;
using Sensors.Models;

namespace Sensors.Components
{
    public class SensorVisuals : MonoBehaviour
    {
        [Header("Status Colors")]
        public Color excellentColor = new Color(0.1f, 0.9f, 0.2f);
        public Color optimalColor = new Color(0.2f, 0.7f, 0.3f);
        public Color suboptimalColor = new Color(0.9f, 0.8f, 0.2f);
        public Color deficientColor = new Color(0.9f, 0.4f, 0.1f);
        public Color criticalColor = new Color(0.9f, 0.1f, 0.1f);

        private MeshRenderer[] _renderers;
        private MaterialPropertyBlock _propBlock;
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _renderers = GetComponentsInChildren<MeshRenderer>(true);
            _propBlock = new MaterialPropertyBlock();
        }

        public void Refresh(SoilAnalysis analysis)
        {
            if (_renderers == null) return;
            
            Color targetColor = GetStatusColor(analysis.health);
            ApplyToLabels(targetColor);
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

        private void ApplyToLabels(Color color)
        {
            if (_renderers == null) return;
            
            _propBlock.SetColor(ColorProp, color);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].SetPropertyBlock(_propBlock);
                }
            }
        }
    }
}
