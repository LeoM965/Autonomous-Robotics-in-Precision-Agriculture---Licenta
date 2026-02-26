using UnityEngine;
using Economics.Models;
namespace Economics.Services
{
    public static class EconomicDataLoader
    {
        private static EconomicDatabase _database;
        private static bool _isLoaded;
        private static void Load()
        {
            if (_isLoaded) return;
            var json = Resources.Load<TextAsset>("EconomicData");
            if (json == null)
            {
                Debug.LogError("[EconomicDataLoader] EconomicData.json lipseste din Resources!");
                return;
            }
            _database = JsonUtility.FromJson<EconomicDatabase>(json.text);
            _isLoaded = true;
        }
        public static RobotData GetRobot(string robotId)
        {
            Load();
            if (_database?.robots == null) return null;
            foreach (var robot in _database.robots)
                if (robot.id == robotId)
                    return robot;
            return _database.robots.Length > 0 ? _database.robots[0] : null;
        }
        public static LaborData GetLabor()
        {
            Load();
            return _database?.labor;
        }
    }
}
