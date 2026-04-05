using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace LoadScreenManager.Runtime
{
    // -------------------------------------------------------------------------
    // LoadScreenDefinition
    // -------------------------------------------------------------------------

    /// <summary>
    /// Describes a single load-screen configuration that can be displayed while a scene or chapter
    /// is loading. Authored in JSON and stored in <c>Resources/LoadScreens/</c> or the external
    /// <c>LoadScreens/</c> folder (mod support).
    /// </summary>
    [Serializable]
    public class LoadScreenDefinition
    {
#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
#endif
        /// <summary>Unique identifier referenced by <see cref="LoadScreenManager"/> and bridge components.</summary>
        public string id;

#if ODIN_INSPECTOR
        [BoxGroup("Identity")]
#endif
        /// <summary>Optional human-readable label (used in the Inspector and Editor).</summary>
        public string label;

#if ODIN_INSPECTOR
        [BoxGroup("Visuals"), LabelText("Background (Resources path)")]
#endif
        /// <summary>
        /// Path (without extension) to a <c>Sprite</c> asset in <c>Resources/</c> used as the
        /// full-screen background while loading. Leave empty to keep the Controller's default background.
        /// </summary>
        public string backgroundResource;

#if ODIN_INSPECTOR
        [BoxGroup("Visuals"), LabelText("Spinner graphic (Resources path)")]
#endif
        /// <summary>
        /// Path (without extension) to a <c>Sprite</c> in <c>Resources/</c> used as the spinner
        /// (loading wheel) graphic. Leave empty to keep the Controller's default spinner sprite.
        /// </summary>
        public string spinnerResource;

#if ODIN_INSPECTOR
        [BoxGroup("Visuals"), LabelText("Progress fill (Resources path)")]
#endif
        /// <summary>
        /// Path (without extension) to a <c>Sprite</c> in <c>Resources/</c> used as the progress
        /// bar fill image. Leave empty to keep the Controller's default progress fill sprite.
        /// </summary>
        public string progressFillResource;

#if ODIN_INSPECTOR
        [BoxGroup("Tips")]
#endif
        /// <summary>
        /// Pool of loading tip strings. One is chosen at random (and rotated every
        /// <see cref="tipRotationInterval"/> seconds) when <see cref="showTips"/> is true.
        /// </summary>
        public string[] tipPool = Array.Empty<string>();

#if ODIN_INSPECTOR
        [BoxGroup("Visibility")]
#endif
        /// <summary>Whether to show and update the progress bar during loading.</summary>
        public bool showProgress = true;

#if ODIN_INSPECTOR
        [BoxGroup("Visibility")]
#endif
        /// <summary>Whether to show the animated spinner graphic during loading.</summary>
        public bool showSpinner = true;

#if ODIN_INSPECTOR
        [BoxGroup("Visibility")]
#endif
        /// <summary>Whether to show tips from <see cref="tipPool"/> during loading.</summary>
        public bool showTips = true;

#if ODIN_INSPECTOR
        [BoxGroup("Tips"), MinValue(0f)]
#endif
        /// <summary>
        /// Seconds between tip rotations when <see cref="showTips"/> is true and the tip pool has
        /// more than one entry. A value of 0 disables rotation.
        /// </summary>
        public float tipRotationInterval = 4f;

#if ODIN_INSPECTOR
        [BoxGroup("Fades"), MinValue(0f)]
#endif
        /// <summary>Duration in seconds to fade the load screen in (0 = instant).</summary>
        public float fadeInDuration = 0.3f;

#if ODIN_INSPECTOR
        [BoxGroup("Fades"), MinValue(0f)]
#endif
        /// <summary>Duration in seconds to fade the load screen out (0 = instant).</summary>
        public float fadeOutDuration = 0.3f;

        /// <summary>Raw JSON text stored during deserialisation (not serialised back).</summary>
        [NonSerialized] public string rawJson;
    }
}
