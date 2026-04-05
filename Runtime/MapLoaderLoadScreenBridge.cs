#if LOADSCREENMANAGER_MLF
using MapLoaderFramework.Runtime;
using UnityEngine;

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>MapLoaderLoadScreenBridge</b> connects LoadScreenManager to MapLoaderFramework without
    /// creating a hard compile-time dependency in either package.
    /// <para>
    /// When <c>LOADSCREENMANAGER_MLF</c> is defined:
    /// <list type="bullet">
    /// <item>Subscribes to <c>MapLoaderFramework.OnChapterChanged</c> and calls
    /// <see cref="LoadScreenManager.Show"/> so the load screen appears during chapter transitions.</item>
    /// <item>Subscribes to <c>MapLoaderFramework.OnMapLoaded</c> and calls
    /// <see cref="LoadScreenManager.Hide"/> once the new map is fully loaded.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add this component to the same GameObject as <see cref="LoadScreenManager"/>
    /// and add <c>LOADSCREENMANAGER_MLF</c> to Player Settings › Scripting Define Symbols.
    /// </para>
    /// </summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Map Loader Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderLoadScreenBridge : MonoBehaviour
    {
        private LoadScreenManager               _lsm;
        private MapLoaderFramework.Runtime.MapLoaderFramework _mlf;

        [Tooltip("Id of the LoadScreenDefinition shown during map/chapter transitions. " +
                 "Leave empty to use LoadScreenManager's defaultScreenId.")]
        [SerializeField] private string loadScreenId = "";

        private void Awake()
        {
            _lsm = GetComponent<LoadScreenManager>() ?? FindFirstObjectByType<LoadScreenManager>();
            _mlf = GetComponent<MapLoaderFramework.Runtime.MapLoaderFramework>()
                ?? FindFirstObjectByType<MapLoaderFramework.Runtime.MapLoaderFramework>();

            if (_lsm == null)
                Debug.LogWarning("[LoadScreenManager/MapLoaderBridge] LoadScreenManager not found.");

            if (_mlf == null)
                Debug.LogWarning("[LoadScreenManager/MapLoaderBridge] MapLoaderFramework not found — load-screen automation disabled.");
        }

        private void OnEnable()
        {
            if (_mlf != null)
            {
                _mlf.OnChapterChanged += OnChapterChanged;
                _mlf.OnMapLoaded      += OnMapLoaded;
            }
        }

        private void OnDisable()
        {
            if (_mlf != null)
            {
                _mlf.OnChapterChanged -= OnChapterChanged;
                _mlf.OnMapLoaded      -= OnMapLoaded;
            }
        }

        private void OnChapterChanged(int previousChapter, int newChapter)
        {
            _lsm?.Show(loadScreenId);
        }

        private void OnMapLoaded(MapData mapData)
        {
            _lsm?.Hide();
        }
    }
}
#else
namespace LoadScreenManager.Runtime
{
    /// <summary>No-op stub — define <c>LOADSCREENMANAGER_MLF</c> to activate this bridge.</summary>
    [UnityEngine.AddComponentMenu("LoadScreenManager/Map Loader Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderLoadScreenBridge : UnityEngine.MonoBehaviour { }
}
#endif
