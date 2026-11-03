# M3 Parts 2–3 — footsteps, doors, dropped objects

School_Test is configured. PlayerLook and PlayerMotor code are unchanged. Inventory.Drop has only two additions: each drop path prepares NoiseOnImpact on the spawned/reactivated body and assigns the player as owner.

FootstepNoise samples actual horizontal displacement in LateUpdate after PlayerMotor movement. Thresholds: walk1.7m/.35, sprint2.3m/.8, crouch1.2m/.1. It uses PlayerMotor.IsSprinting (including stamina exhaustion) and IsCrouching. Standing still, pushing against a blocking wall, airborne movement and blocked input do not emit steps. Mode changes reset stride accumulation; residual distance is preserved within a mode. Large teleports reset tracking.

A downward ray from the controller feet selects the nearest non-trigger supporting collider, ignoring the player. Tags Tile/Wood/Metal/Carpet and Gạch/Gỗ/Kim loại/Thảm are accepted, including tags on ancestors. Recognized tags take precedence over Physics Material names. Physics materials with those names are also accepted; four ready-to-assign assets are in Assets/Recovery/Noise. Unclassified surfaces default to Tile; existing school geometry has not been automatically relabeled from visual material names.

| Surface | Multiplier | Walk loudness / radius | Sprint loudness / radius | Crouch loudness / radius |
|---|---:|---:|---:|---:|
| Tile / Wood | 1 | .35 / 8.75m | .8 / 20m | .1 / 2.5m |
| Metal | 1.3 | .455 / 11.375m | 1 / 25m | .13 / 3.25m |
| Carpet | .6 | .21 / 5.25m | .48 / 12m | .06 / 1.5m |

The same clamped loudness goes to NoiseManager and AudioSource.PlayOneShot. The player has a dedicated spatial Footstep Audio source. Two recovered clips per Tile/Wood/Metal are assigned. CarpetStep1/2.wav are derived from the existing tile clips with a 600Hz low-pass and gain.65; these are prototype muffled footsteps, not new recorded foley. Clips can be replaced in FootstepNoise Inspector. Audio does not determine AI/noise propagation; NoiseManager uses its specified distance formula.

DoorNoise subscribes/unsubscribes to its Door.OnDoorToggled on enable/disable. Loudness=clamp(.1+.004*abs(angularSpeed),.1,1). Existing slow60deg/s gives.34/radius8.5m, strong160 gives.74/radius18.5m. Opening and closing both emit once per accepted toggle, with door ownership. Locked doors and rejected/blocked initial moves do not toggle or emit; blocked retries do not repeatedly generate noise. All13 movable leaves are wired; decorative small leaves remain fixed.

InventoryNoise subscribes to Inventory.OnItemDropped. The release is a Drop event at the supplied position, using weight and releaseSpeed=.8m/s (matching Inventory.Drop). A later collision emits a separate Impact event at the first contact point. This is intentional: release and contact are different events.

NoiseOnImpact is reusable: attach to a bottle/crate with a collider; RequireComponent supplies a Rigidbody. For accepted OnCollisionEnter callbacks:
- energy=.5*mass*relativeSpeed^2
- loudness=clamp(.1+.08*sqrt(energy),.1,1)
- skip speeds below.2m/s and contacts within.12s of the previous emitted impact to avoid resting jitter/contact spam
- owner defaults to the impacting object; Inventory.Drop assigns its player as owner

Three item prefabs and existing sample pickups have NoiseOnImpact. Both new items and legacy flashlight/note are prepared by Inventory.Drop, so runtime drops always receive it. This work does not introduce a new throw control; tests throw bodies using real Rigidbody velocity. F/Shift+F still controls doors, G drops, E picks up/uses items, N retains the Part1 debug emitter.

Tests:
- noise-gameplay-check.txt: isolated Unity Play Mode with real CharacterController movement; exact step-distance thresholds; idle/wall/air silence; ray material + clip choice; metal sprint and carpet crouch loudness/radius; real Door event subscriptions/strong-vs-slow/no locked noise; actual Inventory drop event + impact attachment; Physics.Simulate drives real OnCollisionEnter. Bottle(.15kg, gentle fall) measured.147; crate(8kg, launched at7m/s horizontal/downward) measured1.000.
- noise-scene-check.txt: main School_Test has spatial source and AudioListener, all8 clips decode to nonzero samples, all13 moving doors wired, inventory bridge,3 prefab impacts, all8 English/Vietnamese tags and4 physics-material mappings; tag precedence checked.
- Both tests and main setup exit0. Tests verify the same event loudness used by the Gizmo; no rendered Gizmo screenshot or subjective listening test is claimed.

Scene backup: RecoveryReports/School_Test-before-noise-gameplay.unity. Editor setup menu: Tools/M3/Set up footsteps and object noise. Attach DoorNoise to future door prefabs and InventoryNoise to future players; attach NoiseOnImpact to other dynamic props.
