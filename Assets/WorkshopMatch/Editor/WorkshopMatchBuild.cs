using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    public static class WorkshopMatchBuild
    {
        private const string DefaultWebGlOutput = "Build/WebGL-Release-1.0.1";
        private const string ResponsiveShell = @"<meta name='viewport' content='width=device-width, initial-scale=1, viewport-fit=cover'>
<style id='matchworks-responsive-shell'>
html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#111923}
#unity-container{position:fixed!important;inset:0!important;transform:none!important;width:100%!important;height:100%!important}
#unity-canvas{display:block;width:100%!important;height:100%!important}
#unity-footer{display:none!important}
</style>";

        [MenuItem("Workshop Match/Build/WebGL Production Draft")]
        public static void BuildWebGL() => BuildWebGL(false);

        [MenuItem("Workshop Match/Build/WebGL Startup Test")]
        public static void BuildWebGLStartupTest() => BuildWebGL(true);

        private static void BuildWebGL(bool startupTest)
        {
            string output = ReadCommandLineValue("-buildOutput") ?? (startupTest?"Build/WebGL-StartupTest":DefaultWebGlOutput);
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("WebGL build için etkin sahne bulunamadı.");

            PlayerSettings.companyName = "Ali Portfolio";
            PlayerSettings.productName = "Matchworks";
            PlayerSettings.bundleVersion = "1.0.1";
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = startupTest ? WebGLCompressionFormat.Disabled : WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = !startupTest;
            PlayerSettings.WebGL.threadsSupport = false;
            EditorUserBuildSettings.development = startupTest;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = startupTest?BuildOptions.Development:BuildOptions.None
            });

            BuildSummary summary = report.summary;
            string result = $"T10_WEBGL_BUILD result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} bytes={summary.totalSize} output={output}";
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(result);
            ApplyResponsiveShell(output);
            Debug.Log(result);
        }

        private static void ApplyResponsiveShell(string output)
        {
            string index=Path.Combine(output,"index.html");
            string html=File.ReadAllText(index);
            if(html.Contains("matchworks-responsive-shell"))return;
            File.WriteAllText(index,html.Replace("</head>",ResponsiveShell+"\n</head>"));
        }

        private static string ReadCommandLineValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}
