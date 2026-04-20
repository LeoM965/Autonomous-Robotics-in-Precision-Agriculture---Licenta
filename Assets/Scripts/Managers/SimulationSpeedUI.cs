using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationSpeedUI : MonoBehaviour
{
    private SimulationSpeedController controller;

    [Header("Speed Buttons")]
    public Button[] speedButtons;
    public Image[] speedImages;

    [Header("Boost Components")]
    public Button boostBtn;
    public Image boostImage;
    public TextMeshProUGUI boostText;

    [Header("Status Context")]
    public TextMeshProUGUI statusLabel;

    [Header("Weather and Skip")]
    public Button weatherBtn;
    public TextMeshProUGUI weatherText;
    public Button skipBtn;
    public TextMeshProUGUI skipText;

    [Header("Month Navigation")]
    public Button[] monthButtons;

    [Header("Theme Colors")]
    public Color inactiveBtnColor = new Color(0.12f, 0.20f, 0.35f, 1f);
    public Color activeSpeedColor = new Color(0.1f, 0.9f, 0.4f);
    public Color activeBoostColor = new Color(0.05f, 0.6f, 1f, 1f);
    public Color mainTextColor = Color.white;
    public Color warningColor = new Color(0.95f, 0.7f, 0.1f);

    private void Start()
    {
        controller = SimulationSpeedController.Instance;

        // Auto-assign listeners
        if (speedButtons != null)
        {
            for (int i = 0; i < speedButtons.Length; i++)
            {
                int index = i;
                if (speedButtons[i] != null)
                    speedButtons[i].onClick.AddListener(() => { if (controller != null) controller.SetSpeed(index); });
            }
        }

        if (boostBtn != null) 
            boostBtn.onClick.AddListener(() => { if (controller != null) controller.ToggleBoost(); });

        if (weatherBtn != null)
            weatherBtn.onClick.AddListener(() => {
                if (Weather.Components.WeatherSystem.Instance != null)
                    Weather.Components.WeatherSystem.Instance.CycleForcedWeather();
            });

        if (skipBtn != null) 
            skipBtn.onClick.AddListener(() => { if (controller != null) controller.SkipDay(); });

        if (monthButtons != null)
        {
            for (int i = 0; i < monthButtons.Length; i++)
            {
                int mIndex = i;
                if (monthButtons[i] != null)
                    monthButtons[i].onClick.AddListener(() => {
                        if (TimeManager.Instance != null)
                            TimeManager.Instance.SkipToDate(1, mIndex + 1);
                    });
            }
        }
    }

    private void Update()
    {
        if (controller == null) return;
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        // 1. Update Speeds
        if (speedImages != null && speedImages.Length == controller.Speeds.Length)
        {
            for (int i = 0; i < speedImages.Length; i++)
            {
                bool active = (controller.CurrentIndex == i);
                UpdateButtonTheme(speedImages[i], active ? activeSpeedColor : inactiveBtnColor, active);
            }
        }

        // 2. Update Boost
        if (boostImage != null)
        {
            bool bstActive = controller.IsBoostActive;
            UpdateButtonTheme(boostImage, bstActive ? activeBoostColor : inactiveBtnColor, bstActive);
            
            if (boostText != null) 
                boostText.text = "x" + controller.BoostMultiplier;
        }

        // 3. Update Status Label
        if (statusLabel != null)
        {
            statusLabel.text = controller.IsSkipping ? "SKIPPING..." : (Time.timeScale > 0 ? "Speed: " + Time.timeScale + "x" : "PAUSED");
            statusLabel.color = controller.IsSkipping ? warningColor : mainTextColor;
        }

        // 4. Update Weather Text
        if (weatherText != null && Weather.Components.WeatherSystem.Instance != null)
            weatherText.text = Weather.Components.WeatherSystem.Instance.ForcedWeather.HasValue 
                ? Weather.Components.WeatherSystem.Instance.ForcedWeather.Value.ToString() 
                : "Auto W.";

        // 5. Update Skip State and Interactable Lock
        bool skipBlocked = controller.IsSkipping;
        
        if (skipText != null) 
            skipText.text = skipBlocked ? "..." : "Next Day";
        
        if (weatherBtn != null) weatherBtn.interactable = !skipBlocked;
        if (skipBtn != null) skipBtn.interactable = !skipBlocked;

        if (monthButtons != null)
        {
            for (int i = 0; i < monthButtons.Length; i++)
            {
                if (monthButtons[i] != null) 
                    monthButtons[i].interactable = !skipBlocked;
            }
        }
    }

    private void UpdateButtonTheme(Image img, Color c, bool isActive)
    {
        if (img == null) return;
        
        img.color = c;
        Button b = img.GetComponent<Button>();
        if (b != null)
        {
            var cb = b.colors;
            cb.normalColor = c;
            cb.highlightedColor = c * 1.2f;
            cb.pressedColor = c * 0.8f;
            b.colors = cb;
            
            // Set Text
            var txt = b.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = isActive ? Color.black : Color.white;
        }
    }
}
