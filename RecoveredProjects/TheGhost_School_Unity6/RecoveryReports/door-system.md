# Doors

Door implements IInteractable. Existing SchoolDoor scene components inherit Door, preserving their GUIDs and saved geometry. PlayerInteractor provides the E raycast and TMP prompt; legacy F input is disabled.

Controls: F toggles slowly (60 degrees/second); Shift+F toggles strongly (160 degrees/second). openAngle, speed, strongSpeed, obstacleMask and hingeOffset are configurable on the door. The closed mesh remains in place when adjusting hingeOffset.

LockDoor() locks and halts movement. UnlockDoor() is for authorized scripted unlocks. UnlockDoor(string keyId) returns true only for the exact nonempty requiredKeyId. Locked doors do not interact or emit toggle events. Key collection is not introduced in this step; inventory/story code can call the key overload later.

Subscribe per door:
    door.OnDoorToggled += (Door source, bool open, float speed) => { /* M3 sound/noise */ };

speed is angular speed in degrees/second. The event fires once when a new movement request starts; lower speed can map to lower noise in M3. It does not emit every frame or on blocked retries. No audio/noise implementation is added yet.

IsOpen describes the requested state. IsFullyOpen / IsFullyClosed describe the physical endpoint. IsBlocked and BlockingCollider expose obstruction status; IsMoving can remain true while a blocked door waits. Removing the obstruction resumes the same request without another event. Interacting again reverses the requested direction. LockDoor stops that pending movement.

Collision is checked in small rotational increments (at most 5mm corner travel), including fast motion/low FPS. Both opening and closing stop at the last clear pose rather than pushing the CharacterController. Box colliders on the moving leaf are required. Triggers and self-colliders are ignored. Existing closed-pose static contact is tracked without allowing increasing penetration; player/dynamic-object penetration is never exempted. SetImmediate is for edit-mode repair tools; during Play it uses the safe movement path.

School_Test: four asymmetric pairs are matched across the SchoolModel and Gameplay roots by actual widths and positions. The 0.626m LDoor leaves remain closed, disabled, and off the Interactable layer; the RDoor leaves move (fitted to 1.30m, or 1.26m at the tight hallway jamb). Other single doors remain interactive. The north leaf is mounted 0.20m farther along the wall normal, flush with the far face of its 32cm-deep wall. Large leaves have floor/closing-edge clearance; the tight hall hinge is moved 4cm into the opening with a matching width adjustment. Swing direction is chosen from measured clearance and the hall endpoint is 91 degrees. Walls and fixed small leaves are unchanged.

Reports: door-feature-check.txt (isolated physics/key/event tests), door-feature-setup.txt (exact pairs), door-map-check.txt (map sweeps). Scene backup: School_Test-before-door-feature.unity.

