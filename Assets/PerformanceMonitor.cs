using UnityEngine;
using System.Diagnostics;
using Unity.Profiling;
using Fusion;

public class PerformanceMonitor : MonoBehaviour
{
    private GUIStyle style;
    private int frameCount = 0;
    private float elapsedTime = 0f;
    private float currentFPS = 0f;

    private Process currentProcess;
    private float lastCpuTime;
    private float currentCpuUsage = 0f;

    private float currentGpuFrameTime = 0f;
    private float gpuUsageEstimate = 0f;

    private int currentPing = 0;

    public float updateInterval = 0.5f;
    public float targetFrameRate = 60f;

    private FrameTiming[] frameTimings = new FrameTiming[1];

    void Start()
    {
        style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        currentProcess = Process.GetCurrentProcess();
        lastCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;

        if (targetFrameRate <= 0) targetFrameRate = 60f;
    }

    void Update()
    {
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;
        if (elapsedTime >= updateInterval)
        {
            currentFPS = frameCount / elapsedTime;
            frameCount = 0;
            elapsedTime = 0f;

            UpdateCpuUsage();
            UpdateGpuFrameTime();

            float frameTimeBudgetMs = 1000f / targetFrameRate;
            gpuUsageEstimate = Mathf.Clamp01(currentGpuFrameTime / frameTimeBudgetMs) * 100f;

            UpdateFusionPing();
        }
    }

    void UpdateCpuUsage()
    {
        float newCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;
        float cpuTimeDelta = newCpuTime - lastCpuTime;
        float intervalMs = updateInterval * 1000f;

        currentCpuUsage = (cpuTimeDelta / intervalMs) * 100f / SystemInfo.processorCount;
        lastCpuTime = newCpuTime;
    }

    void UpdateGpuFrameTime()
    {
        FrameTimingManager.CaptureFrameTimings();
        uint framesRetrieved = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (framesRetrieved > 0)
        {
            currentGpuFrameTime = (float)frameTimings[0].gpuFrameTime;
        }
        else
        {
            currentGpuFrameTime = 0f;
        }
    }

    void UpdateFusionPing()
    {
        if (NetworkRunner.Instances.Count > 0)
        {
            var runner = NetworkRunner.Instances[0];
            if (runner != null && runner.IsRunning && runner.LocalPlayer != PlayerRef.None)
            {
                double rttSec = runner.GetPlayerRtt(runner.LocalPlayer);
                currentPing = Mathf.RoundToInt((float)(rttSec * 1000)); // ×ª»»³ÉºÁÃë
            }
            else
            {
                currentPing = -1;
            }
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 320, 160), "");

        GUI.Label(new Rect(20, 20, 300, 30), "FPS: " + currentFPS.ToString("0.0"), style);
        GUI.Label(new Rect(20, 50, 300, 30), "CPU: " + currentCpuUsage.ToString("0.0") + "%", style);
        GUI.Label(new Rect(20, 80, 300, 30), "GPU Time: " + currentGpuFrameTime.ToString("0.00") + " ms", style);
        GUI.Label(new Rect(20, 110, 300, 30), "GPU Use (Est.): " + gpuUsageEstimate.ToString("0.0") + "%", style);
        GUI.Label(new Rect(20, 140, 300, 30), "Fusion Ping: " + currentPing.ToString() + " ms", style);
    }
}
