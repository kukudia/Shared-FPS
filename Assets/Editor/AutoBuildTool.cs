using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System;

public class AutoBuildTool
{
    [MenuItem("Build/Build Windows (Auto Version + Zip)")]
    public static void BuildWindows()
    {
        // === 1. �Զ��汾�� ===
        string version = PlayerSettings.bundleVersion;
        string newVersion = IncrementVersion(version);
        PlayerSettings.bundleVersion = newVersion;

        string buildName = Application.productName;
        string buildFolder = "Builds";
        string exeName = $"{buildName}_v{newVersion}.exe";
        string targetDir = Path.Combine(buildFolder, $"{buildName}_v{newVersion}_Win64");
        string zipPath = Path.Combine(buildFolder, $"{buildName}_v{newVersion}_Win64.zip");

        if (!Directory.Exists(buildFolder))
            Directory.CreateDirectory(buildFolder);

        // === 2. ��� ===
        string[] scenes = GetEnabledScenes();
        BuildPipeline.BuildPlayer(scenes, Path.Combine(targetDir, exeName), BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"Build ���: {targetDir}");

        // === 3. ������־�ļ� ===
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);
        string logPath = Path.Combine(targetDir, "BuildInfo.txt");
        File.WriteAllText(logPath,
            $"Game: {buildName}\n" +
            $"Version: {newVersion}\n" +
            $"Unity: {Application.unityVersion}\n" +
            $"Date: {DateTime.Now}\n" +
            $"Platform: Windows x64\n");

        // === 4. ѹ�� zip ===
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(targetDir, zipPath);
        Debug.Log($"������ѹ����: {zipPath}");

        // === 5. ��Ŀ¼ ===
        EditorUtility.RevealInFinder(zipPath);
    }

    // �Զ������汾�ţ�Ĭ�����һλ��
    private static string IncrementVersion(string version)
    {
        string[] parts = version.Split('.');
        if (parts.Length < 3) parts = new string[] { "1", "0", "0" };

        int patch = int.Parse(parts[parts.Length - 1]);
        patch++;
        parts[parts.Length - 1] = patch.ToString();

        return string.Join(".", parts);
    }

    // ��ȡ��ѡ�ĳ���
    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        string[] enabledScenes = new string[scenes.Length];
        int index = 0;
        foreach (var scene in scenes)
        {
            if (scene.enabled)
                enabledScenes[index++] = scene.path;
        }
        System.Array.Resize(ref enabledScenes, index);
        return enabledScenes;
    }
}
