// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using UnityEngine;

namespace GLTFast.Export
{
    /// <summary>
    /// Pending HDRI sidecar written during glTF bake.
    /// </summary>
    public sealed class FoundationEnvironmentHdriSidecar
    {
        /// <summary>
        /// Equirectangular source texture (typically an imported EXR HDRI) encoded via DDS writer.
        /// </summary>
        public UnityEngine.Texture SourceTexture { get; set; }

        /// <summary>
        /// Raw pre-encoded HDRI bytes (e.g. the baked default <c>.hdr</c>), written verbatim
        /// as a sidecar without re-encoding. Mutually exclusive with <see cref="SourceTexture"/>.
        /// </summary>
        public byte[] RawBytes { get; set; }

        /// <summary>
        /// Relative file name referenced by <see cref="FoundationEnvironment.uri"/>.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Scene extension payload whose <see cref="FoundationEnvironment.uri"/> must stay in sync with <see cref="FileName"/>.
        /// </summary>
        internal FoundationEnvironment EnvironmentPayload { get; set; }
    }

    /// <summary>
    /// Result of gathering Unity environment lighting for export.
    /// </summary>
    public sealed class FoundationEnvironmentGatherResult
    {
        /// <summary>
        /// Extension payload attached to the glTF scene.
        /// </summary>
        public FoundationEnvironment Environment { get; set; }

        /// <summary>
        /// Optional sidecar written next to the exported glTF/GLB.
        /// </summary>
        public FoundationEnvironmentHdriSidecar HdriSidecar { get; set; }
    }
}
