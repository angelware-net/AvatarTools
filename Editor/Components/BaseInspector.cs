#if UNITY_EDITOR
using VRC.SDKBase;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ANGELWARE.AvatarTools
{
    public class BaseInspector : Editor, IEditorOnly
    {
        public override VisualElement CreateInspectorGUI()
        {
            var baseInspectorUxmlPath = AssetDatabase.GUIDToAssetPath("b253e88f62420704393e113287a31839");
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(baseInspectorUxmlPath);

            // Instantiate UXML
            var root = visualTreeAsset.Instantiate();
            SetupContent(root);
            return root;
        }

        protected virtual void SetupContent(VisualElement root)
        {
            // Content
        }
    }
}
#endif