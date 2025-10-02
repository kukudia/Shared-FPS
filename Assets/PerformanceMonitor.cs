using UnityEngine;
using System.Diagnostics;
using Unity.Profiling;
using System.Collections;

public class PerformanceMonitor : MonoBehaviour
{
    // GUI样式
    private GUIStyle style;
    // 帧率计算变量
    private int frameCount = 0;
    private float elapsedTime = 0f;
    private float currentFPS = 0f;
    // CPU使用率变量
    private Process currentProcess;
    private float lastCpuTime;
    private float currentCpuUsage = 0f;
    // GPU相关变量
    private float currentGpuFrameTime = 0f; // GPU帧时间（毫秒）
    private float gpuUsageEstimate = 0f; // 基于帧时间预算的估算使用率
    // 刷新间隔
    public float updateInterval = 0.5f;
    // 目标帧率（用于估算GPU使用率）
    public float targetFrameRate = 60f;

    // 使用FrameTimingManager获取帧时间数据
    private FrameTiming[] frameTimings = new FrameTiming[1];

    void Start()
    {
        // 初始化GUI样式
        style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        // 初始化进程对象（用于CPU监控）
        currentProcess = Process.GetCurrentProcess();
        lastCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;

        // 设置目标帧率（如果项目设置中未限制，则使用默认值）
        if (targetFrameRate <= 0) targetFrameRate = 60f;
    }

    void Update()
    {
        // 计算帧率
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;
        if (elapsedTime >= updateInterval)
        {
            currentFPS = frameCount / elapsedTime;
            frameCount = 0;
            elapsedTime = 0f;

            // 更新CPU使用率
            UpdateCpuUsage();

            // 更新GPU帧时间（使用FrameTimingManager）
            UpdateGpuFrameTime();

            // 基于帧时间预算估算GPU使用率（这是一种近似方法）
            // 假设目标60FPS时，一帧预算时间为16.67ms。如果GPU渲染一帧耗时8.33ms，则估算使用率约为50%。
            float frameTimeBudgetMs = 1000f / targetFrameRate; // 计算目标帧时间（毫秒）
            gpuUsageEstimate = Mathf.Clamp01(currentGpuFrameTime / frameTimeBudgetMs) * 100f;
        }
    }

    void UpdateCpuUsage()
    {
        float newCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;
        float cpuTimeDelta = newCpuTime - lastCpuTime;
        float intervalMs = updateInterval * 1000f;

        // 计算CPU使用率（按核心数调整）
        currentCpuUsage = (cpuTimeDelta / intervalMs) * 100f / SystemInfo.processorCount;
        lastCpuTime = newCpuTime;
    }

    void UpdateGpuFrameTime()
    {
        // 使用FrameTimingManager获取帧时间信息
        FrameTimingManager.CaptureFrameTimings();
        uint framesRetrieved = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (framesRetrieved > 0)
        {
            // gpuFrameTime 的单位是毫秒
            currentGpuFrameTime = (float)frameTimings[0].gpuFrameTime;
        }
        else
        {
            currentGpuFrameTime = 0f;
        }
    }

    void OnGUI()
    {
        // 创建半透明背景
        GUI.Box(new Rect(10, 10, 300, 130), "");

        // 显示监控数据
        GUI.Label(new Rect(20, 20, 280, 30), $"FPS: {currentFPS:0.0}", style);
        GUI.Label(new Rect(20, 50, 280, 30), $"CPU: {currentCpuUsage:0.0}%", style);
        // 显示GPU帧时间
        GUI.Label(new Rect(20, 80, 280, 30), $"GPU Time: {currentGpuFrameTime:0.00} ms", style);
        // 显示估算的GPU使用率（注明是估算）
        GUI.Label(new Rect(20, 110, 280, 30), $"GPU Use (Est.): {gpuUsageEstimate:0.0}%", style);
    }
}