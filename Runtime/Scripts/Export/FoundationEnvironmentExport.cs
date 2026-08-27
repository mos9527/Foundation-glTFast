// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GLTFast.Schema;
using UnityEngine;
using UnityEngine.Rendering;

namespace GLTFast.Export
{
    /// <summary>
    /// Gathers Unity environment lighting for EXT_foundation_environment export.
    /// </summary>
    public static class FoundationEnvironmentExport
    {
        static readonly int s_MainTexProperty = Shader.PropertyToID("_MainTex");
        static readonly int s_TexProperty = Shader.PropertyToID("_Tex");

        /// <summary>
        /// Reads <see cref="RenderSettings"/> ambient lighting for glTF export.
        /// EXR equirectangular HDRIs assigned to a panoramic skybox are exported as raw float32 DDS sidecars.
        /// </summary>
        /// <returns>Gather result, or null when no environment should be exported.</returns>
        public static FoundationEnvironmentGatherResult Gather()
        {
            switch (RenderSettings.ambientMode)
            {
                case AmbientMode.Flat:
                    return CreateColorResult(RenderSettings.ambientLight, RenderSettings.ambientIntensity);
                case AmbientMode.Trilight:
                    return CreateColorResult(RenderSettings.ambientSkyColor, RenderSettings.ambientIntensity);
                case AmbientMode.Skybox:
                    return GatherSkybox();
                default:
                    return null;
            }
        }

        static FoundationEnvironmentGatherResult CreateColorResult(Color color, float strength)
        {
            var linear = color.linear;
            return new FoundationEnvironmentGatherResult
            {
                Environment = new FoundationEnvironment
                {
                    type = "color",
                    color = new[] { linear.r, linear.g, linear.b },
                    strength = strength
                }
            };
        }

        static FoundationEnvironmentGatherResult GatherSkybox()
        {
            var skybox = RenderSettings.skybox;
            UnityEngine.Texture texture = null;
            var strength = RenderSettings.ambientIntensity;
            var azimuthOffset = 0f;

            if (skybox != null)
            {
                TryGetSkyboxHdriTexture(skybox, out texture);

                if (skybox.HasProperty("_Exposure"))
                {
                    strength *= skybox.GetFloat("_Exposure");
                }

                if (skybox.HasProperty("_Rotation"))
                {
                    azimuthOffset = skybox.GetFloat("_Rotation");
                }
            }

            if (texture == null)
            {
                texture = FoundationEnvironmentFallback.LoadDefaultSkyboxHdri?.Invoke();
                if (texture != null)
                {
                    Debug.LogWarning(
                        "EXT_foundation_environment: Skybox mode has no HDRI texture; " +
                        "exporting baked default skybox HDRI fallback.");
                }
                else if (skybox == null)
                {
                    Debug.LogWarning(
                        "EXT_foundation_environment: Environment Source is Skybox but RenderSettings.skybox " +
                        "is not assigned and no default HDRI fallback is available.");
                    return CreateColorResult(Color.white, 1.0f);
                }
                else
                {
                    Debug.LogWarning(
                        $"EXT_foundation_environment: No HDRI texture on skybox material '{skybox.name}' " +
                        $"(shader: {skybox.shader?.name ?? "null"}) and no default HDRI fallback is available.");
                    return CreateColorResult(Color.white, 1.0f);
                }
            }

            return CreateHdriResult(texture, strength, azimuthOffset);
        }

        static FoundationEnvironmentGatherResult CreateHdriResult(
            UnityEngine.Texture texture,
            float strength,
            float azimuthOffset)
        {
            var fileName = BuildHdriFileName(texture);
            var environment = new FoundationEnvironment
            {
                type = "hdri",
                uri = fileName,
                projection = "longlat",
                strength = strength,
                azimuthOffset = azimuthOffset
            };
            return new FoundationEnvironmentGatherResult
            {
                Environment = environment,
                HdriSidecar = new FoundationEnvironmentHdriSidecar
                {
                    SourceTexture = texture,
                    FileName = fileName,
                    EnvironmentPayload = environment
                }
            };
        }

        static bool TryGetSkyboxHdriTexture(UnityEngine.Material skybox, out UnityEngine.Texture texture)
        {
            texture = null;
            if (skybox == null)
            {
                return false;
            }

            foreach (var propertyId in new[] { s_MainTexProperty, s_TexProperty })
            {
                if (!skybox.HasProperty(propertyId))
                {
                    continue;
                }

                var candidate = skybox.GetTexture(propertyId);
                if (candidate != null)
                {
                    texture = candidate;
                    return true;
                }
            }

            return false;
        }

        static string BuildHdriFileName(UnityEngine.Texture texture)
        {
            var baseName = string.IsNullOrEmpty(texture.name) ? "environment" : texture.name;
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                baseName = baseName.Replace(invalid, '_');
            }

            return $"{baseName}_environment.dds";
        }
    }
}
