using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NoiseTestEmitter : MonoBehaviour
{
    public bool enableTestInput = true;
    [Range(0, 1)] public float loudness = .4f;
    public Transform emissionPoint;
    public NoiseTestListener nearListener;
    public NoiseTestListener farListener;
    void Update()
    {
        if (!enableTestInput || !Application.isFocused || Keyboard.current == null) return;
        if (Keyboard.current.nKey.wasPressedThisFrame) EmitTestNoise();
    }
    public void EmitTestNoise()
    {
        Vector3 position = emissionPoint ? emissionPoint.position : transform.position;
        if (nearListener) nearListener.transform.position = position + Vector3.right * 5f;
        if (farListener) farListener.transform.position = position + Vector3.right * 15f;
        NoiseManager.EmitNoise(position, loudness, NoiseSource.Impact, emissionPoint ? emissionPoint.gameObject : gameObject);
    }
}
