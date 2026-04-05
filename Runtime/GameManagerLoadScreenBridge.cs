#if LOADSCREENMANAGER_GM
using GameManager.Runtime;
using UnityEngine;

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>GameManagerLoadScreenBridge</b> connects LoadScreenManager to GameManager without creating
    /// a hard compile-time dependency in either package.
    /// <para>
    /// When <c>LOADSCREENMANAGER_GM</c> is defined:
    /// <list type="bullet">
    /// <item>Subscribes to <c>GameManager.OnBeforeChapterLoad</c> and calls
    /// <see cref="LoadScreenManager.Show"/> so the load screen appears before each chapter loads.</item>
    /// <item>Subscribes to <c>GameManager.OnAfterChapterLoad</c> and calls
    /// <see cref="LoadScreenManager.Hide"/> once the chapter is fully ready.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="LoadScreenManager"/>
    /// and add <c>LOADSCREENMANAGER_GM</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Game Manager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class GameManagerLoadScreenBridge : MonoBehaviour
    {
        private LoadScreenManager         _lsm;
        private GameManager.Runtime.GameManager _gm;

        [Tooltip("Id of the LoadScreenDefinition shown during chapter transitions. " +
                 "Leave empty to use LoadScreenManager's defaultScreenId.")]
        [SerializeField] private string loadScreenId = "";

        private void Awake()
        {
            _lsm = GetComponent<LoadScreenManager>() ?? FindFirstObjectByType<LoadScreenManager>();
            _gm  = GetComponent<GameManager.Runtime.GameManager>()
                ?? FindFirstObjectByType<GameManager.Runtime.GameManager>();

            if (_lsm == null)
                Debug.LogWarning("[LoadScreenManager/GameManagerBridge] LoadScreenManager not found.");

            if (_gm == null)
                Debug.LogWarning("[LoadScreenManager/GameManagerBridge] GameManager not found — load-screen automation disabled.");
        }

        private void OnEnable()
        {
            if (_gm != null)
            {
                _gm.OnBeforeChapterLoad += OnBeforeChapterLoad;
                _gm.OnAfterChapterLoad  += OnAfterChapterLoad;
            }
        }

        private void OnDisable()
        {
            if (_gm != null)
            {
                _gm.OnBeforeChapterLoad -= OnBeforeChapterLoad;
                _gm.OnAfterChapterLoad  -= OnAfterChapterLoad;
            }
        }

        private void OnBeforeChapterLoad(string chapterId)
        {
            _lsm?.Show(loadScreenId);
        }

        private void OnAfterChapterLoad(string chapterId)
        {
            _lsm?.Hide();
        }
    }
}
#else
namespace LoadScreenManager.Runtime
{
    /// <summary>No-op stub — define <c>LOADSCREENMANAGER_GM</c> to activate this bridge.</summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Game Manager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class GameManagerLoadScreenBridge : UnityEngine.MonoBehaviour { }
}
#endif
