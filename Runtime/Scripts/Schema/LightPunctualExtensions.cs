// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast.Schema
{
    /// <summary>
    /// Extensions on a punctual light
    /// </summary>
    [Serializable]
    public class LightPunctualExtensions
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// EXT_foundation_lights private extension
        /// </summary>
        public ExtFoundationLights EXT_foundation_lights;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Cleans up invalid parsing artifacts.
        /// </summary>
        /// <returns>True if the extension is valid, false otherwise.</returns>
        public bool JsonUtilityCleanup()
        {
            if (EXT_foundation_lights != null && EXT_foundation_lights.angularDiameter <= 0.0f)
            {
                EXT_foundation_lights = null;
            }
            return EXT_foundation_lights != null;
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (EXT_foundation_lights != null)
            {
                writer.AddProperty("EXT_foundation_lights");
                EXT_foundation_lights.GltfSerialize(writer);
            }
            writer.Close();
        }
    }

    /// <summary>
    /// EXT_foundation_lights properties
    /// </summary>
    [Serializable]
    public class ExtFoundationLights
    {
        /// <summary>
        /// Apparent size of the light source disk in radians
        /// </summary>
        public float angularDiameter = 0f;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("angularDiameter", angularDiameter);
            writer.Close();
        }
    }
}
