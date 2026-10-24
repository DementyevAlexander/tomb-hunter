using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TombHunter.EditorTools
{
    // Сборка сцены и настроек проекта. Сцена почти пустая: камера и объект Game, весь интерфейс строится из кода.
    [InitializeOnLoad]
    public static class TombSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        static TombSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) return;
                if (!File.Exists(ScenePath)) RebuildScene();
            };
        }

        [MenuItem("Tomb/Setup/Rebuild Main Scene")]
        public static void RebuildScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.416f, 0.31f, 0.447f);
            camGo.transform.position = new Vector3(0, 0, -10);
            new GameObject("Game", typeof(Game));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ApplyProjectSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Tomb: сцена собрана — " + ScenePath);
        }

        public static void ApplyProjectSettings()
        {
            PlayerSettings.productName = "Охотница за гробницами";
            PlayerSettings.companyName = "TombHunter";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultIsNativeResolution = false;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        }

        // Точка входа для командной строки: -executeMethod TombHunter.EditorTools.TombSetup.BatchSetup
        public static void BatchSetup()
        {
            RebuildScene();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tomb/Reset Save")]
        public static void ResetSave()
        {
            MetaLogic.Wipe();
            Debug.Log("Tomb: сохранение стёрто.");
        }

        [MenuItem("Tomb/Build/macOS")]
        public static void BuildMac()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/macOS/TombHunter.app",
                target = BuildTarget.StandaloneOSX,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Tomb: сборка — " + report.summary.result);
        }

        // Точка входа для командной строки: -executeMethod TombHunter.EditorTools.TombSetup.BuildWindows
        [MenuItem("Tomb/Build/Windows")]
        public static void BuildWindows()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/TombHunter.exe",
                target = BuildTarget.StandaloneWindows64,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("Tomb: сборка — " + report.summary.result);
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
