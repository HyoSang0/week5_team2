using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityCI
{
    /// <summary>
    /// Command line build entry point for the Unity CI pipeline.
    /// Invoked with:
    ///   Unity.exe -batchmode -nographics -quit -projectPath &lt;proj&gt; -buildTarget Win64
    ///     -executeMethod UnityCI.BuildScript.Build
    ///     -ciOutput &lt;abs path to exe&gt; -ciVersion &lt;version&gt;
    ///     -ciVersionLabel &lt;label&gt; -ciDevelopment &lt;true|false&gt;
    /// Compiles on Unity 2021.3 through Unity 6 (6000.x).
    /// </summary>
    public static class BuildScript
    {
        public static void Build()
        {
            try
            {
                bool succeeded = RunBuild();
                EditorApplication.Exit(succeeded ? 0 : 1);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static bool RunBuild()
        {
            // ---- Parse command line arguments (value follows the flag) ----
            string[] args = Environment.GetCommandLineArgs();
            string ciOutput = null;
            string ciVersion = null;
            string ciVersionLabel = null;
            string ciDevelopmentRaw = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-ciOutput": ciOutput = args[i + 1]; break;
                    case "-ciVersion": ciVersion = args[i + 1]; break;
                    case "-ciVersionLabel": ciVersionLabel = args[i + 1]; break;
                    case "-ciDevelopment": ciDevelopmentRaw = args[i + 1]; break;
                }
            }

            bool development = string.Equals(ciDevelopmentRaw, "true",
                StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(ciOutput))
            {
                Debug.LogError("[UnityCI] Missing required argument -ciOutput " +
                    "(absolute path of the executable to build).");
                return false;
            }

            // ---- Collect enabled scenes ----
            string[] allScenesPaths;
            {
                var allScenes = EditorBuildSettings.scenes;
                var list = new System.Collections.Generic.List<string>();
                if (allScenes != null)
                {
                    for (int i = 0; i < allScenes.Length; i++)
                    {
                        if (allScenes[i] != null && allScenes[i].enabled &&
                            !string.IsNullOrEmpty(allScenes[i].path))
                        {
                            list.Add(allScenes[i].path);
                        }
                    }
                }
                allScenesPaths = list.ToArray();
            }

            if (allScenesPaths.Length == 0)
            {
                Debug.LogError("[UnityCI] No enabled scenes found in " +
                    "EditorBuildSettings (File > Build Settings). " +
                    "Enable at least one scene and commit it.");
                return false;
            }

            // ---- Temporarily override the bundle version, restore in finally ----
            string originalVersion = PlayerSettings.bundleVersion;
            bool versionOverridden = false;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (!string.IsNullOrEmpty(ciVersion))
                {
                    PlayerSettings.bundleVersion = ciVersion;
                    versionOverridden = true;
                }

                // ---- Make sure the output directory exists ----
                string outputDir = Path.GetDirectoryName(ciOutput);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // ---- Build ----
                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = allScenesPaths;
                options.locationPathName = ciOutput;
                options.target = BuildTarget.StandaloneWindows64;
                options.targetGroup = BuildTargetGroup.Standalone;
                options.options = development ? BuildOptions.Development : BuildOptions.None;

                Debug.Log("[UnityCI] Starting build. Output: " + ciOutput +
                    " | Version: " + (string.IsNullOrEmpty(ciVersion) ? "(unchanged)" : ciVersion) +
                    " | Label: " + (string.IsNullOrEmpty(ciVersionLabel) ? "(none)" : ciVersionLabel) +
                    " | Development: " + development +
                    " | Scenes: " + allScenesPaths.Length);

                BuildReport report = BuildPipeline.BuildPlayer(options);
                stopwatch.Stop();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    WriteBuildInfo(ciOutput, ciVersion, ciVersionLabel);

                    long fileSize = -1;
                    try
                    {
                        if (File.Exists(ciOutput))
                        {
                            fileSize = new FileInfo(ciOutput).Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[UnityCI] Could not read output file size: " + ex.Message);
                    }

                    Debug.Log("[UnityCI] Build SUCCEEDED." +
                        " Size: " + (fileSize >= 0 ? (fileSize / (1024.0 * 1024.0)).ToString("F1",
                            CultureInfo.InvariantCulture) + " MB" : "unknown") +
                        " | Duration: " +
                        stopwatch.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) +
                        "s | Errors: " + report.summary.totalErrors +
                        " | Warnings: " + report.summary.totalWarnings);
                    return true;
                }
                else
                {
                    Debug.LogError("[UnityCI] Build FAILED with result: " + report.summary.result +
                        " | Errors: " + report.summary.totalErrors +
                        " | Warnings: " + report.summary.totalWarnings +
                        " | Duration: " +
                        stopwatch.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + "s");
                    return false;
                }
            }
            finally
            {
                if (versionOverridden)
                {
                    PlayerSettings.bundleVersion = originalVersion;
                }
            }
        }

        private static void WriteBuildInfo(string exePath, string version, string versionLabel)
        {
            string dir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(dir))
            {
                dir = ".";
            }
            string infoPath = Path.Combine(dir, "build_info.json");

            string unityVersion = Application.unityVersion;
            string builtAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ",
                CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"version\": ").Append(JsonString(version)).Append(",\n");
            sb.Append("  \"versionLabel\": ").Append(JsonString(versionLabel)).Append(",\n");
            sb.Append("  \"builtAt\": ").Append(JsonString(builtAt)).Append(",\n");
            sb.Append("  \"unityVersion\": ").Append(JsonString(unityVersion)).Append("\n");
            sb.Append("}\n");

            File.WriteAllText(infoPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[UnityCI] Wrote " + infoPath);
        }

        /// <summary>
        /// Minimal JSON string encoder (no external JSON libraries).
        /// Escapes backslash, double quote and control characters.
        /// </summary>
        private static string JsonString(string value)
        {
            if (value == null)
            {
                return "null";
            }
            StringBuilder sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
