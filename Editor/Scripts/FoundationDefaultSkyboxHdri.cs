// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Export;
using UnityEditor;
using UnityEngine;

namespace GLTFast.Editor
{
    /// <summary>
    /// Registers the baked default skybox HDRI used when Skybox mode has no assigned texture.
    /// </summary>
    static class FoundationDefaultSkyboxHdri
    {
        internal const string AssetPath = "Packages/com.atteneder.gltfast/Runtime/Textures/unity-skybox.hdr";

        [InitializeOnLoadMethod]
        static void RegisterFallback()
        {
            FoundationEnvironmentFallback.LoadDefaultSkyboxHdri = Load;
        }

        static UnityEngine.Texture Load()
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Texture>(AssetPath);
        }
    }
}
