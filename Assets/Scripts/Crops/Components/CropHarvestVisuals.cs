using UnityEngine;
using System.Collections;

public class CropHarvestVisuals : MonoBehaviour
{
    public CropSettings settings;

    public IEnumerator PlayHarvestRoutine(Transform target, System.Action onComplete)
    {
        if (settings == null) { onComplete?.Invoke(); yield break; }

        Vector3 startPos = target.localPosition;
        Vector3 endPos = startPos + Vector3.down * settings.sinkDepth;
        Vector3 startScale = target.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / settings.sinkDuration;
            float smooth = t * t;
            target.localPosition = Vector3.Lerp(startPos, endPos, smooth);
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, smooth);
            yield return null;
        }

        target.localScale = startScale;
        target.localPosition = startPos;
        onComplete?.Invoke();
    }
}
