// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace GLTFast.Schema
{
    /// <summary>
    /// EXT_foundation_colormanagement root extension payload.
    /// </summary>
    [Serializable]
    public class FoundationColorManagement
    {
        /// <summary>
        /// Blender color-management exposure, in EV.
        /// </summary>
        public float postExposure;

        /// <summary>
        /// Foundation SDR LUT tuple formatted as <c>SDR / View / Look</c>.
        /// </summary>
        public string sdr;

        /// <summary>
        /// Foundation HDR LUT tuple formatted as <c>HDR / View / Look</c>.
        /// </summary>
        public string hdr;

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            writer.AddProperty("postExposure", postExposure);
            writer.AddPropertySafe("sdr", sdr);
            writer.AddPropertySafe("hdr", hdr);
            writer.Close();
        }
    }
}
