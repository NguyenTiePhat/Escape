using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionPromptUI : MonoBehaviour
{
    public PlayerInteractor interactor;
    public TMP_Text label;
    void OnEnable()
    {
        if (!interactor || !label) return;
        interactor.OnTargetChanged += ShowTarget;
        ShowTarget(interactor.Target);
    }
    void LateUpdate() { if (interactor && label && interactor.Target != null) ShowTarget(interactor.Target); }
    void OnDisable()
    {
        if (interactor) interactor.OnTargetChanged -= ShowTarget;
        if (label) { label.text = ""; label.enabled = false; }
    }
    void ShowTarget(IInteractable target)
    {
        bool valid = target is MonoBehaviour behaviour && behaviour && behaviour.isActiveAndEnabled;
        label.text = valid ? (target is Door door && !door.isLocked ? "[F] " : "[E] ") + target.Prompt : "";
        label.enabled = valid;
    }
}
