using System;

[Serializable]
public class CropData
{
    public string name;
    public string prefabPath;
    public CropRequirements requirements;
    public float seedCostEUR;
    public float yieldWeightKg;
    public float marketPricePerKg;
    public int growthDays;
    public float nitrogenConsumptionRate;
    public bool isFrostResistant;

    /// <summary>
    /// Temperatura minima de crestere (°C). Sub aceasta, planta nu creste.
    /// Sursa: FAO Crop Information; Porter & Gawith (1999)
    /// </summary>
    public float tempMin = 5f;

    /// <summary>
    /// Temperatura optima de crestere (°C). Rata maxima de crestere.
    /// Sursa: Hatfield & Prueger (2015), USDA Plant Hardiness
    /// </summary>
    public float tempOptimal = 22f;

    /// <summary>
    /// Temperatura maxima de crestere (°C). Peste aceasta, planta nu creste (heat stress).
    /// Sursa: FAO Crop Information; Sánchez et al. (2014)
    /// </summary>
    public float tempMax = 35f;

    public float yieldValueEUR => yieldWeightKg * marketPricePerKg;
    public float GrowthHours => growthDays * 24f;

    /// <summary>
    /// Modelul Cardinal al Temperaturii (Piecewise Linear).
    /// Returneaza un multiplicator intre 0.0 si 1.0 bazat pe temperatura curenta.
    /// 
    /// Referinte:
    /// - Porter, J.R. & Gawith, M. (1999). "Temperatures and the growth and development of wheat"
    /// - Hatfield, J.L. & Prueger, J.H. (2015). "Temperature extremes: Effect on plant growth"
    /// - Sánchez, B. et al. (2014). "Temperatures and the growth and development of maize and rice"
    /// </summary>
    public float GetTemperatureMultiplier(float currentTemp)
    {
        // Sub tempMin sau peste tempMax → crestere zero
        if (currentTemp <= tempMin || currentTemp >= tempMax) return 0f;

        // Intre tempMin si tempOptimal → creste liniar de la 0 la 1
        if (currentTemp <= tempOptimal)
        {
            return (currentTemp - tempMin) / (tempOptimal - tempMin);
        }

        // Intre tempOptimal si tempMax → scade liniar de la 1 la 0
        return (tempMax - currentTemp) / (tempMax - tempOptimal);
    }
}

