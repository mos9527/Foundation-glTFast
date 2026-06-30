// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast.Schema
{
    /// <summary>
    /// EXT_foundation_environment scene extension payload.
    /// </summary>
    [Serializable]
    public class FoundationEnvironment
    {
        /// <summary>
        /// Environment type: <c>"color"</c> or <c>"hdri"</c>.
        /// </summary>
        public string type;

        /// <summary>
        /// Linear RGB environment radiance before <see cref="strength"/>.
        /// </summary>
        public float[] color;

        /// <summary>
        /// Relative URI to an environment image sidecar.
        /// </summary>
        public string uri;

        /// <summary>
        /// HDRI projection. Foundation uses <c>"longlat"</c> for equirectangular maps.
        /// </summary>
        public string projection;

        /// <summary>
        /// Multiplier applied to <see cref="color"/> or HDRI radiance.
        /// </summary>
        public float strength = 1f;

        /// <summary>
        /// Rotation in degrees around the vertical axis.
        /// </summary>
        public float azimuthOffset;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddPropertySafe("type", type);
            if (color != null && color.Length == 3)
            {
                writer.AddArrayProperty("color", color);
            }
            if (!string.IsNullOrEmpty(uri))
            {
                writer.AddPropertySafe("uri", uri);
            }
            if (!string.IsNullOrEmpty(projection))
            {
                writer.AddPropertySafe("projection", projection);
            }
            if (Math.Abs(strength - 1f) > Constants.epsilon)
            {
                writer.AddProperty("strength", strength);
            }
            if (Math.Abs(azimuthOffset) > Constants.epsilon)
            {
                writer.AddProperty("azimuthOffset", azimuthOffset);
            }
            writer.Close();
        }
    }
}
