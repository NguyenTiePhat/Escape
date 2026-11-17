# Drive furnishing revision — 2026-11-14

Scene: Assets/Recovery/Scenes/School_Test.unity.
Source: Drive folder 1mN1p65s_0-Y1HsBqVuxwuKVGnlyc51EQ. Original FBXs and source metadata retained in SourceAssets/DriveFurniture.

Five instances, grouped under "Drive furnishings - curated additions":
- Two student desk/chair sets moved from the upstairs window landing to the rear of Biology Classroom02, facing the teaching wall in the same direction as existing desks. Positions (62.8,1.98,43.1) and (62.8,1.98,45), yaw270.
- Gym table increased from scale2.6 to3.6 (+38%), beside the bleachers.
- Geography display increased from2.8 to4.3 (+54%), fitted to the reading-alcove wall.
- Metal cabinet increased from1.35 to2.1 (+56%), slightly repositioned for wall clearance.

Sizes are calibrated to the recovered map's oversized original environment, not a claim of regulation sports dimensions. No additional decorative clutter added.
Models use preserved imported transforms inside prefab wrappers, optimized meshes and Standard textured materials. Colliders and stationary carving obstacles match visual bounds.

Validation: placement checks cover floor support, existing-collider overlap, matching visual/collision bounds, and baseline walkable connectivity in all three affected areas. 16,545 pre-existing objects/components unchanged. Saved-scene checks cover model/material references, the previous17 furnishings, silver-key DoorNarrowP1 setup, and CharacterController routes; see placement-check.txt.
Images: after-study.png, after-gym.png, after-display.png, after-cabinet.png. Preview cameras use temporary illumination, which is not saved to the scene.
Backup: RecoveryReports/School_Test-before-drive-furnishing.unity.
No full monster gameplay test or lighting rebake performed.
