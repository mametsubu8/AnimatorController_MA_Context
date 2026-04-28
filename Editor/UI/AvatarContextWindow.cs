using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AnimatorControllerMAContext.Editor
{
    public class AvatarContextWindow : EditorWindow
    {
        private GameObject _avatarRoot;
        private ContextSerializeOptions _options = new ContextSerializeOptions();
        private string _result = "";
        private Vector2 _scrollPosition;
        private bool _optionsFoldout = true;

        [MenuItem("Tools/Avatar Context")]
        public static void ShowWindow()
        {
            var window = GetWindow<AvatarContextWindow>("Avatar Context");
            window.minSize = new Vector2(450, 400);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            // Avatar Root
            EditorGUI.BeginChangeCheck();
            _avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar Root", _avatarRoot, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                _result = "";
            }

            EditorGUILayout.Space(4);

            // Options
            _optionsFoldout = EditorGUILayout.Foldout(_optionsFoldout, "Options", true);
            if (_optionsFoldout)
            {
                EditorGUI.indentLevel++;
                _options.IncludeAvatarDescriptor = EditorGUILayout.Toggle("Avatar Descriptor", _options.IncludeAvatarDescriptor);
                _options.IncludeExpressionParameters = EditorGUILayout.Toggle("Expression Parameters", _options.IncludeExpressionParameters);
                _options.IncludeExpressionMenu = EditorGUILayout.Toggle("Expression Menu", _options.IncludeExpressionMenu);
                _options.IncludeHierarchy = EditorGUILayout.Toggle("Hierarchy", _options.IncludeHierarchy);
                _options.IncludeMAComponents = EditorGUILayout.Toggle("Modular Avatar Components", _options.IncludeMAComponents);
                _options.IncludeVRCComponents = EditorGUILayout.Toggle("VRC Components (PhysBone etc.)", _options.IncludeVRCComponents);
                _options.IncludeRenderers = EditorGUILayout.Toggle("Renderers (BlendShapes)", _options.IncludeRenderers);
                _options.IncludeAnimatorControllers = EditorGUILayout.Toggle("AnimatorControllers", _options.IncludeAnimatorControllers);

                EditorGUI.BeginDisabledGroup(!_options.IncludeAnimatorControllers);
                EditorGUI.indentLevel++;
                _options.IncludeAnimationClips = EditorGUILayout.Toggle("AnimationClips in Controllers", _options.IncludeAnimationClips);
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = _avatarRoot != null;
                if (GUILayout.Button("Serialize", GUILayout.Height(28)))
                {
                    _result = AvatarContextSerializer.Serialize(_avatarRoot, _options);
                    _scrollPosition = Vector2.zero;
                }
                GUI.enabled = true;

                GUI.enabled = !string.IsNullOrEmpty(_result);
                if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(28)))
                {
                    GUIUtility.systemCopyBuffer = _result;
                    ShowNotification(new GUIContent("Copied to clipboard"));
                }
                if (GUILayout.Button("Save to File", GUILayout.Height(28)))
                {
                    SaveToFile();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            // Stats
            if (!string.IsNullOrEmpty(_result))
            {
                EditorGUILayout.Space(4);
                int lineCount = _result.Split('\n').Length;
                int charCount = _result.Length;
                EditorGUILayout.LabelField($"Lines: {lineCount:N0}  |  Characters: {charCount:N0}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);

            // Result text area
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                if (!string.IsNullOrEmpty(_result))
                {
                    EditorGUILayout.TextArea(_result, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Avatar RootにアバターのルートGameObjectを設定し、Serializeをクリックしてください。\n\n" +
                        "生成されるコンテキストには以下が含まれます:\n" +
                        "- VRC Avatar Descriptor (Playable Layers, Expressions)\n" +
                        "- Modular Avatar コンポーネント設定\n" +
                        "- PhysBone / Contact コンポーネント\n" +
                        "- SkinnedMeshRenderer (BlendShapes)\n" +
                        "- AnimatorController (AnimatorController Contextパッケージ利用)",
                        MessageType.Info
                    );
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void SaveToFile()
        {
            string defaultName = _avatarRoot != null ? $"{_avatarRoot.name}_context.txt" : "avatar_context.txt";
            string path = EditorUtility.SaveFilePanel("Save Avatar Context", "", defaultName, "txt");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                File.WriteAllText(path, _result, Encoding.UTF8);
                ShowNotification(new GUIContent("Saved"));
                Debug.Log($"[Avatar Context] Saved to {path}");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save file:\n{ex.Message}", "OK");
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject == null || _avatarRoot != null) return;

            var root = FindAvatarRoot(Selection.activeGameObject.transform);
            if (root != null)
            {
                _avatarRoot = root.gameObject;
                Repaint();
            }
        }

        private static Transform FindAvatarRoot(Transform t)
        {
            var current = t;
            while (current != null)
            {
                foreach (var comp in current.GetComponents<Component>())
                {
                    if (comp != null && comp.GetType().Name == "VRCAvatarDescriptor")
                        return current;
                }
                current = current.parent;
            }
            return null;
        }
    }
}
