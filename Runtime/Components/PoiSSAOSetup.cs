#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ANGELWARE.AvatarTools;
using AnimatorAsCode.V1;
using AnimatorAsCode.V1.ModularAvatar;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

[assembly: ExportsPlugin(typeof(PoiSSAOSetupPlugin))]
namespace ANGELWARE.AvatarTools
{
    public class PoiSSAOSetup : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private bool enablePlugin;
        [SerializeField] public List<Material> avatarPoiyomiMaterials;
    }

    public class PoiSSAOSetupPlugin : Plugin<PoiSSAOSetupPlugin>
    {
        // Setup
        public override string QualifiedName => "com.angelware.at.ssao";
        public override string DisplayName => "Poi SSAO";
        
        private const string SystemName = "PoiSSAO";
        private const bool UseWriteDefaults = false;
        
        // Variables
        private SkinnedMeshRenderer[] _skinnedMeshRenderers;
        
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run($"Resolving {DisplayName}", Resolve);
            InPhase(BuildPhase.Generating).Run($"Generate {DisplayName}", Generate);
            InPhase(BuildPhase.Transforming).Run($"Transform {DisplayName}", Transform);
        }

        /// <summary>
        /// Get all skinned mesh renderers on the avatar
        /// </summary>
        /// <param name="ctx"></param>
        private void Resolve(BuildContext ctx)
        {
            _skinnedMeshRenderers = ctx.AvatarRootTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
        }
        
        /// <summary>
        /// Unlock and setup materials for SSAO
        /// </summary>
        /// <param name="ctx">Avatar Context</param>
        private void Generate(BuildContext ctx)
        {
            foreach (var smr in _skinnedMeshRenderers)
            {
                var materials = smr.sharedMaterials;
                List<Material> poiyomiMaterials = new List<Material>();

                foreach (var material in materials)
                {
                    if (material.shader.name.StartsWith(".poiyomi"))
                    {
                        poiyomiMaterials.Add(material);
                    }
                }
                
                Debug.Log("SSAO: Unlocking Materials");
                
                // Unlock all materials on the avatar
                // Thry.ShaderOptimizer.SetLockedForAllMaterials(poiyomiMaterials, 0, true, false, false);
            }
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

            // Setup parameter
            var ssaoParam = fx.FloatParameter("AT/SSAO");
            maAc.NewBoolToFloatParameter(ssaoParam);
            
            
        }
    }
}
#endif