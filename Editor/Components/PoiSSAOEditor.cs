#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase;

namespace ANGELWARE.AvatarTools
{
    [CustomEditor(typeof(PoiSSAOSetup))]
    public class PoiSSAOEditor : BaseInspector
    {
        private SerializedProperty _avatarMaterials;

        private void OnEnable()
        {
            _avatarMaterials = serializedObject.FindProperty("avatarPoiyomiMaterials");
        }
        
        protected override void SetupContent(VisualElement root)
        {
            serializedObject.Update();
         
            // Find root container and set color based on dark / light mode
            var container = root.Q<VisualElement>("Container");

            if (EditorGUIUtility.isProSkin)
            {
                container.style.backgroundColor = new Color(0.21f, 0.21f, 0.21f);
            }
            else
            {
                container.style.backgroundColor = new Color(0.66f, 0.66f, 0.66f);
            }
            
            container.Add(new PropertyField(_avatarMaterials));
            
            // Populate Material List Button
            var populateButton = new Button(PopulateMaterials)
            {
                text = "Populate Materials"
            };
            container.Add(populateButton);
            
            // Unlock and Setup Material for SSAO
            var setupButton = new Button(UnlockAndEnableSSAO)
            {
                text = "Setup Materials"
            };
            container.Add(setupButton);
        }

        private void PopulateMaterials()
        {
            // Get target component and clear list
            var targetComponent = (PoiSSAOSetup)target;
            targetComponent.avatarPoiyomiMaterials.Clear();
            
            // Get the VRC_AvatarDescriptor's root transform
            var root = targetComponent.GetComponentInParent<VRC_AvatarDescriptor>()?.transform;
            if (root == null)
            {
                Debug.LogWarning("No VRC_AvatarDescriptor found in the parent hierarchy.");
                return;
            }
            
            var skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshRenderers)
            {
                foreach (var sharedMaterial in smr.sharedMaterials)
                {
                    // Dirty Poiyomi check
                    if (sharedMaterial.shader.name.Contains("poiyomi") && sharedMaterial.shader.name.Contains("Pro"))
                    {
                        // Old version check (dont care)
                        if (!sharedMaterial.shader.name.Contains("9.1") && !sharedMaterial.shader.name.Contains("9.0") && !sharedMaterial.shader.name.Contains("8.2") && !sharedMaterial.shader.name.Contains("8.1") && !sharedMaterial.shader.name.Contains("8.0") && !sharedMaterial.shader.name.Contains("7.3"))
                            targetComponent.avatarPoiyomiMaterials.Add(sharedMaterial);
                    }
                }
            }
            
            serializedObject.Update();
            EditorUtility.SetDirty(targetComponent);
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"Populated {_avatarMaterials.arraySize} materials.");
        }

        private void UnlockAndEnableSSAO()
        {
            Debug.Log("Unlocking materials and enabling SSAO");
            
            var targetComponent = (PoiSSAOSetup)target;
            var materials = targetComponent.avatarPoiyomiMaterials;

            PoiyomiHelper.LockUnlockMaterials(materials, 0, false, true, false);

            for (var i = 0; i < targetComponent.avatarPoiyomiMaterials.Count; i++)
            {
                var mat = targetComponent.avatarPoiyomiMaterials[i];
                
                
            }
        }
    }
}
#endif