#if UNITY_EDITOR
using System;
using System.IO;
using LoadScreenManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoadScreenManager.Editor
{
    // ────────────────────────────────────────────────────────────────────────────
    // Load Screen Definition JSON Editor Window (per-file)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Editor window for creating and editing individual <c>LoadScreenDefinition</c> JSON files.
    /// Files are stored in <c>Assets/Resources/LoadScreens/</c> (loaded at runtime via Resources).
    /// Open via <b>JSON Editors → Load Screen Manager</b> or via the Manager Inspector button.
    /// </summary>
    public class LoadScreenJsonEditorWindow : EditorWindow
    {
        private static readonly string DefaultDirectory =
            Path.Combine(Application.dataPath, "Resources", "LoadScreens");

        private LoadScreenEditorBridge   _bridge;
        private UnityEditor.Editor       _bridgeEditor;

        private string   _directory;
        private string[] _files         = Array.Empty<string>();
        private int      _selectedIndex  = -1;
        private string   _selectedFile;
        private string   _newFileName    = "new_loadscreen";
        private Vector2  _fileListScroll;
        private Vector2  _editorScroll;
        private string   _status;
        private bool     _statusError;

        [MenuItem("JSON Editors/Load Screen Manager")]
        public static void ShowWindow() =>
            GetWindow<LoadScreenJsonEditorWindow>("Load Screen JSON");

        private void OnEnable()
        {
            _directory = DefaultDirectory;
            _bridge    = CreateInstance<LoadScreenEditorBridge>();
            RefreshFileList();
        }

        private void OnDisable()
        {
            if (_bridgeEditor != null) DestroyImmediate(_bridgeEditor);
            if (_bridge      != null) DestroyImmediate(_bridge);
        }

        private void OnGUI()
        {
            DrawDirectoryBar();

            EditorGUILayout.BeginHorizontal();

            // ── Left panel: file list ────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            DrawFileList();
            EditorGUILayout.EndVertical();

            // ── Separator ───────────────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            var sepRect = GUILayoutUtility.GetRect(2, float.MaxValue, 2, float.MaxValue);
            EditorGUI.DrawRect(sepRect, Color.gray * 0.5f);
            EditorGUILayout.EndVertical();

            // ── Right panel: editor ──────────────────────────────────────────
            EditorGUILayout.BeginVertical();
            DrawEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusError ? MessageType.Error : MessageType.Info);
        }

        // ── Directory bar ────────────────────────────────────────────────────

        private void DrawDirectoryBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Directory:", EditorStyles.miniLabel, GUILayout.Width(60));
            _directory = EditorGUILayout.TextField(_directory, EditorStyles.toolbarTextField);
            if (GUILayout.Button("Browse", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                var dir = EditorUtility.OpenFolderPanel("Select LoadScreens folder", _directory, "");
                if (!string.IsNullOrEmpty(dir)) { _directory = dir; RefreshFileList(); }
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(55)))
                RefreshFileList();
            EditorGUILayout.EndHorizontal();
        }

        // ── File list panel ──────────────────────────────────────────────────

        private void DrawFileList()
        {
            EditorGUILayout.LabelField("JSON Files", EditorStyles.boldLabel);

            _fileListScroll = EditorGUILayout.BeginScrollView(_fileListScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < _files.Length; i++)
            {
                var label   = Path.GetFileName(_files[i]);
                var style   = i == _selectedIndex ? EditorStyles.whiteLabel : EditorStyles.label;
                var rect    = GUILayoutUtility.GetRect(new GUIContent(label), style);
                if (i == _selectedIndex)
                    EditorGUI.DrawRect(rect, new Color(0.24f, 0.49f, 0.91f, 0.35f));
                if (GUI.Button(rect, label, style))
                    SelectFile(i);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("New file name:", EditorStyles.miniLabel);
            _newFileName = EditorGUILayout.TextField(_newFileName);
            if (GUILayout.Button("+ Create New"))
                CreateNewFile();
        }

        // ── Editor panel ─────────────────────────────────────────────────────

        private void DrawEditor()
        {
            if (_selectedFile == null)
            {
                EditorGUILayout.HelpBox("Select a file on the left, or create a new one.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(Path.GetFileName(_selectedFile), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete file",
                    $"Delete '{Path.GetFileName(_selectedFile)}'?",
                    "Delete", "Cancel"))
                    DeleteSelected();
            }
            EditorGUILayout.EndHorizontal();

            if (_bridge == null) return;
            if (_bridgeEditor == null)
                _bridgeEditor = UnityEditor.Editor.CreateEditor(_bridge);

            _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);
            _bridgeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        // ── File operations ──────────────────────────────────────────────────

        private void RefreshFileList()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            _files = Directory.GetFiles(_directory, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(_files);
            _selectedIndex = -1;
            _selectedFile  = null;
        }

        private void SelectFile(int index)
        {
            _selectedIndex = index;
            _selectedFile  = _files[index];
            LoadFile(_selectedFile);
        }

        private void CreateNewFile()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            var name = string.IsNullOrWhiteSpace(_newFileName) ? "new_loadscreen" : _newFileName.Trim();
            if (!name.EndsWith(".json")) name += ".json";
            var path = Path.Combine(_directory, name);

            if (File.Exists(path))
            {
                _status     = $"File '{name}' already exists.";
                _statusError = true;
                return;
            }

            var def = new LoadScreenDefinition { id = Path.GetFileNameWithoutExtension(name), label = name };
            File.WriteAllText(path, JsonUtility.ToJson(def, true));
            AssetDatabase.Refresh();
            RefreshFileList();

            // Select the newly created file
            for (int i = 0; i < _files.Length; i++)
            {
                if (_files[i] == path) { SelectFile(i); break; }
            }

            _status     = $"Created '{name}'.";
            _statusError = false;
        }

        private void LoadFile(string path)
        {
            try
            {
                var def = JsonUtility.FromJson<LoadScreenDefinition>(File.ReadAllText(path));
                _bridge.definition = def ?? new LoadScreenDefinition();

                if (_bridgeEditor != null) { DestroyImmediate(_bridgeEditor); _bridgeEditor = null; }

                _status     = $"Loaded '{Path.GetFileName(path)}'.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Load error: {e.Message}";
                _statusError = true;
            }
        }

        private void Save()
        {
            if (_selectedFile == null) return;
            try
            {
                File.WriteAllText(_selectedFile, JsonUtility.ToJson(_bridge.definition, true));
                AssetDatabase.Refresh();
                _status     = $"Saved '{Path.GetFileName(_selectedFile)}'.";
                _statusError = false;
            }
            catch (Exception e)
            {
                _status     = $"Save error: {e.Message}";
                _statusError = true;
            }
        }

        private void DeleteSelected()
        {
            if (_selectedFile == null) return;
            File.Delete(_selectedFile);
            var meta = _selectedFile + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
            AssetDatabase.Refresh();
            _status     = $"Deleted '{Path.GetFileName(_selectedFile)}'.";
            _statusError = false;
            RefreshFileList();
        }
    }

    // ── ScriptableObject bridge ──────────────────────────────────────────────
    internal class LoadScreenEditorBridge : ScriptableObject
    {
        public LoadScreenDefinition definition = new LoadScreenDefinition();
    }
}
#endif
