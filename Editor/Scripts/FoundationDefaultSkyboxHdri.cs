// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GLTFast.Export;
using UnityEditor;
using UnityEngine;

namespace GLTFast.Editor
{
    /// <summary>
    /// Registers the baked default skybox HDRI used when Skybox mode has no assigned texture.
    /// The source <c>.hdr</c> is already an equirectangular (longlat) map, so its raw bytes are
    /// exported verbatim as the environment sidecar without re-encoding.
    /// </summary>
    static class FoundationDefaultSkyboxHdri
    {
        internal const string AssetPath = "Packages/com.atteneder.gltfast/Runtime/Textures/unity-skybox.hdr";

        [InitializeOnLoadMethod]
        static void RegisterFallback()
        {
            FoundationEnvironmentFallback.LoadDefaultSkyboxHdri = Load;
        }

        static byte[] Load()
        {
            var fullPath = ResolveAssetPath(AssetPath);
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                return File.ReadAllBytes(fullPath);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"EXT_foundation_environment: Failed to read default skybox HDRI '{fullPath}': {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolves a <c>Packages/&lt;pkg&gt;/...</c> asset path to a real file-system path so the
        /// raw bytes can be read directly, independent of import settings.
        /// </summary>
        static string ResolveAssetPath(string packagePath)
        {
            if (!packagePath.StartsWith("Packages/"))
            {
                return packagePath;
            }

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packagePath);
            if (packageInfo == null)
            {
                // Embedded/local package: the Packages directory is directly readable.
                return packagePath;
            }

            var relative = packagePath.Substring($"Packages/{packageInfo.name}/".Length);
            return Path.Combine(packageInfo.resolvedPath, relative);
        }
    }
}
