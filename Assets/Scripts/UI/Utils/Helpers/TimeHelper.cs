using UnityEngine;
public static class TimeHelper
{
    public static string FormatSeconds(float seconds)
    {
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
    public static string FormatHours(float seconds)
    {
        int hours = (int)(seconds / 3600);
        int minutes = (int)((seconds % 3600) / 60);
        int secs = (int)(seconds % 60);
        return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, secs);
    }
    
}
