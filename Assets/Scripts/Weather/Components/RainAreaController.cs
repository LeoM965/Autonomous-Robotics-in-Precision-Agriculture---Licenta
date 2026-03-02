using UnityEngine;
public class RainAreaController : MonoBehaviour
{
    public ParticleSystem rainSystem;
    void Awake()
    {
        if (rainSystem == null)
            rainSystem = GetComponent<ParticleSystem>();
        if (rainSystem == null)
            rainSystem = GetComponentInChildren<ParticleSystem>();
    }

    void OnEnable()
    {
        if (rainSystem != null) rainSystem.Play();
    }

    public void StartRain()
    {
        if (rainSystem != null)
        {
            gameObject.SetActive(true);
            rainSystem.Play();
        }
    }

    public void StopRain()
    {
        if (rainSystem != null)
            rainSystem.Stop();
        gameObject.SetActive(false);
    }

    public void SetIntensity(float multiplier)
    {
        if (rainSystem == null) return;
        var emission = rainSystem.emission;
        emission.rateOverTime = 1000f * multiplier; 
    }
}
