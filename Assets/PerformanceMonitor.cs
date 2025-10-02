using UnityEngine;
using System.Diagnostics;
using Unity.Profiling;
using System.Collections;

public class PerformanceMonitor : MonoBehaviour
{
    // GUI��ʽ
    private GUIStyle style;
    // ֡�ʼ������
    private int frameCount = 0;
    private float elapsedTime = 0f;
    private float currentFPS = 0f;
    // CPUʹ���ʱ���
    private Process currentProcess;
    private float lastCpuTime;
    private float currentCpuUsage = 0f;
    // GPU��ر���
    private float currentGpuFrameTime = 0f; // GPU֡ʱ�䣨���룩
    private float gpuUsageEstimate = 0f; // ����֡ʱ��Ԥ��Ĺ���ʹ����
    // ˢ�¼��
    public float updateInterval = 0.5f;
    // Ŀ��֡�ʣ����ڹ���GPUʹ���ʣ�
    public float targetFrameRate = 60f;

    // ʹ��FrameTimingManager��ȡ֡ʱ������
    private FrameTiming[] frameTimings = new FrameTiming[1];

    void Start()
    {
        // ��ʼ��GUI��ʽ
        style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        // ��ʼ�����̶�������CPU��أ�
        currentProcess = Process.GetCurrentProcess();
        lastCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;

        // ����Ŀ��֡�ʣ������Ŀ������δ���ƣ���ʹ��Ĭ��ֵ��
        if (targetFrameRate <= 0) targetFrameRate = 60f;
    }

    void Update()
    {
        // ����֡��
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;
        if (elapsedTime >= updateInterval)
        {
            currentFPS = frameCount / elapsedTime;
            frameCount = 0;
            elapsedTime = 0f;

            // ����CPUʹ����
            UpdateCpuUsage();

            // ����GPU֡ʱ�䣨ʹ��FrameTimingManager��
            UpdateGpuFrameTime();

            // ����֡ʱ��Ԥ�����GPUʹ���ʣ�����һ�ֽ��Ʒ�����
            // ����Ŀ��60FPSʱ��һ֡Ԥ��ʱ��Ϊ16.67ms�����GPU��Ⱦһ֡��ʱ8.33ms�������ʹ����ԼΪ50%��
            float frameTimeBudgetMs = 1000f / targetFrameRate; // ����Ŀ��֡ʱ�䣨���룩
            gpuUsageEstimate = Mathf.Clamp01(currentGpuFrameTime / frameTimeBudgetMs) * 100f;
        }
    }

    void UpdateCpuUsage()
    {
        float newCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;
        float cpuTimeDelta = newCpuTime - lastCpuTime;
        float intervalMs = updateInterval * 1000f;

        // ����CPUʹ���ʣ���������������
        currentCpuUsage = (cpuTimeDelta / intervalMs) * 100f / SystemInfo.processorCount;
        lastCpuTime = newCpuTime;
    }

    void UpdateGpuFrameTime()
    {
        // ʹ��FrameTimingManager��ȡ֡ʱ����Ϣ
        FrameTimingManager.CaptureFrameTimings();
        uint framesRetrieved = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (framesRetrieved > 0)
        {
            // gpuFrameTime �ĵ�λ�Ǻ���
            currentGpuFrameTime = (float)frameTimings[0].gpuFrameTime;
        }
        else
        {
            currentGpuFrameTime = 0f;
        }
    }

    void OnGUI()
    {
        // ������͸������
        GUI.Box(new Rect(10, 10, 300, 130), "");

        // ��ʾ�������
        GUI.Label(new Rect(20, 20, 280, 30), $"FPS: {currentFPS:0.0}", style);
        GUI.Label(new Rect(20, 50, 280, 30), $"CPU: {currentCpuUsage:0.0}%", style);
        // ��ʾGPU֡ʱ��
        GUI.Label(new Rect(20, 80, 280, 30), $"GPU Time: {currentGpuFrameTime:0.00} ms", style);
        // ��ʾ�����GPUʹ���ʣ�ע���ǹ��㣩
        GUI.Label(new Rect(20, 110, 280, 30), $"GPU Use (Est.): {gpuUsageEstimate:0.0}%", style);
    }
}