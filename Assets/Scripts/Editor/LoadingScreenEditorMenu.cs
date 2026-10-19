#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using SpaceGame.UI;

namespace SpaceGame.Editor
{
    public static class LoadingScreenEditorMenu
    {
        private const string VideoAssetPath = "Assets/Videos/vid0001-0851.mp4";
        private const string GradientAssetPath = "Assets/Textures/BottomFadeGradient.png";
        private const string CloudMaterialPath = "Assets/Materials/AtmosphericCloudVignette.mat";
        private const string SparkHeadAssetPath = "Assets/Textures/BarSparkHead.png";
        private const string SolidWhiteAssetPath = "Assets/Textures/SolidWhite.png";

        [MenuItem("Space Game/Setup Loading Screen in Hierarchy", false, 10)]
        public static void SetupLoadingScreenInHierarchy()
        {
            // 1. Check if user currently has a Canvas selected, or find one in the active scene
            Canvas canvas = null;
            if (Selection.activeGameObject != null)
            {
                canvas = Selection.activeGameObject.GetComponent<Canvas>() ?? Selection.activeGameObject.GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Canvas c in canvases)
                {
                    string n = c.gameObject.name.ToLower();
                    if (n.Contains("loading") || n.Contains("intro") || n.Contains("screen"))
                    {
                        canvas = c;
                        break;
                    }
                }

                if (canvas == null && canvases.Length > 0)
                {
                    canvas = canvases[0];
                }
            }

            // If no canvas exists at all, create one in the Hierarchy
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("LoadingScreen_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Loading Screen Canvas");
            }

            Undo.RecordObject(canvas.gameObject, "Setup Loading Screen Hierarchy");

            // Configure Canvas basics
            canvas.name = "LoadingScreen_Canvas";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            CanvasGroup mainGroup = canvas.GetComponent<CanvasGroup>() ?? canvas.gameObject.AddComponent<CanvasGroup>();
            mainGroup.alpha = 1f;

            // Remove any old standalone StatusText directly under canvas
            Transform oldStandaloneStatus = canvas.transform.Find("StatusText");
            if (oldStandaloneStatus != null)
            {
                Undo.DestroyObjectImmediate(oldStandaloneStatus.gameObject);
            }

            // 2. Visible Child: Background (Solid Black)
            GameObject bgObj = GetOrCreateChild(canvas.transform, "Background");
            Image bgImg = bgObj.GetComponent<Image>() ?? bgObj.AddComponent<Image>();
            bgImg.color = Color.black;
            StretchRect(bgObj.GetComponent<RectTransform>());

            // 3. Visible Child: VideoDisplay (Immediately visible)
            GameObject vidObj = GetOrCreateChild(canvas.transform, "VideoDisplay");
            RawImage rawImage = vidObj.GetComponent<RawImage>() ?? vidObj.AddComponent<RawImage>();
            rawImage.color = Color.white;
            // NOTE: texture will be set at runtime by the controller (runtime RenderTexture)
            StretchRect(vidObj.GetComponent<RectTransform>());

            CanvasGroup videoGroup = vidObj.GetComponent<CanvasGroup>() ?? vidObj.AddComponent<CanvasGroup>();
            videoGroup.alpha = 1f;

            AspectRatioFitter fitter = vidObj.GetComponent<AspectRatioFitter>() ?? vidObj.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16.0f / 9.0f;

            // 4. Visible Child: CloudVignetteOverlay
            Transform oldVig = canvas.transform.Find("VignetteOverlay");
            if (oldVig != null)
            {
                oldVig.name = "CloudVignetteOverlay";
            }
            GameObject cloudObj = GetOrCreateChild(canvas.transform, "CloudVignetteOverlay");
            Image cloudImg = cloudObj.GetComponent<Image>() ?? cloudObj.AddComponent<Image>();
            Material cloudMat = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
            if (cloudMat != null)
            {
                cloudImg.material = cloudMat;
            }
            cloudImg.color = Color.white;
            StretchRect(cloudObj.GetComponent<RectTransform>());

            if (cloudObj.GetComponent<AnimatedCloudVignette>() == null)
            {
                cloudObj.AddComponent<AnimatedCloudVignette>();
            }

            // 5. Visible Child: BottomFadeGradient
            GameObject gradObj = GetOrCreateChild(canvas.transform, "BottomFadeGradient");
            Image gradImg = gradObj.GetComponent<Image>() ?? gradObj.AddComponent<Image>();
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GradientAssetPath);
            if (gradientSprite != null)
            {
                gradImg.sprite = gradientSprite;
                gradImg.type = Image.Type.Simple;
                gradImg.color = Color.white;
            }
            else
            {
                gradImg.color = new Color(0f, 0f, 0f, 0.75f);
            }
            RectTransform gradRect = gradObj.GetComponent<RectTransform>();
            gradRect.anchorMin = new Vector2(0f, 0f);
            gradRect.anchorMax = new Vector2(1f, 0f);
            gradRect.pivot = new Vector2(0.5f, 0f);
            gradRect.anchoredPosition = Vector2.zero;
            gradRect.sizeDelta = new Vector2(0f, 150f);

            // 6. Visible Child: SkipPromptText
            GameObject promptObj = GetOrCreateChild(canvas.transform, "SkipPromptText");
            TextMeshProUGUI promptTmp = promptObj.GetComponent<TextMeshProUGUI>() ?? promptObj.AddComponent<TextMeshProUGUI>();
            promptTmp.text = "[SPACE] : Next";
            promptTmp.fontSize = 18;
            promptTmp.characterSpacing = 8;
            promptTmp.alignment = TextAlignmentOptions.BottomLeft;
            promptTmp.color = new Color(0.82f, 0.80f, 0.74f, 0f);
            promptObj.SetActive(false);
            RectTransform promptRect = promptObj.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0f, 0f);
            promptRect.anchorMax = new Vector2(0f, 0f);
            promptRect.pivot = new Vector2(0f, 0f);
            promptRect.anchoredPosition = new Vector2(70f, 45f);
            promptRect.sizeDelta = new Vector2(400f, 35f);

            // 7. Visible Container: BottomRight_Loading
            GameObject loadingContainer = GetOrCreateChild(canvas.transform, "BottomRight_Loading");
            CanvasGroup loadingCg = loadingContainer.GetComponent<CanvasGroup>() ?? loadingContainer.AddComponent<CanvasGroup>();
            loadingCg.alpha = 1f;
            RectTransform containerRect = loadingContainer.GetComponent<RectTransform>() ?? loadingContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1f, 0f);
            containerRect.anchorMax = new Vector2(1f, 0f);
            containerRect.pivot = new Vector2(1f, 0f);
            containerRect.anchoredPosition = new Vector2(-70f, 45f);
            containerRect.sizeDelta = new Vector2(360f, 42f);

            // 7a. StatusText
            GameObject statusObj = GetOrCreateChild(loadingContainer.transform, "StatusText");
            TextMeshProUGUI statusTmp = statusObj.GetComponent<TextMeshProUGUI>() ?? statusObj.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "LOADING...";
            statusTmp.fontSize = 15;
            statusTmp.characterSpacing = 16;
            statusTmp.alignment = TextAlignmentOptions.BottomRight;
            statusTmp.color = new Color(0.78f, 0.76f, 0.72f, 0.85f);
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(1f, 0f);
            statusRect.anchoredPosition = new Vector2(0f, 18f);
            statusRect.sizeDelta = Vector2.zero;

            // 7b. Enhanced Functional Elden Ring Progress Bar
            GameObject barObj = GetOrCreateChild(loadingContainer.transform, "ProgressBar");
            EldenRingProgressBar erBar = barObj.GetComponent<EldenRingProgressBar>() ?? barObj.AddComponent<EldenRingProgressBar>();

            Slider legacySlider = barObj.GetComponent<Slider>();
            if (legacySlider != null)
            {
                Undo.DestroyObjectImmediate(legacySlider);
            }

            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(1f, 0f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(360f, 16f);

            // Track Line
            GameObject trackObj = GetOrCreateChild(barObj.transform, "TrackLine");
            Image trackImg = trackObj.GetComponent<Image>() ?? trackObj.AddComponent<Image>();
            trackImg.color = new Color(0.85f, 0.82f, 0.75f, 0.25f);
            RectTransform trackRect = trackObj.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(0f, 2.5f);

            // Ticks: Left, Center, Right
            Image tickL = SetupTick(barObj.transform, "Tick_Left", 0f, 12f);
            Image tickC = SetupTick(barObj.transform, "Tick_Center", 0.5f, 9f);
            Image tickR = SetupTick(barObj.transform, "Tick_Right", 1f, 12f);

            // Functional Fill Bar
            GameObject fillObj = GetOrCreateChild(barObj.transform, "FillBar");
            Image fillImg = fillObj.GetComponent<Image>() ?? fillObj.AddComponent<Image>();
            Sprite whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SolidWhiteAssetPath);
            if (whiteSprite != null)
            {
                fillImg.sprite = whiteSprite;
            }
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 3.5f);

            // Diamond Spark Head
            GameObject sparkObj = GetOrCreateChild(barObj.transform, "SparkHead");
            Image sparkImg = sparkObj.GetComponent<Image>() ?? sparkObj.AddComponent<Image>();
            Sprite sparkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SparkHeadAssetPath);
            if (sparkSprite != null)
            {
                sparkImg.sprite = sparkSprite;
            }
            sparkImg.color = Color.white;
            RectTransform sparkRect = sparkObj.GetComponent<RectTransform>();
            sparkRect.anchorMin = new Vector2(0f, 0.5f);
            sparkRect.anchorMax = new Vector2(0f, 0.5f);
            sparkRect.pivot = new Vector2(0.5f, 0.5f);
            sparkRect.anchoredPosition = Vector2.zero;
            sparkRect.sizeDelta = new Vector2(14f, 14f);

            // Clean up legacy FillArea
            Transform legacyFillArea = barObj.transform.Find("FillArea");
            if (legacyFillArea != null)
            {
                Undo.DestroyObjectImmediate(legacyFillArea.gameObject);
            }

            // Wire properties into EldenRingProgressBar using direct serialized field wiring
            WireSerializedField(erBar, "fillImage", fillImg);
            WireSerializedField(erBar, "sparkHead", sparkRect);
            WireSerializedField(erBar, "sparkImage", sparkImg);
            WireSerializedField(erBar, "trackLine", trackImg);
            WireSerializedField(erBar, "tickLeft", tickL);
            WireSerializedField(erBar, "tickCenter", tickC);
            WireSerializedField(erBar, "tickRight", tickR);
            WireSerializedField(erBar, "statusText", statusTmp);

            // Reorder hierarchy for proper visual layering
            bgObj.transform.SetSiblingIndex(0);
            vidObj.transform.SetSiblingIndex(1);
            cloudObj.transform.SetSiblingIndex(2);
            gradObj.transform.SetSiblingIndex(3);
            promptObj.transform.SetSiblingIndex(4);
            loadingContainer.transform.SetSiblingIndex(5);

            // 8. VideoPlayer component on Canvas
            VideoPlayer vp = canvas.GetComponent<VideoPlayer>() ?? canvas.gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false; // Controller handles playback
            vp.waitForFirstFrame = false;
            vp.isLooping = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.skipOnDrop = true;
            // Do NOT set targetTexture here - the controller creates it at runtime

            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoAssetPath);
            if (clip != null)
            {
                vp.clip = clip;
            }
            else
            {
                Debug.LogError("[Space Game] Could not find video clip at: " + VideoAssetPath);
            }

            // 9. LoadingScreenController on Canvas
            LoadingScreenController controller = canvas.GetComponent<LoadingScreenController>() ?? canvas.gameObject.AddComponent<LoadingScreenController>();

            // Wire all references using direct SerializedObject property assignment
            WireSerializedField(controller, "videoDisplay", rawImage);
            WireSerializedField(controller, "videoCanvasGroup", videoGroup);
            WireSerializedField(controller, "mainCanvasGroup", mainGroup);
            WireSerializedField(controller, "bottomFadeGradient", gradImg);
            WireSerializedField(controller, "loadingElementsGroup", loadingCg);
            WireSerializedField(controller, "eldenRingProgressBar", erBar);
            WireSerializedField(controller, "skipPromptText", promptTmp);
            WireSerializedField(controller, "videoPlayer", vp);
            WireSerializedField(controller, "loadingClip", clip);
            Texture poster = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Videos/Untitled design (3).png");
            if (poster != null)
            {
                WireSerializedField(controller, "posterTexture", poster);
            }

            // Select and highlight in Hierarchy
            Selection.activeGameObject = canvas.gameObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.SetDirty(canvas.gameObject);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(vp);

            Debug.Log("<color=#00e5ff><b>[Space Game]</b> Loading Screen setup complete! Video creates its own RenderTexture at runtime — no more void. Press Play to test.</color>");
        }

        [MenuItem("Space Game/Save Loading Screen as Prefab", false, 11)]
        public static void SaveAsPrefab()
        {
            SetupLoadingScreenInHierarchy();
            LoadingScreenController controller = Object.FindAnyObjectByType<LoadingScreenController>();
            if (controller != null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                string prefabPath = "Assets/Prefabs/LoadingScreen_Canvas.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(controller.gameObject, prefabPath, InteractionMode.UserAction);
                Debug.Log($"<color=#00e5ff><b>[Space Game]</b> Saved Loading Screen Canvas prefab at '{prefabPath}'!</color>");
            }
        }

        /// <summary>
        /// Reliably wires a serialized field by name on a component.
        /// Logs a warning if the property name is not found (field name mismatch).
        /// </summary>
        private static void WireSerializedField(Component target, string fieldName, Object value)
        {
            if (target == null || value == null) return;

            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[Space Game] Could not find serialized field '{fieldName}' on {target.GetType().Name}. Check field name.");
            }
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        private static Image SetupTick(Transform parent, string name, float xAnchor, float height)
        {
            GameObject tickObj = GetOrCreateChild(parent, name);
            Image tickImg = tickObj.GetComponent<Image>() ?? tickObj.AddComponent<Image>();
            tickImg.color = new Color(0.85f, 0.82f, 0.75f, 0.35f);
            RectTransform tickRect = tickObj.GetComponent<RectTransform>();
            tickRect.anchorMin = new Vector2(xAnchor, 0.5f);
            tickRect.anchorMax = new Vector2(xAnchor, 0.5f);
            tickRect.pivot = new Vector2(0.5f, 0.5f);
            tickRect.anchoredPosition = Vector2.zero;
            tickRect.sizeDelta = new Vector2(2f, height);
            return tickImg;
        }

        private static void StretchRect(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
