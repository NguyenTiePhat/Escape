# M6 steps 5–6: DoorBack saw assembly and timber cutting

Configured the verified third-floor DoorBack: Level/SchoolModel/Doors/DoorBack, position (35.29,14.95,53.54). Assembly bench is beside the door at (36.4,14.95,52.95). Backup: School_Test-before-saw.unity.

Select a blade, handle or screws and tap E while looking at the bench/recipe panel. One selected ingredient is consumed immediately. All six orders work. Duplicate or unrelated items are rejected without consumption. Installed parts persist when looking away or leaving. The three real item icons begin dim; an installed part becomes bright with a green border. The equation uses icons plus an assembled-saw result icon.

The third installation creates a physical assembled_saw pickup above the workbench; it falls and settles for manual pickup with E. Assembly emits no NoiseEvent. The saw uses the existing worn metal/wood parts and has its own ItemData, prefab and icon.

Select the completed saw and hold E at the timber on DoorBack: 18 seconds, Action noise 0.7 every 0.1 seconds. Release/look away/switch items pauses without erasing progress. The saw must be selected to resume. Completion removes timber, unlocks the metal door and starts opening it. The saw remains in inventory. Door movement keeps obstruction checking, and F can toggle the unlocked door.

Recovered DoorBack was a statically batched mesh and solid colliders. Extracted its own submesh, split frame/leaf into new assets, retained the fixed frame, fitted thin jamb/header colliders, and reduced the separate RoofExit solid blocker to the actual upper wall. Other DoorBack objects are untouched. Swept open angle -95 degrees validated. A player-sized CharacterController traversed the cleared doorway.

Validation: saw-check.txt, real School_Test Play Mode, Unity exit 0. Six installation orders, duplicate prevention, E routing, icon updates, silent assembly, gravity and pickup, selected-saw gate, continuous noise and nearby listener, pause/resume, physical obstruction recovery and doorway traversal all passed. Images of the installed icon and opened doorway were visually inspected. No new rooftop victory trigger or step 7 objectives were added.

Runtime: SawAssemblyStation.cs, SawAssemblyUI.cs, SawWoodBarrier.cs; existing inventory/hold-action/noise systems reused. Reusable Editor setup: SawAssemblySetup.cs (Run refuses duplicate installation). Temporary test harnesses, audit scripts and previews removed after verification. PlayerLook source unchanged; microphone disabled only in test memory. No commit or push.
