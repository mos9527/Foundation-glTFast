// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace GLTFast.Export
{
    /// <summary>
    /// Editor-provided fallbacks for environment export when scene data is incomplete.
    /// </summary>
    public static class FoundationEnvironmentFallback
    {
        /// <summary>
        /// Loads the baked default skybox HDRI. Set by glTFast.Editor on domain reload.
        /// </summary>
        public static Func<UnityEngine.Texture> LoadDefaultSkyboxHdri { get; set; }
    }
}
