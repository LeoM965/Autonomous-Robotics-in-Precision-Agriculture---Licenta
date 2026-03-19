using UnityEngine;

namespace Settings
{
    public interface ISettingsTab
    {
        string Title { get; }
        void Draw(Rect area, UITheme theme);
    }
}
