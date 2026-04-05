#if LOADSCREENMANAGER_DOTWEEN
using UnityEngine;
using DG.Tweening;

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// Optional bridge that drives <see cref="LoadScreenController"/> fade transitions with
    /// DOTween instead of built-in coroutines, providing eased fade-in and fade-out of the
    /// load-screen <see cref="CanvasGroup"/>.
    /// Enable define <c>LOADSCREENMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// This bridge sets <see cref="LoadScreenManager.ShowOverride"/> and
    /// <see cref="LoadScreenManager.HideOverride"/> so all fade transitions are routed through
    /// DOTween.
    /// </para>
    /// </summary>
    [AddComponentMenu("LoadScreenManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenLoadScreenBridge : MonoBehaviour
    {
        [Header("Fade")]
        [Tooltip("DOTween ease applied to the fade-in transition.")]
        [SerializeField] private Ease fadeInEase  = Ease.OutQuad;

        [Tooltip("DOTween ease applied to the fade-out transition.")]
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;

        // -------------------------------------------------------------------------

        private LoadScreenManager    _lsm;
        private LoadScreenController _ctrl;
        private CanvasGroup          _canvasGroup;
        private Tween                _fadeTween;

        private void Awake()
        {
            _lsm  = GetComponent<LoadScreenManager>()    ?? FindFirstObjectByType<LoadScreenManager>();
            _ctrl = GetComponent<LoadScreenController>() ?? FindFirstObjectByType<LoadScreenController>();

            if (_lsm == null)
            {
                Debug.LogWarning("[LoadScreenManager/DotweenBridge] LoadScreenManager not found.");
                return;
            }

            // Locate the CanvasGroup from the controller
            if (_ctrl != null)
                _canvasGroup = _ctrl.GetComponent<CanvasGroup>()
                    ?? _ctrl.GetComponentInChildren<CanvasGroup>();

            _lsm.ShowOverride = HandleShow;
            _lsm.HideOverride = HandleHide;
        }

        private void OnDestroy()
        {
            if (_lsm == null) return;
            if (_lsm.ShowOverride == (System.Action<LoadScreenDefinition, float>)HandleShow)
                _lsm.ShowOverride = null;
            if (_lsm.HideOverride == (System.Action<float>)HandleHide)
                _lsm.HideOverride = null;
        }

        // -------------------------------------------------------------------------

        private void HandleShow(LoadScreenDefinition def, float fadeDuration)
        {
            if (_ctrl != null)
            {
                // Let the controller handle all visual setup except the fade
                _ctrl.gameObject.SetActive(true);
                _ctrl.Show(def, 0f);     // instant fade — we own the easing
            }

            if (_canvasGroup == null) return;
            _fadeTween?.Kill();
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
            _fadeTween = _canvasGroup.DOFade(1f, fadeDuration <= 0f ? 0f : fadeDuration)
                                     .SetEase(fadeInEase)
                                     .SetUpdate(true);    // unscaledTime safe
        }

        private void HandleHide(float fadeDuration)
        {
            if (_canvasGroup == null)
            {
                _ctrl?.Hide(0f);
                return;
            }

            _fadeTween?.Kill();
            _fadeTween = _canvasGroup.DOFade(0f, fadeDuration <= 0f ? 0f : fadeDuration)
                                     .SetEase(fadeOutEase)
                                     .SetUpdate(true)
                                     .OnComplete(() =>
                                     {
                                         if (_ctrl != null)
                                             _ctrl.gameObject.SetActive(false);
                                         if (_canvasGroup != null)
                                         {
                                             _canvasGroup.interactable   = false;
                                             _canvasGroup.blocksRaycasts = false;
                                         }
                                     });
        }
    }
}
#endif
