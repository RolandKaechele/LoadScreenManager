#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LoadScreenManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="LoadScreenManager.Runtime.LoadScreenManager"/>.
    /// Adds runtime test controls — show/hide the load screen and set progress — directly
    /// from the Unity Inspector.
    /// </summary>
    [CustomEditor(typeof(LoadScreenManager.Runtime.LoadScreenManager))]
    public class LoadScreenManagerEditor : UnityEditor.Editor
    {
        private string _testScreenId = "";
        private float  _testProgress = 0f;
        private string _testTip      = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (LoadScreenManager.Runtime.LoadScreenManager)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls.", MessageType.Info);
                return;
            }

            // Show / Hide
            EditorGUILayout.LabelField("Show / Hide", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _testScreenId = EditorGUILayout.TextField("Screen ID (empty = default)", _testScreenId);
            if (GUILayout.Button("Show", GUILayout.Width(60)))
                mgr.Show(_testScreenId);
            if (GUILayout.Button("Hide", GUILayout.Width(60)))
                mgr.Hide();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Is Showing: {mgr.IsShowing}", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // Progress
            EditorGUILayout.LabelField("Progress", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _testProgress = EditorGUILayout.Slider(_testProgress, 0f, 1f);
            if (GUILayout.Button("Set", GUILayout.Width(50)))
                mgr.SetProgress(_testProgress);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Tip
            EditorGUILayout.LabelField("Tip Override", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _testTip = EditorGUILayout.TextField(_testTip);
            if (GUILayout.Button("Set", GUILayout.Width(50)))
                mgr.SetTip(_testTip);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Reload
            if (GUILayout.Button("Reload All Definitions"))
                mgr.LoadAllDefinitions();
        }
    }
}
#endif
