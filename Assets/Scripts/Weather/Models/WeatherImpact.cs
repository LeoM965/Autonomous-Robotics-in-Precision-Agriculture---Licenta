using System.Collections.Generic;

namespace Weather.Models
{
    public readonly struct WeatherImpact
    {
        public readonly float temperatureMin;
        public readonly float temperatureMax;
        public readonly float cropGrowth;
        public readonly float movementSpeed;
        public readonly float precipitationRate;

        public WeatherImpact(float tempMin, float tempMax, float crop, float movement, float precip)
        {
            temperatureMin = tempMin;
            temperatureMax = tempMax;
            cropGrowth = crop;
            movementSpeed = movement;
            precipitationRate = precip;
        }

        private static readonly Dictionary<WeatherType, WeatherImpact> impacts = new Dictionary<WeatherType, WeatherImpact>
        {
            { WeatherType.Sunny,  new WeatherImpact(  0f,     0f,   1.2f, 1.0f,  0f  ) },
            { WeatherType.Rainy,  new WeatherImpact( -4.0f,  -1.5f, 0.9f, 0.85f, 1.5f) },
            { WeatherType.Stormy, new WeatherImpact( -6.0f,  -3.0f, 0.5f, 0.5f,  4.0f) },
            { WeatherType.Snowy,  new WeatherImpact(-10.0f,  -6.0f, 0.0f, 0.6f,  0.3f) },
            { WeatherType.Foggy,  new WeatherImpact( -2.0f,  -0.5f, 0.8f, 0.9f,  0.1f) }
        };

        public static WeatherImpact Get(WeatherType type)
        {
            return impacts.TryGetValue(type, out var impact) ? impact : new WeatherImpact(0, 0, 1f, 1f, 0f);
        }
    }
}
