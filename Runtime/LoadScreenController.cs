using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>LoadScreenController</b> manages the visual elements of the load screen overlay.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Fade the root <see cref="CanvasGroup"/> in and out using a coroutine (replacable by
    /// <see cref="DotweenLoadScreenBridge"/> when <c>LOADSCREENMANAGER_DOTWEEN</c> is defined).</item>
    /// <item>Drive the progress bar fill.</item>
    /// <item>Rotate tips from the active <see cref="LoadScreenDefinition.tipPool"/>.</item>
    /// <item>Spin the optional spinner transform.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to the same GameObject as <see cref="LoadScreenManager"/> (or a child).
    /// Wire the UI references in the Inspector; any left unassigned are silently ignored.
    /// </para>
    /// </summary>
    [AddComponentMenu("LoadScreenManager/Load Screen Controller")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class LoadScreenController : Sirenix.OdinInspector.SerializedMonoBehaviour
#else
    public class LoadScreenController : MonoBehaviour
#endif
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Root")]
        [Tooltip("CanvasGroup on the root load-screen panel. Used for fade-in / fade-out and interaction block.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Progress")]
        [Tooltip("Slider or Image used as a progress bar. When assigned as Slider, the .value (0-1) is set." +
                 " When assigned as Image with fillMethod != None, the .fillAmount is set.")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image  progressFillImage;

        [Header("Spinner")]
        [Tooltip("Transform of the spinner graphic. Rotated continuously while the screen is visible.")]
        [SerializeField] private Transform spinnerTransform;
        [Tooltip("Degrees per second the spinner rotates (negative = counter-clockwise).")]
        [SerializeField] private float spinnerSpeed = -200f;

        [Header("Tips")]
        [Tooltip("TextMeshPro — TextMeshProUGUI or legacy Text component that displays loading tips.")]
        [SerializeField] private TMPro.TextMeshProUGUI tipText;

        [Header("Background")]
        [Tooltip("Image that covers the screen. Its sprite is replaced by LoadScreenDefinition.backgroundResource.")]
        [SerializeField] private Image backgroundImage;

        // -------------------------------------------------------------------------
        // Internal state
        // -------------------------------------------------------------------------

        private Coroutine _fadeCoroutine;
        private Coroutine _tipCoroutine;
        private bool      _spinning;

        /// <summary>True while the load screen canvas is visible (alpha > 0).</summary>
#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
#endif
        public bool IsVisible { get; private set; }

        // -------------------------------------------------------------------------
        // Public API — called by LoadScreenManager (or DOTween bridge override)
        // -------------------------------------------------------------------------

        /// <summary>Apply the definition's visual settings and fade in the panel.</summary>
        public void Show(LoadScreenDefinition def, float fadeDuration)
        {
            gameObject.SetActive(true);

            // Background
            if (!string.IsNullOrEmpty(def?.backgroundResource))
            {
                var spr = Resources.Load<Sprite>(def.backgroundResource);
                if (spr != null && backgroundImage != null) backgroundImage.sprite = spr;
            }

            // Spinner graphic
            if (!string.IsNullOrEmpty(def?.spinnerResource) && spinnerTransform != null)
            {
                var img = spinnerTransform.GetComponent<Image>();
                if (img != null)
                {
                    var spr = Resources.Load<Sprite>(def.spinnerResource);
                    if (spr != null) img.sprite = spr;
                }
            }

            // Progress fill graphic
            if (!string.IsNullOrEmpty(def?.progressFillResource) && progressFillImage != null)
            {
                var spr = Resources.Load<Sprite>(def.progressFillResource);
                if (spr != null) progressFillImage.sprite = spr;
            }

            // Sub-element visibility
            if (progressSlider   != null) progressSlider.gameObject.SetActive(def?.showProgress ?? true);
            if (progressFillImage!= null) progressFillImage.gameObject.SetActive(def?.showProgress ?? true);
            if (spinnerTransform != null) spinnerTransform.gameObject.SetActive(def?.showSpinner ?? true);

            // Progress reset
            UpdateProgress(0f);

            // Tips
            bool useTips = def?.showTips ?? true;
            if (tipText != null) tipText.gameObject.SetActive(useTips);
            StopTipRotation();
            if (useTips && def?.tipPool is { Length: > 0 })
            {
                SetTip(def.tipPool[Random.Range(0, def.tipPool.Length)]);
                if (def.tipRotationInterval > 0f && def.tipPool.Length > 1)
                    _tipCoroutine = StartCoroutine(RotateTips(def.tipPool, def.tipRotationInterval));
            }

            // Spinner
            _spinning = def?.showSpinner ?? true;

            // Fade
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeTo(1f, fadeDuration, onComplete: () => IsVisible = true));
        }

        /// <summary>Fade out and deactivate the panel.</summary>
        public void Hide(float fadeDuration)
        {
            StopTipRotation();
            _spinning = false;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeTo(0f, fadeDuration, onComplete: () =>
            {
                IsVisible = false;
                gameObject.SetActive(false);
            }));
        }

        /// <summary>Set the progress bar fill to <paramref name="value"/> (0–1).</summary>
        public void UpdateProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (progressSlider    != null) progressSlider.value      = value;
            if (progressFillImage != null) progressFillImage.fillAmount = value;
        }

        /// <summary>Override the displayed tip text directly.</summary>
        public void SetTip(string text)
        {
            if (tipText != null) tipText.text = text;
        }

        /// <summary>Override the background sprite directly.</summary>
        public void SetBackground(Sprite sprite)
        {
            if (backgroundImage != null) backgroundImage.sprite = sprite;
        }

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
            IsVisible = false;
        }

        private void Update()
        {
            if (_spinning && spinnerTransform != null)
                spinnerTransform.Rotate(0f, 0f, spinnerSpeed * Time.unscaledDeltaTime);
        }

        // -------------------------------------------------------------------------
        // Coroutines
        // -------------------------------------------------------------------------

        private IEnumerator FadeTo(float targetAlpha, float duration, System.Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            if (duration <= 0f)
            {
                canvasGroup.alpha          = targetAlpha;
                canvasGroup.interactable   = targetAlpha > 0f;
                canvasGroup.blocksRaycasts = targetAlpha > 0f;
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            canvasGroup.interactable   = targetAlpha > 0f;
            canvasGroup.blocksRaycasts = targetAlpha > 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private IEnumerator RotateTips(string[] pool, float interval)
        {
            int index = 0;
            while (true)
            {
                yield return new WaitForSecondsRealtime(interval);
                index = (index + 1) % pool.Length;
                SetTip(pool[index]);
            }
        }

        private void StopTipRotation()
        {
            if (_tipCoroutine != null) { StopCoroutine(_tipCoroutine); _tipCoroutine = null; }
        }
    }
}
