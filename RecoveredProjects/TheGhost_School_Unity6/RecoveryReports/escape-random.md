# Random escape pickups — School_Test

Each new round spawns one silver key, one key mold, one metal blank, one saw blade, one saw handle and one screw set. All eight ItemData assets now reference the pickup prefabs under Assets/Recovery/ReferenceProps/Prefabs, including the crafted gold key and assembled saw. Crafted outputs are not spawned as shortcuts.

Silver key: 13 vetted positions accessible without locked-door traversal. Other five ingredients: 15 shared candidate positions in the existing map regions, accessible with the reusable silver key. Setup checks NavMesh connectivity, locked-door crossings, standing space, interaction sight line/range, support and item clearance. Runtime assignment uses randomized backtracking and a 0.6m minimum spacing. Spawn occurs on Playing and at most once per scene instance; the existing restart scene reload creates a fresh assignment. Identical layouts across two random rounds remain possible by design.

Validation: 50 seeded rounds produced 50 distinct layouts, exactly six unique items, no overlap and no second spawn on repeated Spawn calls. Two Play Mode rounds including scene reload passed: six ReferenceProps pickups, no fixed duplicates, each item collectible once. All original furniture retained: comparison with the pre-change scene removed only the fixed silver-key prefab instance, changed the spawner/its hierarchy and scene roots, and added spawn-point transforms.

The existing five-slot inventory, crafting recipes, doors, win conditions, battery and fuse remain unchanged. The screw model counts as one recipe set, not individual screw pickups. Multiplayer replication and extended subjective difficulty balancing were not tested.

Follow-up integration: escape-flow-check.txt passes24 assertions covering spawned ingredients through crafting, barrier removal, main-door physical traversal to victory and restart reset. The test calls gameplay APIs and does not simulate keyboard input or walking between every pickup/station. No runtime fixes required.

Access follow-up: actual PlayerInteractor raycasts passed all88 item/position combinations, the key machine and both upper-floor station targets from local standing positions. User confirmed normal upper-floor access via intentional C/crouching under a wooden obstruction. Preserve this geometry. Global standing NavMesh connectivity is not a valid player-access criterion here. The automated straight-waypoint traversal remains inconclusive near a door; it is not evidence of a confirmed map defect. No gameplay or scene changes made during this follow-up.
