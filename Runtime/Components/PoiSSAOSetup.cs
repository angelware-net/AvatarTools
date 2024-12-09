#if UNITY_EDITOR
using System.Collections.Generic;
using ANGELWARE.AvatarTools;
using AnimatorAsCode.V1;
using AnimatorAsCode.V1.ModularAvatar;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using LightType = UnityEngine.LightType;

[assembly: ExportsPlugin(typeof(PoiSSAOSetupPlugin))]
namespace ANGELWARE.AvatarTools
{
    [AddComponentMenu("ANGELWARE/Avatar Tools/Poiyomi SSAO Configurator")]
    [Icon("Packages/com.angelware.at/Editor/Resources/Textures/documenticon.png")]
    [DisallowMultipleComponent]
    public class PoiSSAOSetup : MonoBehaviour, IEditorOnly
    {
        [SerializeField] public List<Material> avatarPoiyomiMaterials;
        [SerializeField] public bool enableIntensity = true;
        [SerializeField] public bool createMenuEntry = true;
    }

    public class PoiSSAOSetupPlugin : Plugin<PoiSSAOSetupPlugin>
    {
        // Setup
        public override string QualifiedName => "com.angelware.at.ssao";
        public override string DisplayName => "Poi SSAO";
        private const string SystemName = "PoiSSAO";
        private const bool UseWriteDefaults = false;
        
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).Run($"Transform {DisplayName}", Transform);
        }
        
        /// <summary>
        /// Generate animations for SSAO
        /// </summary>
        /// <param name="ctx">Avatar Context</param>
        private void Transform(BuildContext ctx)
        {
            // Find components to run on
            var components = ctx.AvatarRootTransform.GetComponentsInChildren<PoiSSAOSetup>(false);
            if (components.Length == 0) return;
            
            // Initialize Animator As Code.
            var aac = AacV1.Create(new AacConfiguration
            {
                SystemName = SystemName,
                AnimatorRoot = ctx.AvatarRootTransform,
                DefaultValueRoot = ctx.AvatarRootTransform,
                AssetKey = GUID.Generate().ToString(),
                AssetContainer = ctx.AssetContainer,
                ContainerMode = AacConfiguration.Container.OnlyWhenPersistenceRequired,
                // States will be created with Write Defaults set to ON or OFF based on whether UseWriteDefaults is true or false.
                DefaultsProvider = new AacDefaultsProvider(UseWriteDefaults)
            });
            
            // Create controller
            var ctrl = aac.NewAnimatorController();

            // Initialize MaAc
            var maAc = MaAc.Create(new GameObject(SystemName)
            {
                transform = { parent = ctx.AvatarRootTransform }
            });
            
            // We don't run a for loop here, because we only want to ever run this plugin once
            var my = components[0];
            var fx = ctrl.NewLayer($"AAT_SSAO");

            // Setup parameters
            var oneFloat = fx.FloatParameter("AT/OneFloat");
            fx.OverrideValue(oneFloat, 1.0f);
            maAc.NewParameter(oneFloat).NotSynced().WithDefaultValue(1.0f);
            
            var ssaoParam = fx.FloatParameter("AT/SSAO");
            maAc.NewBoolToFloatParameter(ssaoParam).NotSaved().WithDefaultValue(false);

            var ssaoLightParam = fx.FloatParameter("AT/SSAO/Light");
            maAc.NewBoolToFloatParameter(ssaoLightParam).NotSaved().WithDefaultValue(false);

            var ssaoAmountParam = fx.FloatParameter("AT/SSAO/Amount");
            if (my.enableIntensity)
            {
                fx.OverrideValue(ssaoAmountParam, 0.2f);
                maAc.NewParameter(ssaoAmountParam).NotSaved().WithDefaultValue(0.2f);
            }
            
            // Get relevant skinned mesh renderers to animate on
            List<SkinnedMeshRenderer> poiSkinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            
            // hahahaha wtf
            var skinnedMeshRenderers = ctx.AvatarRootTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (var i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                var sharedMaterials = skinnedMeshRenderers[i].sharedMaterials;
                for (var j = 0; j < sharedMaterials.Length; j++)
                {
                    if (!sharedMaterials[j].shader.name.Contains("poiyomi") ||
                        !sharedMaterials[j].shader.name.Contains("Pro")) continue;
                    
                    if (!sharedMaterials[j].shader.name.Contains("9.1") && !sharedMaterials[j].shader.name.Contains("9.0") &&
                        !sharedMaterials[j].shader.name.Contains("8.2") && !sharedMaterials[j].shader.name.Contains("8.1") &&
                        !sharedMaterials[j].shader.name.Contains("8.0") && !sharedMaterials[j].shader.name.Contains("7.3"))
                        poiSkinnedMeshRenderers.Add(skinnedMeshRenderers[i]);
                }
            }

            // Create animation clips
            var enableClip = aac.NewClip();
            var disableClip = aac.NewClip();
            var amountClip0 = aac.NewClip();
            var amountClip1 = aac.NewClip();
            
            for (var i = 0; i < poiSkinnedMeshRenderers.Count; i++)
            {
                enableClip.Animating(clip =>
                {
                    clip.Animates(poiSkinnedMeshRenderers[i], $"material._SSAOAnimationToggle")
                        .WithOneFrame(1.0f);
                });
                
                disableClip.Animating(clip =>
                {
                    clip.Animates(poiSkinnedMeshRenderers[i], $"material._SSAOAnimationToggle")
                        .WithOneFrame(0.0f);
                });

                if (my.enableIntensity)
                {
                    amountClip0.Animating(clip =>
                    {
                        clip.Animates(poiSkinnedMeshRenderers[i], $"material._SSAOIntensity")
                            .WithOneFrame(0.0f);
                    });

                    amountClip1.Animating(clip =>
                    {
                        clip.Animates(poiSkinnedMeshRenderers[i], $"material._SSAOIntensity")
                            .WithOneFrame(5.0f);
                    });
                }
            }

            // Find the depth light
            var lightObject = CreateDepthLight(ctx);
            
            // Animate the depth light
            var enableLightClip = aac.NewClip().Animating(clip =>
            {
                clip.Animates(lightObject).WithOneFrame(1.0f);
            });
            
            var disableLightClip = aac.NewClip().Animating(clip =>
            {
                clip.Animates(lightObject).WithOneFrame(0.0f);
            });
            
            // Create the blend tree
            var bt = aac.NewBlendTree().Direct();

            // Enable / disable tree
            var enableTree = aac.NewBlendTree()
                .Simple1D(ssaoParam)
                .WithAnimation(enableClip, 1.0f)
                .WithAnimation(disableClip, 0.0f);
            bt.WithAnimation(enableTree, oneFloat);
            
            if (my.enableIntensity)
            {
                // Intensity tree
                var intensityTree = aac.NewBlendTree()
                    .Simple1D(ssaoAmountParam)
                    .WithAnimation(amountClip0, 0.0f)
                    .WithAnimation(amountClip1, 1.0f);
                bt.WithAnimation(intensityTree, oneFloat);
            }
            
            // Depth light tree
            var depthLightTree = aac.NewBlendTree()
                .Simple1D(ssaoLightParam)
                .WithAnimation(disableLightClip, 0.0f)
                .WithAnimation(enableLightClip, 1.0f);
            bt.WithAnimation(depthLightTree, oneFloat);
            
            // Setup animator layer
            fx.NewState("BlendTree").WithAnimation(bt).WithWriteDefaultsSetTo(true);
            
            // Merge animator
            maAc.NewMergeAnimator(ctrl, VRCAvatarDescriptor.AnimLayerType.FX);

        }

        /// <summary>
        ///     Check for a depth light, if our tool was used there will already be one on the avatar, if the user used
        ///     the Poiyomi prefab, we can check for that too, otherwise check for all Directional lights and check their
        ///     color, if the color is black, we will assume it is a depth light. Finally if no objects are found we should
        ///     create a new depth light here.
        ///
        ///     This is super complicated but it's the safest way I can think to do this at the moment.
        /// </summary>
        /// <param name="ctx">Avatar Build Context</param>
        private GameObject CreateDepthLight(BuildContext ctx)
        {
            GameObject depthLight = null;
            var lightObjects = ctx.AvatarRootTransform.GetComponentsInChildren<Light>();
            
            // Find all objects with the name "DepthLight" in them, this is to check for multiples. If we have multiples
            // we will have to remove them all and create a new light, as we might run into issues with control.
            var depthLights = new List<GameObject>();
            for (var i = 0; i < lightObjects.Length; i++)
            {
                var lightObject = lightObjects[i];
                
                if (lightObject.gameObject.name.Contains("DepthGet"))
                {
                    depthLights.Add(lightObject.gameObject);
                }
            }

            // Destroy the depth lights
            if (depthLights.Count > 1)
            {
                for (var i = 0; i < depthLights.Count; i++)
                {
                    var dL = depthLights[i];
                    GameObject.Destroy(dL);
                    Debug.Log("AT SSAO: Found extra depth light, destroying...");
                }
            } 
            // Return if only one depth light was found
            else if (depthLights.Count == 1)
            {
                depthLight = depthLights[0].gameObject;
                Debug.Log("AT SSAO: Depth Light found!");
                return depthLight;
            }

            Debug.Log("AT SSAO: Creating a new depth light");
            // Create a new depth light
            depthLight = new GameObject();
            depthLight.name = "DepthLight";
            var light = depthLight.AddComponent<Light>();
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
            
            depthLight.transform.SetParent(ctx.AvatarRootTransform);

            return depthLight;
        }
    }
}
#endif