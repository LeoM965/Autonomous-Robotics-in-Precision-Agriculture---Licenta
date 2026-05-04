using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace SaveSystem
{
    /// <summary>
    /// Auto-bootstraps into simulation scenes.
    /// Loads saved state if requested by MainMenuUI.
    /// Uses a longer delay to ensure all scene objects are fully initialized.
    /// </summary>
    public class SimLoader : MonoBehaviour
    {
        public static bool ShouldLoadSave = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu") return;

            if (SimSaveManager.Instance == null)
            {
                var go = new GameObject("SaveManager");
                go.AddComponent<SimSaveManager>();
            }

            if (ShouldLoadSave && !string.IsNullOrEmpty(SimSaveManager.LastSaveName))
            {
                var loader = new GameObject("_SimLoader");
                loader.AddComponent<SimLoader>();
            }
        }

        private IEnumerator Start()
        {
            // Wait for all Awake + Start to finish across the scene
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;

            if (ShouldLoadSave && !string.IsNullOrEmpty(SimSaveManager.LastSaveName))
            {
                SimSaveManager.Instance?.Load(SimSaveManager.LastSaveName);
                Debug.Log($"[SimLoader] Încărcat salvarea \"{SimSaveManager.LastSaveName}\"");
            }

            // Reset flag AFTER load so RobotEnergy.Start() can check it
            ShouldLoadSave = false;
            Destroy(gameObject);
        }
    }
}
