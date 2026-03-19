using UnityEngine;

namespace Settings.Tabs
{
    public class RobotStatsTab : ISettingsTab
    {
        public string Title => "Tehnic";
        private Vector2 scroll;

        public void Draw(Rect area, UITheme theme)
        {
            float x = 0, y = 0;
            GUI.Label(new Rect(x, y, 350, 20), "CONFIGURARE PERFORMANȚĂ ROBOȚI", theme.Title);
            y += 30;

            var robotDB = RobotDataLoader.Load();
            if (robotDB?.robots != null)
            {
                Rect scrollArea = new Rect(x, y, area.width, area.height - y);
                Rect content = new Rect(0, 0, area.width - 20, robotDB.robots.Count * 160);
                scroll = GUI.BeginScrollView(scrollArea, scroll, content);

                float sy = 0;
                foreach (var r in robotDB.robots)
                {
                    GUI.Label(new Rect(0, sy, 340, 20), $"<b>{r.model} ({r.namePrefix})</b>", theme.Value);
                    sy += 25;

                    Utils.SettingsUIHelper.DrawLabeledSlider(ref sy, "Baterie (Wh):", ref r.batteryCapacity, 500f, 100000f, "F0", theme);
                    Utils.SettingsUIHelper.DrawLabeledSlider(ref sy, "Viteză (km/h):", ref r.maxSpeed, 1f, 30f, "F1", theme);
                    float movingWhm = r.consumptionMeter * 1000f;
                    Utils.SettingsUIHelper.DrawLabeledSlider(ref sy, "Consum Mișcare (Wh/m):", ref movingWhm, 0.1f, 50f, "F1", theme);
                    r.consumptionMeter = movingWhm / 1000f;

                    float workWhs = r.consumptionWorkSec * 1000f;
                    Utils.SettingsUIHelper.DrawLabeledSlider(ref sy, "Consum Lucru (Wh/s):", ref workWhs, 0.001f, 10f, "F3", theme);
                    r.consumptionWorkSec = workWhs / 1000f;

                    float rechargeWhs = r.rechargeRate * 1000f;
                    Utils.SettingsUIHelper.DrawLabeledSlider(ref sy, "Rată Reîncărcare (Wh/s):", ref rechargeWhs, 0.1f, 100f, "F1", theme);
                    r.rechargeRate = rechargeWhs / 1000f;
                    
                    sy += 15;
                    GUI.Box(new Rect(0, sy - 5, 340, 1), ""); 
                    sy += 5;
                }
                GUI.EndScrollView();
            }
        }
    }
}
