using System;
using System.Collections.Generic;
using UnityEngine;

public static class MicCalibration
{
    public const float NormalVoiceTarget = .6f;
    public static bool TryCalculate(IList<float> silence, IList<float> speech, out float floor, out float max, out float sensitivity)
    {
        floor = -50; max = -10; sensitivity = 1;
        if (silence == null || speech == null || silence.Count < 20 || speech.Count < 20) return false;
        float background = Percentile(silence, .9f);
        float normal = Percentile(speech, .7f);
        if (!float.IsFinite(background) || !float.IsFinite(normal)) return false;
        floor = Mathf.Clamp(background + 6f, -100, -6);
        if (normal <= floor + 3 || background >= -12) return false;
        max = Mathf.Clamp(floor + (normal - floor) / NormalVoiceTarget, floor + 6, 0);
        sensitivity = Mathf.Clamp(NormalVoiceTarget / Mathf.Max(.001f, MicrophoneNoiseDetector.NormalizeDb(normal, floor, max, 1)), .5f, 2);
        return true;
    }
    static float Percentile(IList<float> values, float percentile)
    {
        var copy = new List<float>(values); copy.Sort();
        return copy[Mathf.Clamp(Mathf.RoundToInt((copy.Count - 1) * percentile), 0, copy.Count - 1)];
    }
}
