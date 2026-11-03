using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-150), DisallowMultipleComponent, RequireComponent(typeof(MicrophoneNoiseDetector))]
public sealed class MicSettingsPanel : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    public MicrophoneNoiseDetector detector;
    public Canvas noiseHud;
    bool previousHudEnabled;
    string[] devices = System.Array.Empty<string>();
    bool dropdown, dirty;
    float saveAt, level, rawDb = -120, stageEnds;
    string status, calibrationMessage = "";
    int stage;
    readonly List<float> silence = new List<float>(), speech = new List<float>();
    Coroutine calibration;
    CursorLockMode previousCursor;
    bool previousVisible;
    Vector2 deviceScroll;
    void Awake() { if (!detector) detector = GetComponent<MicrophoneNoiseDetector>(); }
    void OnEnable()
    {
        if (!detector) detector = GetComponent<MicrophoneNoiseDetector>();
        detector.OnLevelChanged += ShowLevel; detector.OnDevicesChanged += ShowDevices; detector.OnStatusChanged += ShowStatus;
        devices = detector.Devices; status = detector.Status;
    }
    void OnDisable()
    {
        CancelCalibration(); Close();
        if (dirty && detector) { detector.SaveSettings(); dirty = false; }
        if (detector) { detector.OnLevelChanged -= ShowLevel; detector.OnDevicesChanged -= ShowDevices; detector.OnStatusChanged -= ShowStatus; }
    }
    void ShowLevel(float db, float value)
    {
        rawDb = db; level = value;
        if (stage == 1) silence.Add(db); else if (stage == 2) speech.Add(db);
    }
    void ShowDevices(string[] value) { devices = value; }
    void ShowStatus(string value) { status = value; if (stage != 0 && !detector.IsCapturing) { CancelCalibration(); calibrationMessage = "Hiệu chỉnh đã dừng: micro không còn thu."; } }
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f2Key.wasPressedThisFrame) { if (IsOpen) Close(); else Open(); }
        else if (IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
        if (dirty && Time.unscaledTime >= saveAt) { detector.SaveSettings(); dirty = false; }
    }
    public void Open()
    {
        var room = FindAnyObjectByType<StartRoom>(); if (room && room.Reading) return;
        if (IsOpen) return;
        previousCursor = Cursor.lockState; previousVisible = Cursor.visible;
        if (noiseHud) { previousHudEnabled = noiseHud.enabled; noiseHud.enabled = false; }
        IsOpen = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        detector.RefreshDevices();
    }
    public void Close()
    {
        if (!IsOpen) return;
        CancelCalibration(); dropdown = false; IsOpen = false;
        Cursor.lockState = previousCursor; Cursor.visible = previousVisible;
        if (noiseHud) noiseHud.enabled = previousHudEnabled;
        if (dirty) { detector.SaveSettings(); dirty = false; }
    }
    public void BeginCalibration()
    {
        if (!detector.IsCapturing || !detector.detectionEnabled) { calibrationMessage = "Bật micro và kiểm tra quyền trước khi hiệu chỉnh."; return; }
        CancelCalibration(); silence.Clear(); speech.Clear();
        detector.SuppressEmission = true; calibration = StartCoroutine(Calibrate());
    }
    IEnumerator Calibrate()
    {
        stage = 1; stageEnds = Time.unscaledTime + 3;
        yield return new WaitForSecondsRealtime(3);
        if (!detector.IsCapturing) { CancelCalibration(); yield break; }
        stage = 2; stageEnds = Time.unscaledTime + 3;
        yield return new WaitForSecondsRealtime(3);
        stage = 0; calibration = null; detector.SuppressEmission = false;
        if (MicCalibration.TryCalculate(silence, speech, out float floor, out float max, out float sensitivity))
        {
            detector.noiseFloorDb = floor; detector.maxDb = max; detector.sensitivity = sensitivity; detector.SaveSettings();
            calibrationMessage = "Đã lưu hiệu chỉnh. Giọng bình thường đặt khoảng 60%.";
        }
        else calibrationMessage = "Chưa nghe rõ giọng so với tiếng nền. Giữ cài đặt cũ; hãy thử lại.";
    }
    void CancelCalibration()
    {
        if (calibration != null) { StopCoroutine(calibration); calibration = null; }
        stage = 0; if (detector) detector.SuppressEmission = false;
    }
    void OnGUI()
    {
        if (!IsOpen || !detector) return;
        float width = Mathf.Min(540, Screen.width - 24), height = Mathf.Min(470, Screen.height - 24);
        Rect panel = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
        GUI.depth = -1000;
        var panelColor = GUI.color; GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = new Color(.07f, .08f, .10f, .98f); GUI.DrawTexture(panel, Texture2D.whiteTexture); GUI.color = panelColor;
        GUI.Box(panel, "Cài đặt micro [F2 / Esc]");
        GUILayout.BeginArea(new Rect(panel.x + 20, panel.y + 30, width - 40, height - 45));
        GUILayout.Label("Micro thu âm");
        GUI.enabled = stage == 0;
        string current = string.IsNullOrEmpty(detector.selectedDevice) ? "Mặc định hệ thống" : detector.selectedDevice;
        if (GUILayout.Button(current + "  ▼")) dropdown = !dropdown;
        if (dropdown)
        {
            deviceScroll = GUILayout.BeginScrollView(deviceScroll, GUILayout.Height(95));
            if (GUILayout.Button("Mặc định hệ thống")) { detector.SelectDevice(""); dropdown = false; }
            foreach (string device in devices) if (GUILayout.Button(device)) { detector.SelectDevice(device); dropdown = false; }
            GUILayout.EndScrollView();
        }
        GUI.enabled = true;
        GUILayout.Label(status ?? "");
        GUILayout.Label($"Mức vào: {rawDb:F1} dBFS  •  {level:P0}");
        var bar = GUILayoutUtility.GetRect(100, 18, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(bar, Texture2D.grayTexture);
        var color = GUI.color; GUI.color = level < .65f ? new Color(.3f, .8f, .45f) : new Color(1, .35f, .2f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * level, bar.height), Texture2D.whiteTexture); GUI.color = color;
        GUILayout.Space(8); GUILayout.Label($"Độ nhạy: {detector.sensitivity:F2}");
        GUI.enabled = stage == 0;
        float sensitivity = GUILayout.HorizontalSlider(detector.sensitivity, .5f, 2);
        if (Mathf.Abs(sensitivity - detector.sensitivity) > .0001f) { detector.sensitivity = sensitivity; dirty = true; saveAt = Time.unscaledTime + .5f; }
        GUILayout.Label($"Ngưỡng nền: {detector.noiseFloorDb:F1} dBFS  •  Mức tối đa: {detector.maxDb:F1} dBFS");
        if (GUILayout.Button("Hiệu chỉnh micro (3 giây im lặng + 3 giây nói)")) BeginCalibration();
        GUI.enabled = true;
        if (stage != 0) GUILayout.Label((stage == 1 ? "Giữ im lặng để đo tiếng nền: " : "Hãy nói với âm lượng bình thường: ") + Mathf.CeilToInt(Mathf.Max(0, stageEnds - Time.unscaledTime)) + " giây");
        else GUILayout.Label(calibrationMessage);
        if (GUILayout.Button(detector.detectionEnabled ? "Tắt phát hiện giọng nói" : "Bật phát hiện giọng nói")) { CancelCalibration(); detector.SetDetectionEnabled(!detector.detectionEnabled); }
        GUILayout.Space(6); if (GUILayout.Button("Đóng")) Close();
        GUILayout.EndArea(); GUI.enabled = true;
    }
}
