# Interaction system

Scene: Assets/Recovery/Scenes/School_Test.unity.

- PlayerInteractor casts from the camera position along its forward direction, range 2.5 metres. Only the Interactable layer (slot 13) supplies targets. Colliders may be children of the component implementing IInteractable. The nearest enabled interface wins; colliders without an interface are skipped. A separate obstruction mask prevents seeing targets through walls and ignores the player's own hierarchy.
- IInteractable exposes string Prompt and void Interact(). E uses the new Input System and calls once per press. OnTargetChanged(Action<IInteractable>) fires on changes only, including null when looking away, out of range, disabled, or reading a note.
- Interaction UI / Interaction prompt is a centered TextMeshProUGUI label, subscribed by InteractionPromptUI. It displays [E] plus Prompt and clears on null. The UI does not intercept mouse input.
- The test cube starts in front of the player. Look at it within 2.5m: prompt appears. Press E: cube changes colour and Console prints its interaction count. Look away: prompt disappears. The cube can be deleted after testing.
- Flashlight and note implement the same interface; their colliders use Interactable. StartRoom no longer casts a second pickup ray or processes a second E pickup. E without a target retains use of the selected inventory item; notes block targeting while open. Doors use F interaction; Shift+F selects the stronger swing. E uses the selected item when targeting a door. See door-system.md.
- To add an object: add a Collider, assign Interactable to that collider's GameObject, and implement IInteractable on it or a parent. Ignore-trigger querying is intentional: use a solid collider for interactable objects.
- Editor menu Tools > FPS contains setup and Play Mode checks. Setup adds scene components, UI and cube; do not rerun setup after intentionally deleting the cube unless you want it recreated.

Validation: interaction-check.txt (isolated raycast/event tests), interaction-play-check.txt (School_Test component pipeline in Play Mode; native E input was checked separately during the first live run), interaction-prompt.png (rendered UI).
