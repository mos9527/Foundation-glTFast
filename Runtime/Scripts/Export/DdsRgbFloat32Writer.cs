// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Buffers.Binary;
using System.IO;
using UnityEngine;

namespace GLTFast.Export
{
    /// <summary>
    /// Writes uncompressed DDS files with DXGI <c>R32G32B32A32_FLOAT</c> pixel data.
    /// </summary>
    static class DdsRgbFloat32Writer
    {
        const uint k_Magic = 0x20534444; // "DDS "
        const uint k_HeaderSize = 124;
        const uint k_HeaderFlags = 0x1 | 0x2 | 0x4 | 0x8 | 0x1000;
        const uint k_CapsTexture = 0x1000;
        const uint k_PixelFormatFlagsFourCC = 0x4;
        const uint k_FourCCDx10 = 0x30315844; // "DX10"
        const uint k_DxgiFormatR32G32B32A32Float = 2;
        const uint k_ResourceDimensionTexture2D = 3;
        const int k_BytesPerPixel = 16;
        const int k_HeaderBytes = 148;

        static Material s_CubemapToEquirectMaterial;

        /// <summary>
        /// Reads <paramref name="source"/> into linear RGBAFloat and writes an uncompressed DDS file.
        /// </summary>
        public static bool TryWriteTexture(Texture source, string filePath, bool overwrite)
        {
            if (source == null || string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            if (File.Exists(filePath) && !overwrite)
            {
                return false;
            }

            try
            {
                if (source is Cubemap cubemap)
                {
                    return TryWriteCubemap(cubemap, filePath);
                }

                return TryWriteTexture2D(source, filePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to encode environment DDS '{filePath}': {exception.Message}");
                return false;
            }
        }

        static bool TryWriteCubemap(Cubemap cubemap, string filePath)
        {
            var material = GetCubemapToEquirectMaterial();
            if (material == null)
            {
                Debug.LogWarning("Missing shader Hidden/glTFExportCubemapToEquirect; cannot export cubemap skybox HDRI.");
                return false;
            }

            var faceSize = cubemap.width;
            var width = faceSize * 2;
            var height = faceSize;
            return BlitToDds(
                rt =>
                {
                    material.SetTexture("_CubeTex", cubemap);
                    Graphics.Blit(null, rt, material);
                },
                width,
                height,
                filePath
            );
        }

        static bool TryWriteTexture2D(Texture source, string filePath)
        {
            if (source is Texture2D { isReadable: true } readableSource
                && readableSource.format == TextureFormat.RGBAFloat)
            {
                WriteRgbaFloatTexture(readableSource, filePath);
                return true;
            }

            return BlitToDds(rt => Graphics.Blit(source, rt), source.width, source.height, filePath);
        }

        static bool BlitToDds(Action<RenderTexture> blit, int width, int height, string filePath)
        {
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning($"Cannot export environment DDS with invalid dimensions {width}x{height}.");
                return false;
            }

            Texture2D readable = null;
            RenderTexture temporary = null;
            var previousActive = RenderTexture.active;

            try
            {
                temporary = RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear
                );
                blit(temporary);
                RenderTexture.active = temporary;
                readable = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                readable.Apply(false, false);
                WriteRgbaFloatTexture(readable, filePath);
                return true;
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }

                if (readable != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(readable);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(readable);
                    }
                }
            }
        }

        static Material GetCubemapToEquirectMaterial()
        {
            if (s_CubemapToEquirectMaterial == null)
            {
                var shader = Shader.Find("Hidden/glTFExportCubemapToEquirect");
                if (shader == null)
                {
                    return null;
                }

                s_CubemapToEquirectMaterial = new Material(shader);
            }

            return s_CubemapToEquirectMaterial;
        }

        static void WriteRgbaFloatTexture(Texture2D texture, string filePath)
        {
            var width = texture.width;
            var height = texture.height;
            var rowStride = width * k_BytesPerPixel;
            var pixelBytes = texture.GetRawTextureData<byte>();

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            WriteHeader(stream, width, height, rowStride);

            var rowBuffer = new byte[rowStride];
            for (var y = 0; y < height; y++)
            {
                var srcOffset = y * rowStride;
                Unity.Collections.NativeArray<byte>.Copy(pixelBytes, srcOffset, rowBuffer, 0, rowStride);
                stream.Write(rowBuffer, 0, rowStride);
            }
        }

        static void WriteHeader(Stream stream, int width, int height, int rowStride)
        {
            Span<byte> header = stackalloc byte[k_HeaderBytes];
            header.Clear();

            var offset = 0;
            WriteUInt32(header, ref offset, k_Magic);
            WriteUInt32(header, ref offset, k_HeaderSize);
            WriteUInt32(header, ref offset, k_HeaderFlags);
            WriteUInt32(header, ref offset, (uint)height);
            WriteUInt32(header, ref offset, (uint)width);
            WriteUInt32(header, ref offset, (uint)rowStride);
            offset += 8; // depth, mipMapCount
            offset += 44; // reserved1

            WriteUInt32(header, ref offset, 32); // pixel format size
            WriteUInt32(header, ref offset, k_PixelFormatFlagsFourCC);
            WriteUInt32(header, ref offset, k_FourCCDx10);
            offset += 20; // remaining pixel format fields

            WriteUInt32(header, ref offset, k_CapsTexture);
            offset += 16; // caps2, caps3, caps4, reserved2

            WriteUInt32(header, ref offset, k_DxgiFormatR32G32B32A32Float);
            WriteUInt32(header, ref offset, k_ResourceDimensionTexture2D);
            offset += 4; // miscFlag
            WriteUInt32(header, ref offset, 1); // arraySize

            stream.Write(header.ToArray());
        }

        static void WriteUInt32(Span<byte> buffer, ref int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset, 4), value);
            offset += 4;
        }
    }
}
