#if LOADSCREENMANAGER_MGM
using MiniGameManager.Runtime;
using UnityEngine;

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>MiniGameLoadScreenBridge</b> connects LoadScreenManager to MiniGameManager without
    /// creating a hard compile-time dependency in either package.
    /// <para>
    /// When <c>LOADSCREENMANAGER_MGM</c> is defined:
    /// <list type="bullet">
    /// <item>Subscribes to <c>MiniGameManager.OnMiniGameStarted</c> and calls
    /// <see cref="LoadScreenManager.Show"/> so the load screen appears while a mini-game
    /// scene or prefab is being loaded.</item>
    /// <item>Subscribes to <c>MiniGameManager.OnMiniGameCompleted</c> and calls
    /// <see cref="LoadScreenManager.Hide"/> once the mini-game result is available.</item>
    /// <item>Subscribes to <c>MiniGameManager.OnMiniGameAborted</c> and calls
    /// <see cref="LoadScreenManager.Hide"/> when the mini-game is cancelled.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="LoadScreenManager"/>
    /// and add <c>LOADSCREENMANAGER_MGM</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Mini Game Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MiniGameLoadScreenBridge : MonoBehaviour
    {
        private LoadScreenManager              _lsm;
        private MiniGameManager.Runtime.MiniGameManager _mgm;

        [Tooltip("Id of the LoadScreenDefinition shown when a mini-game starts loading. " +
                 "Leave empty to use LoadScreenManager's defaultScreenId.")]
        [SerializeField] private string loadScreenId = "";

        [Tooltip("Show the load screen when a mini-game starts. " +
                 "Disable if a different bridge already triggers the show.")]
        [SerializeField] private bool showOnStart = true;

        [Tooltip("Hide the load screen when a mini-game completes.")]
        [SerializeField] private bool hideOnComplete = true;

        [Tooltip("Hide the load screen when a mini-game is aborted.")]
        [SerializeField] private bool hideOnAbort = true;

        private void Awake()
        {
            _lsm = GetComponent<LoadScreenManager>() ?? FindFirstObjectByType<LoadScreenManager>();
            _mgm = GetComponent<MiniGameManager.Runtime.MiniGameManager>()
                ?? FindFirstObjectByType<MiniGameManager.Runtime.MiniGameManager>();

            if (_lsm == null)
                Debug.LogWarning("[LoadScreenManager/MiniGameBridge] LoadScreenManager not found.");

            if (_mgm == null)
                Debug.LogWarning("[LoadScreenManager/MiniGameBridge] MiniGameManager not found — mini-game load-screen automation disabled.");
        }

        private void OnEnable()
        {
            if (_mgm == null) return;
            if (showOnStart)    _mgm.OnMiniGameStarted    += OnMiniGameStarted;
            if (hideOnComplete) _mgm.OnMiniGameCompleted  += OnMiniGameCompleted;
            if (hideOnAbort)    _mgm.OnMiniGameAborted    += OnMiniGameAborted;
        }

        private void OnDisable()
        {
            if (_mgm == null) return;
            _mgm.OnMiniGameStarted   -= OnMiniGameStarted;
            _mgm.OnMiniGameCompleted -= OnMiniGameCompleted;
            _mgm.OnMiniGameAborted   -= OnMiniGameAborted;
        }

        // -------------------------------------------------------------------------
        // MiniGame → LoadScreen
        // -------------------------------------------------------------------------

        private void OnMiniGameStarted(string miniGameId)
        {
            _lsm?.Show(loadScreenId);
        }

        private void OnMiniGameCompleted(MiniGameResult result)
        {
            _lsm?.Hide();
        }

        private void OnMiniGameAborted(string miniGameId)
        {
            _lsm?.Hide();
        }
    }
}
#else
namespace LoadScreenManager.Runtime
{
    /// <summary>No-op stub — define <c>LOADSCREENMANAGER_MGM</c> to activate this bridge.</summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Mini Game Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MiniGameLoadScreenBridge : UnityEngine.MonoBehaviour { }
}
#endif
