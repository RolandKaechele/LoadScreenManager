#if LOADSCREENMANAGER_SM
using StateManager.Runtime;
using UnityEngine;

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>StateManagerLoadScreenBridge</b> connects LoadScreenManager to StateManager without
    /// creating a hard compile-time dependency in either package.
    /// <para>
    /// When <c>LOADSCREENMANAGER_SM</c> is defined:
    /// <list type="bullet">
    /// <item>Subscribes to <c>StateManager.OnStatePushed</c>: when <see cref="AppState.Loading"/>
    /// is pushed onto the stack, <see cref="LoadScreenManager.Show"/> is called.</item>
    /// <item>Subscribes to <c>StateManager.OnStatePopped</c>: when the top of the stack is
    /// <see cref="AppState.Loading"/> and it is popped, <see cref="LoadScreenManager.Hide"/>
    /// is called.</item>
    /// <item>Optionally (when <see cref="pushStateOnShow"/> is true) pushes
    /// <see cref="AppState.Loading"/> onto StateManager when <see cref="LoadScreenManager.Show"/>
    /// is called from any other bridge, and pops it when the screen hides.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="LoadScreenManager"/>
    /// and add <c>LOADSCREENMANAGER_SM</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/State Manager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class StateManagerLoadScreenBridge : MonoBehaviour
    {
        private LoadScreenManager      _lsm;
        private StateManager.Runtime.StateManager _sm;

        [Tooltip("If true, this bridge pushes AppState.Loading when the load screen shows " +
                 "(and pops it when hidden). Useful when LoadScreenManager is the authority. " +
                 "Set to false when StateManager or another system drives the state changes.")]
        [SerializeField] private bool pushStateOnShow = false;

        [Tooltip("Id of the LoadScreenDefinition to show when AppState.Loading is pushed externally. " +
                 "Leave empty to use LoadScreenManager's defaultScreenId.")]
        [SerializeField] private string loadScreenId = "";

        private void Awake()
        {
            _lsm = GetComponent<LoadScreenManager>() ?? FindFirstObjectByType<LoadScreenManager>();
            _sm  = GetComponent<StateManager.Runtime.StateManager>()
                ?? FindFirstObjectByType<StateManager.Runtime.StateManager>();

            if (_lsm == null)
                Debug.LogWarning("[LoadScreenManager/StateManagerBridge] LoadScreenManager not found.");

            if (_sm == null)
                Debug.LogWarning("[LoadScreenManager/StateManagerBridge] StateManager not found — state-based load-screen automation disabled.");
        }

        private void OnEnable()
        {
            if (_sm != null)
            {
                _sm.OnStatePushed += OnStatePushed;
                _sm.OnStatePopped += OnStatePopped;
            }

            if (_lsm != null && pushStateOnShow)
            {
                _lsm.OnScreenShown  += OnScreenShown;
                _lsm.OnScreenHidden += OnScreenHidden;
            }
        }

        private void OnDisable()
        {
            if (_sm != null)
            {
                _sm.OnStatePushed -= OnStatePushed;
                _sm.OnStatePopped -= OnStatePopped;
            }

            if (_lsm != null)
            {
                _lsm.OnScreenShown  -= OnScreenShown;
                _lsm.OnScreenHidden -= OnScreenHidden;
            }
        }

        // -------------------------------------------------------------------------
        // State → LoadScreen
        // -------------------------------------------------------------------------

        private void OnStatePushed(AppState state)
        {
            if (state == AppState.Loading)
                _lsm?.Show(loadScreenId);
        }

        private void OnStatePopped(AppState state)
        {
            if (state == AppState.Loading)
                _lsm?.Hide();
        }

        // -------------------------------------------------------------------------
        // LoadScreen → State (when pushStateOnShow = true)
        // -------------------------------------------------------------------------

        private void OnScreenShown(string _)
        {
            if (_sm != null && _sm.CurrentState != AppState.Loading)
                _sm.PushState(AppState.Loading);
        }

        private void OnScreenHidden()
        {
            if (_sm != null && _sm.CurrentState == AppState.Loading)
                _sm.PopState();
        }
    }
}
#else
namespace LoadScreenManager.Runtime
{
    /// <summary>No-op stub — define <c>LOADSCREENMANAGER_SM</c> to activate this bridge.</summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/State Manager Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class StateManagerLoadScreenBridge : UnityEngine.MonoBehaviour { }
}
#endif
