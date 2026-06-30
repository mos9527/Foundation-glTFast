// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

namespace GLTFast.Schema
{
    static class Constants
    {
        public const float epsilon = .001f;

        /// <summary>
        /// Radiometric watt to photometric lumen at 555 nm (SI candela peak).
        /// Used when exporting from Unity pipelines that store non-physical light
        /// intensities into glTF fields that expect candela or lux.
        /// </summary>
        internal const float PbrWattsToLumens = 683f;

        /// <summary>
        /// Default angular diameter for soft shadows (in degrees).
        /// </summary>
        internal const float SoftShadowAngularDiameter = 1.0f;
    }
}
