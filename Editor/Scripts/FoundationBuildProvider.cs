// SPDX-FileCopyrightText: 2025 Foundation and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_EDITOR_WIN

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GLTFast.Editor
{
    /// <summary>
    /// Editor progress bar shown during View in Foundation.
    /// Uses <see cref="EditorUtility.DisplayProgressBar"/> so status is visible during async work.
    /// </summary>
    sealed class FoundationProgressScope : IDisposable
    {
        const string k_Title = "View in Foundation";
        bool m_Disposed;

        public void Report(float progress, string info) =>
            EditorUtility.DisplayProgressBar(k_Title, info, Mathf.Clamp01(progress));

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            EditorUtility.ClearProgressBar();
            m_Disposed = true;
        }
    }

    /// <summary>
    /// Downloads and caches the Foundation editor nightly build for Windows.
    /// Uses GET (not HEAD) to read the response ETag and streams the archive on update.
    /// </summary>
    static class FoundationBuildProvider
    {
        const string k_BuildUrl = "https://nightly.link/mos9527/Foundation/workflows/build/dev/Foundation-win-x64.zip";
        const string k_EditorExeName = "Editor.exe";
        const string k_EtagFileName = "etag.txt";
        const string k_ZipFileName = "Foundation-win-x64.zip";

        static string InstallDir => Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            "Library",
            "gltfast",
            "Foundation"
        );

        /// <summary>
        /// Fixed glTF file name for View in Foundation exports (stable path for file-lock detection).
        /// </summary>
        public const string ExportedSceneFileName = "FoundationExportedScene.gltf";

        /// <summary>
        /// Fixed folder used for scene exports opened in Foundation.
        /// </summary>
        public static string ExportDirectory => Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            "Library",
            "gltfast",
            "FoundationExport"
        );

        static string EditorPath => Path.Combine(InstallDir, k_EditorExeName);
        static string EtagPath => Path.Combine(InstallDir, k_EtagFileName);
        static string ZipPath => Path.Combine(InstallDir, k_ZipFileName);

        /// <summary>
        /// Ensures a local Foundation editor build is present and up to date.
        /// </summary>
        /// <returns>Coroutine that completes with the full path to <c>Editor.exe</c>.</returns>
        public static IEnumerator EnsureBuild(
            Action<string> onComplete,
            Action<string> onError,
            Action<float, string> report = null,
            float progressMin = 0f,
            float progressMax = 1f)
        {
            void Report(float t, string message) =>
                report?.Invoke(Mathf.Lerp(progressMin, progressMax, t), message);

            Directory.CreateDirectory(InstallDir);
            Report(0f, "Checking Foundation build...");

            var etagTask = FetchEtagAsync(k_BuildUrl);
            while (!etagTask.IsCompleted)
            {
                yield return null;
            }

            if (etagTask.IsFaulted)
            {
                onError(etagTask.Exception!.GetBaseException().Message);
                yield break;
            }

            var remoteEtag = etagTask.Result;
            var cachedEtag = ReadCachedEtag();

            if (!string.IsNullOrEmpty(remoteEtag)
                && remoteEtag == cachedEtag
                && File.Exists(EditorPath))
            {
                Report(1f, "Using cached Foundation build");
                onComplete(EditorPath);
                yield break;
            }

            if (File.Exists(ZipPath))
            {
                File.Delete(ZipPath);
            }

            Report(0.1f, "Downloading Foundation build...");
            using (var request = UnityWebRequest.Get(k_BuildUrl))
            {
                request.downloadHandler = new DownloadHandlerFile(ZipPath);
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    var downloaded = request.downloadedBytes;
                    if (ulong.TryParse(request.GetResponseHeader("Content-Length"), out var total) && total > 0)
                    {
                        var downloadT = Mathf.Clamp01(downloaded / (float)total);
                        Report(
                            Mathf.Lerp(0.1f, 0.75f, downloadT),
                            $"Downloading Foundation build ({FormatBytes(downloaded)} / {FormatBytes(total)})..."
                        );
                    }
                    else
                    {
                        Report(
                            Mathf.Lerp(0.1f, 0.75f, operation.progress),
                            $"Downloading Foundation build ({FormatBytes(downloaded)})..."
                        );
                    }

                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError($"Failed to download Foundation build: {request.error} (HTTP {request.responseCode})");
                    yield break;
                }

                if (string.IsNullOrEmpty(remoteEtag))
                {
                    remoteEtag = request.GetResponseHeader("ETag")?.Trim('"');
                }

                if (File.Exists(ZipPath))
                {
                    Report(0.75f, $"Download complete ({FormatBytes((ulong)new FileInfo(ZipPath).Length)})");
                }
            }

            if (!File.Exists(ZipPath))
            {
                onError($"Download reported success but zip is missing at {ZipPath}");
                yield break;
            }

            try
            {
                Report(0.85f, "Extracting Foundation build...");
                ExtractBuild(ZipPath, InstallDir);
                if (!string.IsNullOrEmpty(remoteEtag))
                {
                    File.WriteAllText(EtagPath, remoteEtag);
                }
            }
            catch (Exception e)
            {
                onError($"Failed to extract Foundation build: {e.Message}");
                yield break;
            }
            finally
            {
                if (File.Exists(ZipPath))
                {
                    File.Delete(ZipPath);
                }
            }

            if (!File.Exists(EditorPath))
            {
                onError($"Foundation editor not found at {EditorPath}");
                yield break;
            }

            Report(1f, "Foundation build ready");
            onComplete(EditorPath);
        }

        public static Task<string> EnsureBuildAsync(
            Action<float, string> report = null,
            float progressMin = 0f,
            float progressMax = 1f)
        {
            var tcs = new TaskCompletionSource<string>();
            EditorCoroutine.Start(EnsureBuild(
                path => tcs.TrySetResult(path),
                error => tcs.TrySetException(new InvalidOperationException(error)),
                report,
                progressMin,
                progressMax
            ));
            return tcs.Task;
        }

        public static void PrepareExportDirectory()
        {
            if (Directory.Exists(ExportDirectory))
            {
                Directory.Delete(ExportDirectory, true);
            }
            Directory.CreateDirectory(ExportDirectory);
        }

        static async Task<string> FetchEtagAsync(string url)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response.Headers.ETag?.Tag?.Trim('"');
        }

        static string ReadCachedEtag()
        {
            return File.Exists(EtagPath) ? File.ReadAllText(EtagPath).Trim() : null;
        }

        static void ExtractBuild(string zipPath, string destinationDir)
        {
            var zipFullPath = Path.GetFullPath(zipPath);
            foreach (var file in Directory.GetFiles(destinationDir))
            {
                if (string.Equals(Path.GetFullPath(file), zipFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(destinationDir))
            {
                Directory.Delete(dir, true);
            }

            ZipFile.ExtractToDirectory(zipPath, destinationDir);
        }

        static string FormatBytes(ulong bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            if (bytes < 1024 * 1024)
            {
                return $"{bytes / 1024f:0.#} KB";
            }

            return $"{bytes / (1024f * 1024f):0.#} MB";
        }

        static class EditorCoroutine
        {
            public static void Start(IEnumerator routine)
            {
                EditorApplication.update += Tick;

                void Tick()
                {
                    try
                    {
                        if (!routine.MoveNext())
                        {
                            EditorApplication.update -= Tick;
                        }
                    }
                    catch (Exception)
                    {
                        EditorApplication.update -= Tick;
                        throw;
                    }
                }
            }
        }
    }
}

#endif
