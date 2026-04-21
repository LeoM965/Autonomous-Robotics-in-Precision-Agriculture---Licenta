using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace UI.Canvas
{
    /// <summary>
    /// SOLID: Base class for panels that interact with 3D objects via Raycasting.
    /// DRY: Centralizes mouse input and UI blocking checks.
    /// </summary>
    public abstract class InteractivePanel : MonoBehaviour
    {
        protected Camera mainCam;
        protected GameObject visuals;
        protected Transform selectedTarget;
        private float refreshTimer;

        protected virtual void Start()
        {
            mainCam = Camera.main;
            visuals = transform.Find("Visuals")?.gameObject;
            if (visuals) visuals.SetActive(false);
            
            OnInitialize();
        }

        protected virtual void Update()
        {
            HandleMouseInput();
            HandleKeyboardInput();
            
            if (visuals != null && visuals.activeSelf && selectedTarget != null)
            {
                if ((refreshTimer -= Time.unscaledDeltaTime) <= 0f)
                {
                    OnRefresh();
                    refreshTimer = 0.25f;
                }
            }
        }

        private void HandleMouseInput()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // KISS: Ignore if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Transform target = FindTarget(hit.transform);
                if (target != null)
                {
                    selectedTarget = target;
                    SetVisible(true);
                }
                else
                {
                    SetVisible(false);
                }
            }
            else
            {
                SetVisible(false);
            }
        }

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetVisible(false);
            }
        }

        public void SetVisible(bool visible)
        {
            if (visuals != null)
            {
                visuals.SetActive(visible);
                if (visible) OnRefresh();
            }
        }

        protected TextMeshProUGUI GetText(string path) => CanvasHelper.GetText(transform, path);

        // Methods to be implemented by derived classes
        protected abstract void OnInitialize();
        protected abstract void OnRefresh();
        protected abstract Transform FindTarget(Transform hitTransform);
    }
}
