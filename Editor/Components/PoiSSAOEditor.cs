#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
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
                container.style.backgroundColor = new Color(0.21f, 0.21f, 0.21f);
            else
                container.style.backgroundColor = new Color(0.66f, 0.66f, 0.66f);

            // Info label explaining what the tool does
            var infoContainer = new VisualElement
            {
                style =
                {
                    paddingBottom = 5,
                    paddingTop = 5,
                    paddingRight = 5,
                    paddingLeft = 5,
                    marginBottom = 5,
                    flexWrap = Wrap.Wrap,
                    width = Length.Percent(100),
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.FlexStart,
                    borderBottomColor = new Color(0, 0, 0, 0.5f),
                    borderBottomWidth = 1.0f,
                    borderTopColor = new Color(0, 0, 0, 0.5f),
                    borderTopWidth = 1.0f,
                    borderRightColor = new Color(0, 0, 0, 0.5f),
                    borderRightWidth = 1.0f,
                    borderLeftColor = new Color(0, 0, 0, 0.5f),
                    borderLeftWidth = 1.0f,
                    backgroundColor = new Color(0, 0, 0, 0.15f)
                }
            };

            var info = new Label(
                "This tool facilitates the automatic setup of SSAO in Poiyomi Pro. The tool will automatically enable SSAO on all materials, and create animations and radial menu entries for controlling SSAO. For more information, specifics, and steps, click on the Question Mark (?) in the component's title bar.");
            info.style.flexWrap = Wrap.Wrap;
            info.style.whiteSpace = WhiteSpace.Normal;
            
            infoContainer.Add(info);
            container.Add(infoContainer);

            // Add material list
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

        /// <summary>
        ///     Populate the list of materials in one go
        /// </summary>
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
            foreach (var sharedMaterial in smr.sharedMaterials)
                // Dirty Poiyomi check
                if (sharedMaterial.shader.name.Contains("poiyomi") && sharedMaterial.shader.name.Contains("Pro"))
                    // Old version check (dont care)
                    if (!sharedMaterial.shader.name.Contains("9.1") && !sharedMaterial.shader.name.Contains("9.0") &&
                        !sharedMaterial.shader.name.Contains("8.2") && !sharedMaterial.shader.name.Contains("8.1") &&
                        !sharedMaterial.shader.name.Contains("8.0") && !sharedMaterial.shader.name.Contains("7.3"))
                        targetComponent.avatarPoiyomiMaterials.Add(sharedMaterial);

            serializedObject.Update();
            EditorUtility.SetDirty(targetComponent);
            serializedObject.ApplyModifiedProperties();

            if (targetComponent.avatarPoiyomiMaterials.Count >= 0)
                EditorUtility.DisplayDialog("AvatarTools",
                    "No Poiyomi Pro 9.2+ materials could be found! Please make sure you are using the latest version of Poiyomi Pro! Poiyomi Toon (free version) does not support SSAO!",
                    "Okay");

            Debug.Log($"Populated {_avatarMaterials.arraySize} materials.");
        }

        /// <summary>
        ///     Unlock all of the materials in our list and enable SSAO.
        /// </summary>
        private void UnlockAndEnableSSAO()
        {
            // If materials list is empty, we should return because we have no materials to operate on.
            if (_avatarMaterials.arraySize <= 0)
            {
                EditorUtility.DisplayDialog("AvatarTools",
                    "No Poiyomi Pro 9.2+ materials could be found! Please make sure you are using the latest version of Poiyomi Pro! Poiyomi Toon (free version) does not support SSAO!",
                    "Okay");
                return;
            }

            // Give the user a warning that we are unlocking all of their materials, some users may want to do this manually.
            var diag = EditorUtility.DisplayDialog("AvatarTools",
                "Warning: This will unlock all Poiyomi Pro 9.2+ materials and enable SSAO!", "Okay", "Cancel");
            if (!diag) return;

            Debug.Log("Unlocking materials and enabling SSAO");

            var targetComponent = (PoiSSAOSetup)target;
            var materials = targetComponent.avatarPoiyomiMaterials;

            // Use the reflected method to unlock all selected materials
            PoiyomiHelper.LockUnlockMaterials(materials, 0, false, true, false);

            // For each of the materials we need to enable SSAO and the animated property. This will not be default in-game,
            // but can be used to show the user what it will look like in-editor. 
            for (var i = 0; i < targetComponent.avatarPoiyomiMaterials.Count; i++)
            {
                var mat = targetComponent.avatarPoiyomiMaterials[i];

                mat.SetFloat("_SSAOEnabled", 1.0f);
                mat.SetFloat("_SSAOAnimationToggle", 1.0f);
            }
            
            // TODO: Enable animation on properties
            
            // Finally, we need to add a depth light to the avatar so the user can actually see what's going on
            // Get the VRC_AvatarDescriptor's root transform
            var root = targetComponent.GetComponentInParent<VRC_AvatarDescriptor>()?.transform;
            if (root == null)
            {
                Debug.LogWarning("No VRC_AvatarDescriptor found in the parent hierarchy.");
                return;
            }
            
            // Create the gameObject, name it, and add the depth light
            var depthObject = new GameObject();
            depthObject.name = "DepthGet";
            
            var light = depthObject.AddComponent<Light>();
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.type = LightType.Directional;
            light.intensity = 0.001f;
            light.color = Color.black;
            light.shadows = LightShadows.Hard;
            light.shadowStrength = 1f;
            light.shadowResolution = LightShadowResolution.Low;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.shadowNearPlane = 0.2f;
            light.cookieSize = 10.0f;
            light.renderMode = LightRenderMode.Auto;
            
            depthObject.transform.SetParent(root);
        }
    }
}
#endif