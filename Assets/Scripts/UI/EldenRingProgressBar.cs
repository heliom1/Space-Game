using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpaceGame.UI
{
    /// <summary>
    /// Functional Elden Ring-style Loading Progress Bar:
    /// - Smooth horizontal fill using reliable Image.FillMethod
    /// - Traveling diamond spark head at the leading edge
    /// - Responsive division ticks that illuminate as the bar passes them (0%, 50%, 100%)
    /// - Animated dots on the LOADING... title
    /// - Smooth interpolation and completion pulse
    /// </summary>
    public class EldenRingProgressBar : MonoBehaviour
    {
        [Header("Hierarchy UI References")]
        [Tooltip("The horizontal fill Image (Image Type must be Filled -> Horizontal)")]
        [SerializeField] private Image fillImage;

        [Tooltip("The diamond spark head RectTransform that glides at the leading edge")]
        [SerializeField] private RectTransform sparkHead;

        [Tooltip("Spark head Image for glow effects")]
        [SerializeField] private Image sparkImage;

        [Tooltip("Background rail track image")]
        [SerializeField] private Image trackLine;

        [Tooltip("Division tick at 0%")]
        [SerializeField] private Image tickLeft;

        [Tooltip("Division tick at 50%")]
        [SerializeField] private Image tickCenter;

        [Tooltip("Division tick at 100%")]
        [SerializeField] private Image tickRight;

        [Tooltip("Status text ('LOADING...') directly above the bar")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Optional percentage text ('45%')")]
        [SerializeField] private TextMeshProUGUI percentText;

        [Header("Animation Settings")]
        [Tooltip("Speed of smooth fill bar catch-up")]
        [SerializeField] private float fillSpeed = 1.8f;

        [Tooltip("Animate cycling 3 dots (. -> .. -> ...) on status text")]
        [SerializeField] private bool animateDots = true;
        [SerializeField] private float dotSpeed = 0.32f;

        [Header("Colors")]
        [SerializeField] private Color fillColor = new Color(0.96f, 0.94f, 0.88f, 1f); // Luminous ivory
        [SerializeField] private Color tickInactiveColor = new Color(0.85f, 0.82f, 0.75f, 0.3f);
        [SerializeField] private Color tickActiveColor = new Color(1f, 0.98f, 0.90f, 1f);

        private float targetProgress = 0f;
        private float currentProgress = 0f;
        private RectTransform barRect;
        private string baseStatus = "LOADING";
        private bool isComplete = false;

        public float Progress => currentProgress;
        public bool IsComplete => currentProgress >= 0.999f;

        private void Awake()
        {
            barRect = GetComponent<RectTransform>();

            if (statusText != null)
            {
                baseStatus = statusText.text.TrimEnd('.', ' ');
                if (string.IsNullOrEmpty(baseStatus)) baseStatus = "LOADING";
            }

            ResetVisuals();
        }

        private void OnEnable()
        {
            ResetVisuals();
        }

        public void ResetVisuals()
        {
            currentProgress = 0f;
            targetProgress = 0f;
            isComplete = false;

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.fillAmount = 0f;
                fillImage.color = fillColor;
            }

            if (sparkHead != null)
            {
                sparkHead.gameObject.SetActive(false);
            }

            if (tickLeft != null) tickLeft.color = tickInactiveColor;
            if (tickCenter != null) tickCenter.color = tickInactiveColor;
            if (tickRight != null) tickRight.color = tickInactiveColor;
        }

        private void Update()
        {
            // 1. Smoothly interpolate current progress towards target
            if (currentProgress < targetProgress)
            {
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * fillSpeed);
            }
            else if (targetProgress >= 1f && currentProgress >= 0.99f)
            {
                currentProgress = 1f;
            }

            // 2. Update Fill Image
            if (fillImage != null)
            {
                fillImage.fillAmount = currentProgress;
            }

            // 3. Update Traveling Spark Head at leading edge
            if (sparkHead != null && barRect != null)
            {
                float totalWidth = barRect.rect.width;
                if (totalWidth <= 0) totalWidth = 360f;

                float xPos = totalWidth * currentProgress;
                sparkHead.anchoredPosition = new Vector2(xPos, 0f);

                // Show spark while moving, hide when empty or completely done
                bool showSpark = currentProgress > 0.01f && currentProgress < 0.995f;
                if (sparkHead.gameObject.activeSelf != showSpark)
                {
                    sparkHead.gameObject.SetActive(showSpark);
                }

                // Gentle pulsing shimmer on spark
                if (sparkImage != null && showSpark)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 12f);
                    sparkImage.color = new Color(1f, 1f, 1f, pulse);
                }
            }

            // 4. Illuminate Ticks as the bar crosses them
            if (tickLeft != null)
            {
                tickLeft.color = currentProgress > 0.02f ? tickActiveColor : tickInactiveColor;
            }
            if (tickCenter != null)
            {
                tickCenter.color = currentProgress >= 0.5f ? tickActiveColor : tickInactiveColor;
            }
            if (tickRight != null)
            {
                tickRight.color = currentProgress >= 0.99f ? tickActiveColor : tickInactiveColor;
            }

            // 5. Update Loading Text with animated 3 dots
            if (statusText != null && statusText.gameObject.activeInHierarchy && !isComplete)
            {
                if (animateDots)
                {
                    int dotCount = (int)(Time.time * (1f / dotSpeed)) % 4;
                    statusText.text = baseStatus + new string('.', dotCount);
                }
            }

            // 6. Update percentage text if assigned
            if (percentText != null)
            {
                percentText.text = Mathf.RoundToInt(currentProgress * 100f) + "%";
            }

            // 7. Completion state
            if (!isComplete && currentProgress >= 0.999f)
            {
                isComplete = true;
                if (sparkHead != null) sparkHead.gameObject.SetActive(false);
            }
        }

        public void SetProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);
        }

        public void SetInstantProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);
            currentProgress = targetProgress;
            if (fillImage != null) fillImage.fillAmount = currentProgress;
            if (sparkHead != null)
            {
                if (barRect == null) barRect = GetComponent<RectTransform>();
                float totalWidth = barRect != null ? barRect.rect.width : 360f;
                sparkHead.anchoredPosition = new Vector2(totalWidth * currentProgress, 0f);
            }
        }

        public void SetStatusText(string text)
        {
            baseStatus = text.TrimEnd('.', ' ');
            if (statusText != null) statusText.text = text;
        }
    }
}
