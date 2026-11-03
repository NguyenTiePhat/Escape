using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MicrophoneNoiseDetector : MonoBehaviour
{
    public const int SampleRate = 16000;
    public const float SampleInterval = .05f, EmitInterval = .2f;
    public const string DefaultPrefsPrefix = "M3.Microphone.";
    public string prefsPrefix = DefaultPrefsPrefix;
    public Transform player;
    public float noiseFloorDb = -50f, maxDb = -10f;
    [Range(.5f, 2f)] public float sensitivity = 1f;
    [Min(.001f)] public float attackSeconds = .035f, releaseSeconds = .35f;
    public bool detectionEnabled = true;
    public string selectedDevice = "";
    public bool SuppressEmission { get; set; }
    public float RawDb { get; private set; } = -120;
    public float SmoothedDb { get; private set; } = -120;
    public float Level { get; private set; }
    public bool IsCapturing { get; private set; }
    public string Status { get; private set; } = "Chưa khởi động";
    public string[] Devices { get; private set; } = Array.Empty<string>();
    public event Action<float, float> OnLevelChanged;
    public event Action<string> OnStatusChanged;
    public event Action<string[]> OnDevicesChanged;
    IMicrophoneInput input;
    Coroutine startup;
    AudioClip recording;
    string activeDevice;
    float[] samples;
    int lastPosition;
    float nextSample, nextDevices, lastProgress, nextEmit;
    bool starting, retryOnReconnect;

    void Awake() { if (!player) player = transform; if (input == null) input = new UnityMicrophoneInput(); LoadSettings(); }
    void OnEnable() { RefreshDevices(); if (detectionEnabled) StartCapture(); else SetStatus("Đã tắt phát hiện giọng nói"); }
    void OnDisable() { StopCapture(); }
    void OnApplicationQuit() { SaveSettings(); StopCapture(); }
    void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextDevices) { nextDevices = now + 1; RefreshDevices(); }
        if (IsCapturing && now >= nextSample) { nextSample = now + SampleInterval; PollCapture(now); }
    }
    // Only call while disabled. Used by automated tests and platform adapters.
    public void SetInputBackend(IMicrophoneInput backend)
    {
        if (isActiveAndEnabled) throw new InvalidOperationException("Disable detector before changing input backend.");
        input = backend ?? throw new ArgumentNullException(nameof(backend));
    }
    public void LoadSettings()
    {
        detectionEnabled = PlayerPrefs.GetInt(prefsPrefix + "enabled", detectionEnabled ? 1 : 0) != 0;
        selectedDevice = PlayerPrefs.GetString(prefsPrefix + "device", selectedDevice);
        noiseFloorDb = PlayerPrefs.GetFloat(prefsPrefix + "floor", noiseFloorDb);
        maxDb = PlayerPrefs.GetFloat(prefsPrefix + "max", maxDb);
        sensitivity = PlayerPrefs.GetFloat(prefsPrefix + "sensitivity", sensitivity);
        SanitizeSettings();
    }
    public void SaveSettings()
    {
        SanitizeSettings();
        PlayerPrefs.SetInt(prefsPrefix + "enabled", detectionEnabled ? 1 : 0);
        PlayerPrefs.SetString(prefsPrefix + "device", selectedDevice ?? "");
        PlayerPrefs.SetFloat(prefsPrefix + "floor", noiseFloorDb);
        PlayerPrefs.SetFloat(prefsPrefix + "max", maxDb);
        PlayerPrefs.SetFloat(prefsPrefix + "sensitivity", sensitivity);
        PlayerPrefs.Save();
    }
    void SanitizeSettings()
    {
        if (!float.IsFinite(noiseFloorDb)) noiseFloorDb = -50;
        if (!float.IsFinite(maxDb)) maxDb = -10;
        if (!float.IsFinite(sensitivity)) sensitivity = 1;
        maxDb = Mathf.Clamp(maxDb, -94, 0);
        noiseFloorDb = Mathf.Clamp(noiseFloorDb, -100, maxDb - 6);
        sensitivity = Mathf.Clamp(sensitivity, .5f, 2);
    }
    public void SetDetectionEnabled(bool value)
    {
        detectionEnabled = value; SaveSettings();
        if (value && isActiveAndEnabled) StartCapture(); else { StopCapture(); SetStatus("Đã tắt phát hiện giọng nói"); }
    }
    public void SelectDevice(string device)
    {
        selectedDevice = device ?? ""; SaveSettings();
        if (detectionEnabled && isActiveAndEnabled) StartCapture();
    }
    public void StartCapture()
    {
        StopCapture(); if (!detectionEnabled || !isActiveAndEnabled) return;
        if (input == null) input = new UnityMicrophoneInput();
        starting = true; startup = StartCoroutine(BeginCapture());
    }
    IEnumerator BeginCapture()
    {
        // Ensures startup has been assigned before this coroutine can finish synchronously.
        yield return null;
        bool permission;
        try { permission = input.HasPermission; }
        catch (Exception e) { Fail("Không kiểm tra được quyền micro: " + e.Message, false); yield break; }
        if (!permission)
        {
            SetStatus("Đang chờ quyền truy cập micro…");
            IEnumerator request;
            try { request = input.RequestPermission(); }
            catch (Exception e) { Fail("Không yêu cầu được quyền micro: " + e.Message, false); yield break; }
            // The OS permission coroutine owns its asynchronous callback.
            while (true)
            {
                bool moved; object wait;
                try { moved = request != null && request.MoveNext(); wait = moved ? request.Current : null; }
                catch (Exception e) { Fail("Lỗi yêu cầu quyền micro: " + e.Message, false); yield break; }
                if (!moved) break;
                yield return wait;
            }
            try { permission = input.HasPermission; }
            catch (Exception e) { Fail("Lỗi quyền micro: " + e.Message, false); yield break; }
            if (!permission) { Fail("Quyền micro bị từ chối. Hãy cấp quyền trong cài đặt hệ thống.", false); yield break; }
        }
        RefreshDevices();
        if (Devices.Length == 0) { Fail("Không có micro được kết nối.", true); yield break; }
        string device = string.IsNullOrEmpty(selectedDevice) ? Devices[0] : selectedDevice;
        if (Array.IndexOf(Devices, device) < 0) { Fail("Micro đã chọn không còn kết nối.", true); yield break; }
        try
        {
            activeDevice = device;
            recording = input.Start(device, true, 1, SampleRate);
            if (!recording) { Fail("Không thể bắt đầu thu micro. Kiểm tra quyền hệ thống hoặc thiết bị đang bận.", true); yield break; }
            samples = new float[Mathf.Max(1, recording.frequency / 20) * recording.channels];
            lastPosition = 0; nextSample = Time.unscaledTime + SampleInterval;
            lastProgress = Time.unscaledTime; nextEmit = Time.unscaledTime;
            IsCapturing = true; starting = false; startup = null; retryOnReconnect = false;
            SetStatus("Đang thu: " + device);
        }
        catch (Exception e) { Fail("Không thể bắt đầu micro: " + e.Message, true); }
    }
    public void RefreshDevices()
    {
        if (input == null) return;
        try
        {
            var devices = input.Devices ?? Array.Empty<string>();
            bool changed = devices.Length != Devices.Length;
            for (int i = 0; !changed && i < devices.Length; i++) changed = devices[i] != Devices[i];
            if (changed) { Devices = (string[])devices.Clone(); OnDevicesChanged?.Invoke(Devices); }
            if (IsCapturing && Array.IndexOf(Devices, activeDevice) < 0) { Fail("Micro đã bị ngắt kết nối.", true); return; }
            if (changed && retryOnReconnect && detectionEnabled && !IsCapturing && !starting && Devices.Length > 0 && (string.IsNullOrEmpty(selectedDevice) || Array.IndexOf(Devices, selectedDevice) >= 0)) StartCapture();
        }
        catch (Exception e) { Fail("Không đọc được danh sách micro: " + e.Message, true); }
    }
    public void PollCapture(float now)
    {
        if (!IsCapturing || !recording) return;
        try
        {
            int position = input.GetPosition(activeDevice);
            if (position < 0 || position >= recording.samples) { Fail("Micro không còn trả về dữ liệu hợp lệ.", true); return; }
            int freshFrames = (position - lastPosition + recording.samples) % recording.samples;
            if (!input.IsRecording(activeDevice) || freshFrames == 0)
            {
                if (now - lastProgress > 2f) Fail("Micro đã dừng hoặc không gửi dữ liệu.", true);
                return;
            }
            int frames = Mathf.Min(freshFrames, Mathf.Max(1, recording.frequency / 20));
            int size = frames * recording.channels;
            if (samples == null || samples.Length != size) samples = new float[size];
            int offset = (position - frames + recording.samples) % recording.samples;
            if (!input.Read(recording, samples, offset)) { Fail("Không đọc được mẫu âm thanh từ micro.", true); return; }
            float elapsed = Mathf.Clamp(now - lastProgress, .001f, .5f);
            lastPosition = position; lastProgress = now;
            ProcessSamples(samples, elapsed, now);
        }
        catch (Exception e) { Fail("Micro gặp lỗi hoặc đã bị rút: " + e.Message, true); }
    }
    public static float SamplesToDb(float[] values)
    {
        if (values == null || values.Length == 0) return -120;
        double square = 0;
        foreach (float value in values) if (float.IsFinite(value)) square += (double)value * value;
        return Mathf.Max(-120, 20f * Mathf.Log10(Mathf.Max(.000001f, (float)Math.Sqrt(square / values.Length))));
    }
    public static float NormalizeDb(float db, float floor, float ceiling, float gain) => Mathf.Clamp01((db - floor) / Mathf.Max(1, ceiling - floor) * Mathf.Clamp(gain, .5f, 2));
    public void ProcessSamples(float[] values, float elapsed, float now)
    {
        RawDb = SamplesToDb(values);
        float tau = RawDb > SmoothedDb ? attackSeconds : releaseSeconds;
        SmoothedDb = Mathf.Lerp(SmoothedDb, RawDb, 1 - Mathf.Exp(-Mathf.Max(0, elapsed) / Mathf.Max(.001f, tau)));
        Level = NormalizeDb(SmoothedDb, noiseFloorDb, maxDb, sensitivity);
        OnLevelChanged?.Invoke(RawDb, Level);
        if (!detectionEnabled || SuppressEmission || RawDb <= noiseFloorDb || Level <= 0 || now < nextEmit) return;
        nextEmit = now + EmitInterval;
        var origin = player ? player : transform;
        NoiseManager.EmitNoise(origin.position, Level, NoiseSource.Voice, origin.gameObject);
    }
    public void StopCapture()
    {
        if (startup != null) { StopCoroutine(startup); startup = null; }
        starting = false; ReleaseDevice(); ResetLevel();
    }
    void ReleaseDevice()
    {
        IsCapturing = false;
        if (!string.IsNullOrEmpty(activeDevice) && input != null)
        {
            try { input.End(activeDevice); }
            catch (Exception e) { Debug.LogWarning("[Microphone] Không thể đóng thiết bị: " + e.Message, this); }
        }
        activeDevice = null;
        if (recording) Destroy(recording);
        recording = null; samples = null;
    }
    void ResetLevel() { RawDb = SmoothedDb = -120; Level = 0; OnLevelChanged?.Invoke(RawDb, 0); }
    void Fail(string message, bool retry)
    {
        starting = false; startup = null; retryOnReconnect = retry;
        ReleaseDevice(); ResetLevel(); SetStatus(message);
        if (lastWarning != message) { lastWarning = message; Debug.LogWarning("[Microphone] " + message, this); }
    }
    string lastWarning;
    void SetStatus(string status) { Status = status; OnStatusChanged?.Invoke(status); }
}
