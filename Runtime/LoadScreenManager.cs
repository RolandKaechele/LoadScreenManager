using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace LoadScreenManager.Runtime
{
    /// <summary>
    /// <b>LoadScreenManager</b> is the central orchestrator for the loading-screen overlay.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Load <see cref="LoadScreenDefinition"/> JSON files from <c>Resources/LoadScreens/</c>
    /// and an optional external <c>LoadScreens/</c> folder on disk (mod / hot-load support).</item>
    /// <item>Show and hide the load screen on demand, driving the sub-controller
    /// <see cref="LoadScreenController"/>.</item>
    /// <item>Forward progress updates and tip overrides to the active controller.</item>
    /// <item>Expose delegate hooks consumed by bridge components for MapLoaderFramework,
    /// GameManager, StateManager, and DOTween integration.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to a persistent manager GameObject.
    /// Attach (or let auto-resolve) <see cref="LoadScreenController"/> on the same GameObject or in
    /// the scene. Place definition JSON files in <c>Assets/Resources/LoadScreens/</c>.
    /// </para>
    /// <para>
    /// <b>Optional integration defines:</b>
    /// <list type="bullet">
    ///   <item><c>LOADSCREENMANAGER_MLF</c>  — MapLoaderFramework: auto-show on chapter change, hide on map loaded.</item>
    ///   <item><c>LOADSCREENMANAGER_GM</c>   — GameManager: auto-show on chapter load start, hide on load complete.</item>
    ///   <item><c>LOADSCREENMANAGER_SM</c>   — StateManager: show when AppState.Loading is pushed, hide when it pops.</item>
    ///   <item><c>LOADSCREENMANAGER_DOTWEEN</c> — DOTween Pro: replaces coroutine fades with eased DOTween tweens.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("LoadScreenManager/Load Screen Manager")]
    [DisallowMultipleComponent]
#if ODIN_INSPECTOR
    public class LoadScreenManager : SerializedMonoBehaviour
#else
    public class LoadScreenManager : MonoBehaviour
#endif
    {
        // -------------------------------------------------------------------------
        // Inspector
        // -------------------------------------------------------------------------

        [Header("Sub-controller (auto-resolved if not assigned)")]
        [SerializeField] private LoadScreenController controller;

        [Header("Default screen")]
        [Tooltip("Id of the LoadScreenDefinition used when Show() is called without an id. " +
                 "Falls back to any loaded definition when empty.")]
        [SerializeField] private string defaultScreenId = "default_load";

        [Header("JSON / Modding")]
        [Tooltip("Folder within Resources/ that contains LoadScreenDefinition JSON files (without trailing slash).")]
        [SerializeField] private string resourcesFolder = "LoadScreens";

        [Tooltip("Enable loading / merging definition JSON files from Application.persistentDataPath at startup.")]
        [SerializeField] private bool loadFromExternalFolder = true;

        [Tooltip("Subfolder name inside Application.persistentDataPath used for external definitions.")]
        [SerializeField] private string externalFolderName = "LoadScreens";

        [Header("Loaded definitions (read-only)")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [SerializeField] private List<string> loadedIds = new();

        // -------------------------------------------------------------------------
        // Delegate hooks (set by bridge components)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Optional callback invoked instead of the default <see cref="LoadScreenController.Show"/>
        /// when the screen should appear. Signature: (definition, fadeDuration).
        /// Set automatically by <c>DotweenLoadScreenBridge</c> when <c>LOADSCREENMANAGER_DOTWEEN</c>
        /// is defined.
        /// </summary>
        public Action<LoadScreenDefinition, float> ShowOverride;

        /// <summary>
        /// Optional callback invoked instead of the default <see cref="LoadScreenController.Hide"/>
        /// when the screen should disappear. Signature: fadeDuration.
        /// Set automatically by <c>DotweenLoadScreenBridge</c> when <c>LOADSCREENMANAGER_DOTWEEN</c>
        /// is defined.
        /// </summary>
        public Action<float> HideOverride;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired when the load screen starts showing. Parameter: screen id (or empty).</summary>
        public event Action<string> OnScreenShown;

        /// <summary>Fired when the load screen finishes hiding.</summary>
        public event Action OnScreenHidden;

        // -------------------------------------------------------------------------
        // Internal state
        // -------------------------------------------------------------------------

        private readonly Dictionary<string, LoadScreenDefinition> _definitions = new();
        private string _activeId;

        /// <summary>True while the load screen is showing (or fading in).</summary>
        public bool IsShowing { get; private set; }

        // -------------------------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<LoadScreenController>()
                    ?? FindFirstObjectByType<LoadScreenController>();

            LoadAllDefinitions();
        }

        // -------------------------------------------------------------------------
        // Loading
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reload all <see cref="LoadScreenDefinition"/> JSON files from <c>Resources/LoadScreens/</c>
        /// and (if enabled) from <c>Application.persistentDataPath/LoadScreens/</c>.
        /// Previously loaded definitions are replaced; uniqueness is by <see cref="LoadScreenDefinition.id"/>.
        /// </summary>
        public void LoadAllDefinitions()
        {
            _definitions.Clear();

            // Bundled Resources
            var resources = Resources.LoadAll<TextAsset>(resourcesFolder);
            foreach (var ta in resources)
                TryAddDefinition(ta.text, source: $"Resources/{resourcesFolder}/{ta.name}");

            // External folder
            if (loadFromExternalFolder)
            {
                string externalPath = Path.Combine(Application.persistentDataPath, externalFolderName);
                if (Directory.Exists(externalPath))
                {
                    foreach (string file in Directory.GetFiles(externalPath, "*.json"))
                    {
                        try { TryAddDefinition(File.ReadAllText(file), source: file); }
                        catch (Exception ex)
                        { Debug.LogWarning($"[LoadScreenManager] Failed to read external definition '{file}': {ex.Message}"); }
                    }
                }
            }

            loadedIds.Clear();
            foreach (var id in _definitions.Keys) loadedIds.Add(id);

            Debug.Log($"[LoadScreenManager] Loaded {_definitions.Count} screen definition(s).");
        }

        private void TryAddDefinition(string json, string source)
        {
            try
            {
                var def = JsonUtility.FromJson<LoadScreenDefinition>(json);
                if (def == null || string.IsNullOrEmpty(def.id))
                {
                    Debug.LogWarning($"[LoadScreenManager] Definition at '{source}' has no id — skipped.");
                    return;
                }
                def.rawJson = json;
                _definitions[def.id] = def;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoadScreenManager] Failed to parse definition at '{source}': {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Show the load screen using the definition identified by <paramref name="screenId"/>.
        /// When <paramref name="screenId"/> is null or empty the <see cref="defaultScreenId"/> is used;
        /// if that is also not found, the first loaded definition is used; if none are loaded, a bare
        /// canvas fade is performed.
        /// </summary>
        public void Show(string screenId = null)
        {
            if (IsShowing) return;
            IsShowing = true;

            var def  = ResolveDefinition(screenId);
            _activeId = def?.id ?? string.Empty;

            float fadeDuration = def?.fadeInDuration ?? 0.3f;

            if (ShowOverride != null)
                ShowOverride(def, fadeDuration);
            else
                controller?.Show(def, fadeDuration);

            OnScreenShown?.Invoke(_activeId);
        }

        /// <summary>Hide the currently active load screen.</summary>
        public void Hide()
        {
            if (!IsShowing) return;
            IsShowing = false;

            var def = ResolveDefinition(_activeId);
            float fadeDuration = def?.fadeOutDuration ?? 0.3f;

            if (HideOverride != null)
                HideOverride(fadeDuration);
            else
                controller?.Hide(fadeDuration);

            OnScreenHidden?.Invoke();
        }

        /// <summary>Update the progress bar fill to <paramref name="value"/> (0–1).</summary>
        public void SetProgress(float value) => controller?.UpdateProgress(value);

        /// <summary>Override the displayed tip text directly.</summary>
        public void SetTip(string text) => controller?.SetTip(text);

        /// <summary>Override the background sprite directly.</summary>
        public void SetBackground(UnityEngine.Sprite sprite) => controller?.SetBackground(sprite);

        /// <summary>Returns the <see cref="LoadScreenDefinition"/> with the given id, or null.</summary>
        public LoadScreenDefinition GetDefinition(string id)
            => _definitions.TryGetValue(id, out var def) ? def : null;

        /// <summary>Returns all loaded definition ids.</summary>
        public IReadOnlyList<string> GetDefinitionIds() => loadedIds;

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private LoadScreenDefinition ResolveDefinition(string id)
        {
            if (!string.IsNullOrEmpty(id) && _definitions.TryGetValue(id, out var hit)) return hit;
            if (!string.IsNullOrEmpty(defaultScreenId) && _definitions.TryGetValue(defaultScreenId, out var dflt)) return dflt;
            foreach (var v in _definitions.Values) return v;   // first available
            return null;
        }
    }
}
