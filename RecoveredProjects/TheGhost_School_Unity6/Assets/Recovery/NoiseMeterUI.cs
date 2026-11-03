using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class NoiseMeterUI : MonoBehaviour
{
    public GameObject player;
    public Image fill;
    public TMP_Text label;
    public float historySeconds = .5f;
    public float fallPerSecond = 1.5f;
    public Color quietColor = new Color(.25f, .8f, .45f), warningColor = new Color(1f, .7f, .15f), dangerColor = new Color(1f, .2f, .15f);
    public float RecentPeak { get; private set; }
    public float DisplayedLevel { get; private set; }
    public Color CurrentColor => DisplayedLevel >= .65f ? dangerColor : DisplayedLevel >= .25f ? warningColor : quietColor;
    readonly Queue<(float time, float level)> samples = new Queue<(float, float)>();
    Coroutine animation;
    void OnEnable() { NoiseManager.OnNoiseEmitted += ReceiveNoise; Draw(); }
    void OnDisable()
    {
        NoiseManager.OnNoiseEmitted -= ReceiveNoise;
        if (animation != null) StopCoroutine(animation);
        animation = null; samples.Clear(); RecentPeak = DisplayedLevel = 0; Draw();
    }
    void ReceiveNoise(NoiseEvent e)
    {
        if (!player || e.owner != player || (e.source != NoiseSource.Footstep && e.source != NoiseSource.Voice)) return;
        Record(e.loudness, Time.unscaledTime);
        if (animation == null && isActiveAndEnabled) animation = StartCoroutine(Animate());
    }
    public void Record(float level, float now)
    {
        samples.Enqueue((now, Mathf.Clamp01(level))); Advance(now, 0);
    }
    public void Advance(float now, float deltaTime)
    {
        while (samples.Count > 0 && now - samples.Peek().time >= historySeconds) samples.Dequeue();
        RecentPeak = 0; foreach (var sample in samples) RecentPeak = Mathf.Max(RecentPeak, sample.level);
        DisplayedLevel = RecentPeak >= DisplayedLevel ? RecentPeak : Mathf.MoveTowards(DisplayedLevel, RecentPeak, Mathf.Max(0, deltaTime) * fallPerSecond);
        Draw();
    }
    IEnumerator Animate()
    {
        var interval = new WaitForSecondsRealtime(.05f); float previous = Time.unscaledTime;
        // Run only while there is recent noise or a visible falling bar, never poll at idle.
        while (samples.Count > 0 || DisplayedLevel > 0)
        {
            yield return interval;
            float now = Time.unscaledTime; Advance(now, now - previous); previous = now;
        }
        animation = null;
    }
    void Draw()
    {
        if (fill) { fill.fillAmount = DisplayedLevel; fill.color = CurrentColor; }
        if (label) label.text = DisplayedLevel >= .65f ? "Tiếng ồn: Nguy hiểm" : DisplayedLevel >= .25f ? "Tiếng ồn: Cảnh báo" : "Tiếng ồn: Yên tĩnh";
    }
}
