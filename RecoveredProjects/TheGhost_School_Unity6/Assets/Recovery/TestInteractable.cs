using UnityEngine;
using UnityEngine.Events;

public sealed class TestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "Tương tác khối thử";
    public string Prompt => prompt;
    public int InteractionCount { get; private set; }
    public UnityEvent onInteract = new UnityEvent();
    public void Interact()
    {
        InteractionCount++;
        var renderer = GetComponent<Renderer>();
        if (renderer)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            var color = InteractionCount % 2 == 1 ? Color.green : Color.cyan;
            properties.SetColor("_BaseColor", color); properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(properties);
        }
        Debug.Log("Test cube Interact #" + InteractionCount, this);
        onInteract.Invoke();
    }
}
