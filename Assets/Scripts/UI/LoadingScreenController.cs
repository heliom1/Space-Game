using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using SpaceGame.Player;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpaceGame.UI
{
    /// <summary>
    /// Manages the loading screen sequence:
    /// - Displays the first frame poster texture IMMEDIATELY upon pressing Play (zero void/black screen!)
    /// - Seamlessly crossfades or switches to the active VideoPlayer RenderTexture as it begins playing
    /// - Fades in the loading text and progress bar AFTER the background is visibly active
    /// - Progress bar animates smoothly from 0% towards 100% over loadingDuration
    /// - Displays [SPACE] : Next when loading completes
    /// - Smoothly fades out into first-person gameplay
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingScreenController : MonoBehaviour
    {
        [Header("Hierarchy UI References")]
        [Tooltip("RawImage child object displaying the video")]
        [SerializeField] private RawImage videoDisplay;

        [Tooltip("CanvasGroup on VideoDisplay")]
        [SerializeField] private CanvasGroup videoCanvasGroup;

        [Tooltip("Main CanvasGroup on Canvas")]
        [SerializeField] private CanvasGroup mainCanvasGroup;

        [Tooltip("Bottom black fade gradient image")]
        [SerializeField] private Image bottomFadeGradient;

        [Tooltip("Parent container of the loading text & progress bar (BottomRight_Loading)")]
        [SerializeField] private CanvasGroup loadingElementsGroup;

        [Tooltip("The Elden Ring-style functional progress bar widget")]
        [SerializeField] private EldenRingProgressBar eldenRingProgressBar;

        [Tooltip("Skip prompt text at bottom-left ('[SPACE] : Next')")]
        [SerializeField] private TextMeshProUGUI skipPromptText;

        [Tooltip("VideoPlayer component")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("Video Settings")]
        [Tooltip("The loading video clip (Assets/Videos/vid0001-0851.mp4)")]
        [SerializeField] private VideoClip loadingClip;

        [Tooltip("First frame / poster texture to display immediately on play (Assets/Videos/Untitled design (3).png)")]
        [SerializeField] private Texture posterTexture;

        [Tooltip("Loop the video continuously while loading")]
        [SerializeField] private bool loopVideo = true;

        [Header("Sequence Timings (Seconds)")]
        [Tooltip("Delay in seconds after background displays before showing the progress bar")]
        [SerializeField] private float progressStartDelay = 0.5f;

        [Tooltip("Duration in seconds for the progress bar to fill from 0% to 100%")]
        [SerializeField] private float loadingDuration = 4.0f;

        [Tooltip("Duration in seconds of the smooth fade-out into gameplay")]
        [SerializeField] private float finalFadeOutDuration = 0.8f;

        private RenderTexture runtimeRT;
        private FPSController cachedPlayer;
        private bool isDismissing = false;
        private bool isSkipAllowed = false;
        private bool isVideoPlayingLive = false;
        private Coroutine sequenceCoroutine;

        private void Reset()
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
            videoPlayer = GetComponentInChildren<VideoPlayer>();
            videoDisplay = GetComponentInChildren<RawImage>();
            eldenRingProgressBar = GetComponentInChildren<EldenRingProgressBar>();
            if (videoDisplay != null)
            {
                videoCanvasGroup = videoDisplay.GetComponent<CanvasGroup>();
            }

            Transform brLoading = transform.Find("BottomRight_Loading");
            if (brLoading != null)
            {
                loadingElementsGroup = brLoading.GetComponent<CanvasGroup>();
            }
        }

        private void Awake()
        {
            ResetState();
            SetupVideoPlayback();
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void ResetState()
        {
            isDismissing = false;
            isSkipAllowed = false;
            isVideoPlayingLive = false;

            if (mainCanvasGroup == null) mainCanvasGroup = GetComponent<CanvasGroup>();
            if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
            if (videoDisplay == null) videoDisplay = GetComponentInChildren<RawImage>();
            if (eldenRingProgressBar == null) eldenRingProgressBar = GetComponentInChildren<EldenRingProgressBar>();

            if (loadingElementsGroup == null)
            {
                Transform brLoading = transform.Find("BottomRight_Loading");
                if (brLoading != null)
                {
                    loadingElementsGroup = brLoading.GetComponent<CanvasGroup>() ?? brLoading.gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Ensure proper full-screen stretching on videoDisplay
            if (videoDisplay != null)
            {
                RectTransform rt = videoDisplay.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Ensure main canvas is active & opaque
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.interactable = true;
                mainCanvasGroup.blocksRaycasts = true;
            }

            // Video Display visible
            if (videoCanvasGroup != null)
            {
                videoCanvasGroup.alpha = 1f;
            }

            // Load poster texture if not assigned
#if UNITY_EDITOR
            if (posterTexture == null)
            {
                posterTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>("Assets/Videos/Untitled design (3).png");
            }
#endif

            // SHOW POSTER TEXTURE IMMEDIATELY SO THERE IS NEVER A VOID OR BLACK SCREEN!
            if (videoDisplay != null)
            {
                if (posterTexture != null)
                {
                    videoDisplay.texture = posterTexture;
                }
                videoDisplay.color = Color.white;
            }

            // Loading bar container starts hidden so it NEVER appears before the visual background!
            if (loadingElementsGroup != null)
            {
                loadingElementsGroup.alpha = 0f;
            }

            // Hide skip prompt text immediately
            if (skipPromptText != null)
            {
                SetTextAlpha(skipPromptText, 0f);
                skipPromptText.gameObject.SetActive(false);
            }

            // Reset progress bar to 0% immediately
            if (eldenRingProgressBar != null)
            {
                eldenRingProgressBar.SetInstantProgress(0f);
            }
        }

        private void SetupVideoPlayback()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
            }

            if (videoDisplay == null)
            {
                videoDisplay = GetComponentInChildren<RawImage>();
            }

            // Assign clip
            if (loadingClip != null)
            {
                videoPlayer.clip = loadingClip;
            }
#if UNITY_EDITOR
            if (videoPlayer.clip == null)
            {
                videoPlayer.clip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/vid0001-0851.mp4");
            }
#endif

            if (videoPlayer.clip == null)
            {
                Debug.LogWarning("[LoadingScreen] No video clip assigned; poster will remain active.");
                return;
            }

            int w = (int)videoPlayer.clip.width;
            int h = (int)videoPlayer.clip.height;
            if (w <= 0) w = 1920;
            if (h <= 0) h = 1080;

            if (runtimeRT != null)
            {
                runtimeRT.Release();
                Destroy(runtimeRT);
            }

            runtimeRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            runtimeRT.name = "LoadingVideo_RT";
            runtimeRT.Create();

            // Configure VideoPlayer
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = runtimeRT;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = loopVideo;
            videoPlayer.waitForFirstFrame = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.sendFrameReadyEvents = true;

            // Clean event re-subscription
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.frameReady -= OnVideoFrameReady;
            videoPlayer.frameReady += OnVideoFrameReady;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;

            // Start preparing and playback
            videoPlayer.Prepare();
            videoPlayer.Play();
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            vp.Play();
        }

        private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
        {
            // Switch video display to the live RenderTexture the exact instant the first frame arrives
            if (!isVideoPlayingLive)
            {
                isVideoPlayingLive = true;
                if (videoDisplay != null && runtimeRT != null)
                {
                    videoDisplay.texture = runtimeRT;
                }
            }
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogWarning("[LoadingScreen] VideoPlayer notice: " + message);
        }

        private void Start()
        {
            // Suppress player controls immediately
            SuppressPlayerControls(true);

            if (videoPlayer != null && !videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }

            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(SequenceRoutine());
        }

        private void Update()
        {
            if (isDismissing) return;

            // Backup detection: if videoPlayer is playing and past frame 0, switch to RT
            if (!isVideoPlayingLive && videoPlayer != null && videoPlayer.isPlaying && videoPlayer.frame > 0)
            {
                isVideoPlayingLive = true;
                if (videoDisplay != null && runtimeRT != null)
                {
                    videoDisplay.texture = runtimeRT;
                }
            }

            // Check skip input ONLY once the progress bar has filled to 100%
            if (isSkipAllowed && CheckSkipInput())
            {
                DismissLoadingScreen();
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.frameReady -= OnVideoFrameReady;
                videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.loopPointReached -= OnVideoFinished;
            }

            if (runtimeRT != null)
            {
                runtimeRT.Release();
                Destroy(runtimeRT);
                runtimeRT = null;
            }

            SuppressPlayerControls(false);
        }

        private void OnVideoFinished(VideoPlayer source)
        {
            if (!loopVideo)
            {
                DismissLoadingScreen();
            }
        }

        private IEnumerator SequenceRoutine()
        {
            mainCanvasGroup.alpha = 1f;

            // 1. Give the background scene a short moment so the user sees the backdrop video first
            if (progressStartDelay > 0f)
            {
                yield return new WaitForSeconds(progressStartDelay);
            }

            // 2. Smoothly fade in the loading text & progress bar
            if (loadingElementsGroup != null)
            {
                float fadeIn = 0f;
                float fadeInDuration = 0.4f;
                while (fadeIn < fadeInDuration)
                {
                    fadeIn += Time.deltaTime;
                    loadingElementsGroup.alpha = Mathf.Clamp01(fadeIn / fadeInDuration);
                    yield return null;
                }
                loadingElementsGroup.alpha = 1f;
            }

            // Ensure progress bar starts at 0%
            if (eldenRingProgressBar != null)
            {
                eldenRingProgressBar.SetInstantProgress(0f);
            }

            // 3. Smoothly animate progress bar towards 100% over loadingDuration
            float timeline = 0f;
            while (timeline < loadingDuration)
            {
                timeline += Time.deltaTime;
                float progressFraction = Mathf.Clamp01(timeline / loadingDuration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progressFraction);

                if (eldenRingProgressBar != null)
                {
                    eldenRingProgressBar.SetProgress(easedProgress);
                }

                yield return null;
            }

            // --- LOADING COMPLETE (At 4 seconds) ---
            if (eldenRingProgressBar != null)
            {
                eldenRingProgressBar.SetProgress(1.0f);
            }

            // 4. Enable skip input and fade in [SPACE] : Next
            isSkipAllowed = true;
            if (skipPromptText != null)
            {
                StartCoroutine(FadeInText(skipPromptText, 0.4f));
            }
        }

        private IEnumerator FadeInText(TextMeshProUGUI tmp, float duration)
        {
            if (tmp == null) yield break;

            tmp.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = tmp.color;
            c.a = 0f;
            tmp.color = c;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / duration);
                tmp.color = c;
                yield return null;
            }
            c.a = 1f;
            tmp.color = c;
        }

        private void SetTextAlpha(TextMeshProUGUI tmp, float a)
        {
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = a;
                tmp.color = c;
            }
        }

        public void DismissLoadingScreen()
        {
            if (isDismissing) return;
            StartCoroutine(DismissRoutine());
        }

        private IEnumerator DismissRoutine()
        {
            isDismissing = true;

            float elapsed = 0f;
            float startAlpha = mainCanvasGroup != null ? mainCanvasGroup.alpha : 1f;

            while (elapsed < finalFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                if (mainCanvasGroup != null)
                {
                    mainCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / finalFadeOutDuration);
                }
                yield return null;
            }

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.interactable = false;
                mainCanvasGroup.blocksRaycasts = false;
            }

            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }

            SuppressPlayerControls(false);

            gameObject.SetActive(false);
        }

        private void SuppressPlayerControls(bool suppress)
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<FPSController>();
            }

            if (cachedPlayer != null)
            {
                cachedPlayer.enabled = !suppress;
                cachedPlayer.SetCursorLock(!suppress);
            }
        }

        private bool CheckSkipInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
                return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                return true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
                return true;
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
                return true;
#endif
            return false;
        }
    }
}
