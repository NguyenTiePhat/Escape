# M3 Part 1 — Noise core

NoiseEvent contains position, loudness, source and owner. NoiseSource: Footstep, Door, Drop, Voice, Impact. INoiseListener exposes OnNoiseHeard(NoiseEvent e, float perceivedLoudness).

Emit from gameplay using:
```csharp
NoiseManager.EmitNoise(transform.position, .4f, NoiseSource.Impact, gameObject);
```
NoiseManager.Instance provides the singleton and creates it lazily if a scene has none. Register listeners in OnEnable with NoiseManager.RegisterListener(this, transform), and unregister in OnDisable with NoiseManager.UnregisterListener(this). A different transform may be supplied as the hearing position. Duplicate registration replaces the position rather than duplicating callbacks. Disabled/destroyed listeners are skipped. Registration changes and nested emissions during callbacks are safe through snapshots.

Radius = clamp01(loudness) * 25 metres.
Perceived loudness = loudness * (1 - distance / radius).
Only distances <= radius receive callbacks; exact boundary receives zero. Zero/negative loudness is silent, values above 1 clamp to 1, non-finite positions/loudness are ignored. Distance is Euclidean in 3D. No wall occlusion, source ownership filter, or gameplay noise producers are introduced in Part 1.

School_Test test setup:
- Noise Manager: debugLogging=true, drawGizmos=true. Both can be toggled in Inspector.
- M3 Noise Test - N key: press N with Game view focused in Play Mode. Emits Impact at the player at loudness .4 (radius 10m).
- Dedicated near/far test listeners are repositioned 5m and 15m from the emission point on each test. Near hears .2; far hears nothing.
- Enable Gizmos in Scene view (or Game view) to view the last event's orange wire sphere for one unscaled second. New emissions replace the previous sphere.
- Disable enableTestInput or the test emitter when done. No interaction with F/G/E bindings.

Validation: NoiseCoreCheck.Run executed successfully in isolated Unity batch project; main project compiled and NoiseSetup.RunBatch saved School_Test successfully. Report noise-core-check.txt verifies attenuation, radius boundary/exclusion, payload, duplicate registration, disabled/unregistered/destroyed listeners, clamp/zero, the same EmitTestNoise action called by N, optional logs and gizmo lifetime predicate. Tests call the action programmatically; a physical N key and a rendered Gizmo screenshot were not automated. Logs show near5m perceived=.200 and no far15m delivery for loudness .4.

Backup: School_Test-before-noise.unity. No automatic footstep, door, drop, voice or impact integration yet.
