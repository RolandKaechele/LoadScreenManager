#if LOADSCREENMANAGER_TITLE
using TitleScreenManager.Runtime;
using UnityEngine;

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>TitleScreenLoadScreenBridge</b> connects LoadScreenManager to TitleScreenManager without
    /// creating a hard compile-time dependency in either package.
    /// <para>
    /// When <c>LOADSCREENMANAGER_TITLE</c> is defined:
    /// <list type="bullet">
    /// <item>Subscribes to <c>TitleScreenManager.OnNewGame</c> and calls
    /// <see cref="LoadScreenManager.Show"/> so the load screen appears when a new game is started.</item>
    /// <item>Subscribes to <c>TitleScreenManager.OnContinue</c> and calls
    /// <see cref="LoadScreenManager.Show"/> when the last save slot is resumed.</item>
    /// <item>Subscribes to <c>TitleScreenManager.OnLoadSlot</c> and calls
    /// <see cref="LoadScreenManager.Show"/> when a specific save slot is selected.</item>
    /// </list>
    /// </para>
    /// <para>
    /// The load screen is hidden automatically by whichever bridge owns the post-load event
    /// (<c>LOADSCREENMANAGER_GM</c>, <c>LOADSCREENMANAGER_MLF</c>, or <c>LOADSCREENMANAGER_SM</c>).
    /// If none of those bridges are present, enable <see cref="autoHideDelay"/> as a fallback.
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="LoadScreenManager"/>
    /// and add <c>LOADSCREENMANAGER_TITLE</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Title Screen Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class TitleScreenLoadScreenBridge : MonoBehaviour
    {
        private LoadScreenManager                         _lsm;
        private TitleScreenManager.Runtime.TitleScreenManager _tsm;

        [Tooltip("Id of the LoadScreenDefinition shown during scene transitions triggered by the title screen. " +
                 "Leave empty to use LoadScreenManager's defaultScreenId.")]
        [SerializeField] private string loadScreenId = "";

        [Tooltip("Show the load screen on OnNewGame. Disable if another system (e.g. GameManager) already handles it.")]
        [SerializeField] private bool showOnNewGame = true;

        [Tooltip("Show the load screen on OnContinue. Disable if another system already handles it.")]
        [SerializeField] private bool showOnContinue = true;

        [Tooltip("Show the load screen on OnLoadSlot. Disable if another system already handles it.")]
        [SerializeField] private bool showOnLoadSlot = true;

        [Header("Fallback hide (use only when no other bridge handles OnAfterChapterLoad)")]
        [Tooltip("When > 0, the load screen is automatically hidden after this many seconds " +
                 "if no other bridge has hidden it. Useful when LoadScreenManager is used stand-alone.")]
        [SerializeField] private float autoHideDelay = 0f;

        private void Awake()
        {
            _lsm = GetComponent<LoadScreenManager>() ?? FindFirstObjectByType<LoadScreenManager>();
            _tsm = GetComponent<TitleScreenManager.Runtime.TitleScreenManager>()
                ?? FindFirstObjectByType<TitleScreenManager.Runtime.TitleScreenManager>();

            if (_lsm == null)
                Debug.LogWarning("[LoadScreenManager/TitleScreenBridge] LoadScreenManager not found.");

            if (_tsm == null)
                Debug.LogWarning("[LoadScreenManager/TitleScreenBridge] TitleScreenManager not found — title-screen load-screen automation disabled.");
        }

        private void OnEnable()
        {
            if (_tsm == null) return;
            if (showOnNewGame)  _tsm.OnNewGame  += OnNewGame;
            if (showOnContinue) _tsm.OnContinue += OnContinue;
            if (showOnLoadSlot) _tsm.OnLoadSlot  += OnLoadSlot;
        }

        private void OnDisable()
        {
            if (_tsm == null) return;
            _tsm.OnNewGame  -= OnNewGame;
            _tsm.OnContinue -= OnContinue;
            _tsm.OnLoadSlot  -= OnLoadSlot;
        }

        // -------------------------------------------------------------------------
        // TitleScreen → LoadScreen
        // -------------------------------------------------------------------------

        private void OnNewGame()
        {
            ShowScreen();
        }

        private void OnContinue(int slotIndex)
        {
            ShowScreen();
        }

        private void OnLoadSlot(int slotIndex)
        {
            ShowScreen();
        }

        private void ShowScreen()
        {
            _lsm?.Show(loadScreenId);

            if (autoHideDelay > 0f)
                StartCoroutine(AutoHide(autoHideDelay));
        }

        private System.Collections.IEnumerator AutoHide(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (_lsm != null && _lsm.IsShowing)
                _lsm.Hide();
        }
    }
}
#else
namespace LoadScreenManager.Runtime
{
    /// <summary>No-op stub — define <c>LOADSCREENMANAGER_TITLE</c> to activate this bridge.</summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Title Screen Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class TitleScreenLoadScreenBridge : UnityEngine.MonoBehaviour { }
}
#endif
