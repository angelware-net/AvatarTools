#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ANGELWARE.AvatarTools
{
    /// <summary>
    /// I don't want to create a hard dependency on Poiyomi / Thry editor, in case people are using tools unrelated and
    /// want to use a different shader. Instead of directly calling methods, we will have to create a soft-dependency
    /// using reflection and call the methods that way. This is mainly just for locking / unlocking materials.
    /// </summary>
    public class PoiyomiHelper : AssetPostprocessor
    {
#if !POIYOMI_TOON && !POIYOMI_PRO
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Every time we import an asset we first check the type of imported asset, if it is *not* a shader file or
            // a .unitypackage file, we should continue and step out of the loop to check the next asset. This will keep
            // us from checking for the directory every single asset that is imported. 
            //
            // I am not entirely sure the performance implications of this setup, if someone knows a better solution 
            // please PR. 
            //
            // Realistically though, in my silly little head, this should not kill performance of asset imports.
            for (var i = 0; i < importedAssets.Length; i++)
            {
                var asset = importedAssets[i];
                
                // Check for unitypackage or shader files. We check for both because if we import via VPM it obviously
                // won't be using a unitypackage file. This is kinda jank and I should probably consult for a better way
                // to do this.
                if (!asset.EndsWith(".unitypackage") && !asset.EndsWith(".shader")) continue;
                
                // These are the two possible paths Poiyomi could live at, if we don't find either, than just return.
                var assetsPath = Path.Combine("Assets", "_PoiyomiShaders");
                var packagesPath = Path.Combine("Packages", "com.poiyomi.toon");

                if (!Directory.Exists(assetsPath) && !Directory.Exists(packagesPath)) return;

                // If the ModularShader folder is found, we must be using Poi Pro, so we can set the directive accordingly
                var proShaderPath = Path.Combine("Assets", "_PoiyomiShaders", "ModularShader");

                // Check our build target, if we're not using Android, it must be standalone, anything else is a crazy
                // misconfig that we can just ignore.
                BuildTargetGroup buildTarget;
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                {
                    buildTarget = BuildTargetGroup.Android;
                }
                else
                {
                    buildTarget = BuildTargetGroup.Standalone;
                }

                // Get all defines for build target group
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

                // Set define symbols based on presence of Poiyomi and the version type
                if (Directory.Exists(proShaderPath))
                {
                    const string symbol = "POIYOMI_PRO";

                    // Avoid duplicates
                    if (!defines.Contains(symbol))
                    {
                        defines = string.IsNullOrEmpty(defines) ? symbol : $"{defines};{symbol}";
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
                        Debug.Log($"Added directive: {symbol}");
                    }
                    else
                    {
                        Debug.LogWarning($"Directive '{symbol}' already exists.");
                    }
                }
                else
                {
                    const string symbol = "POIYOMI_TOON";

                    // Avoid duplicates
                    if (!defines.Contains(symbol))
                    {
                        defines = string.IsNullOrEmpty(defines) ? symbol : $"{defines};{symbol}";
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
                        Debug.Log($"Added directive: {symbol}");
                    }
                    else
                    {
                        Debug.LogWarning($"Directive '{symbol}' already exists.");
                    }
                }
            }
        }
#endif

        public static bool LockUnlockMaterials(IEnumerable<Material> materials, int lockState, bool showDialog, bool showProgressBar, bool allowCancel)
        {
            try
            {
                // Locate the assembly
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "ThryAssemblyDefinition");
                if (assembly == null)
                {
                    Debug.LogWarning("ThryAssemblyDefinition assembly not found.");
                    return false;
                }

                // Locate the type
                var type = assembly.GetType("Thry.ShaderOptimizer");
                if (type == null)
                {
                    Debug.LogWarning("ShaderOptimizer type not found in ThryAssemblyDefinition.");
                    return false;
                }

                // Locate the method
                var method = type.GetMethod("SetLockedForAllMaterials", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogWarning("SetLockedForAllMaterials method not found in ShaderOptimizer.");
                    return false;
                }

                // Prepare parameters
                object[] parameters = { materials, lockState, showProgressBar, showDialog, allowCancel, null };

                // Invoke the method
                var result = method.Invoke(null, parameters);

                // Return the result
                return result != null && (bool)result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error invoking SetLockedForAllMaterials: {e.Message}");
                return false;
            }
        }
    }
}

#endif