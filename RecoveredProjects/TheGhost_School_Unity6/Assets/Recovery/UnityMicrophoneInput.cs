using System.Collections;
using UnityEngine;

// Small adapter lets device loss and denied permission be tested without recording a real mic.
public interface IMicrophoneInput
{
    bool HasPermission { get; }
    IEnumerator RequestPermission();
    string[] Devices { get; }
    AudioClip Start(string device, bool loop, int lengthSeconds, int frequency);
    void End(string device);
    bool IsRecording(string device);
    int GetPosition(string device);
    bool Read(AudioClip clip, float[] samples, int offsetFrames);
}

public sealed class UnityMicrophoneInput : IMicrophoneInput
{
    public bool HasPermission
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
#else
            return Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
        }
    }
    public IEnumerator RequestPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool completed = false;
        var callbacks = new UnityEngine.Android.PermissionCallbacks();
        callbacks.PermissionGranted += _ => completed = true;
        callbacks.PermissionDenied += _ => completed = true;
        UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
        while (!completed) yield return null;
#else
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
#endif
    }
    public string[] Devices => Microphone.devices;
    public AudioClip Start(string device, bool loop, int lengthSeconds, int frequency) => Microphone.Start(device, loop, lengthSeconds, frequency);
    public void End(string device) => Microphone.End(device);
    public bool IsRecording(string device) => Microphone.IsRecording(device);
    public int GetPosition(string device) => Microphone.GetPosition(device);
    public bool Read(AudioClip clip, float[] samples, int offsetFrames) => clip.GetData(samples, offsetFrames);
}
