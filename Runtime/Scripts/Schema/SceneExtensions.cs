// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

namespace GLTFast.Schema
{
    /// <summary>
    /// Scene extensions.
    /// </summary>
    [System.Serializable]
    public class SceneExtensions
    {
        // ReSharper disable InconsistentNaming

        /// <inheritdoc cref="FoundationEnvironment"/>
        public FoundationEnvironment EXT_foundation_environment;

        // ReSharper restore InconsistentNaming

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (EXT_foundation_environment != null)
            {
                writer.AddProperty("EXT_foundation_environment");
                EXT_foundation_environment.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
