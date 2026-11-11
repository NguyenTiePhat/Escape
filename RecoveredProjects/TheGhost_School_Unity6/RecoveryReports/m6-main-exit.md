# M6 step 4 — main exit

School_Test main exit: Gameplay/Doors/DoorMain_L and DoorMain_R, matching the selected screenshot. Both swing outward; original solid blocker disabled, real frame retained. Main leaf fit repaired and swept collision verified.

Silver key is on the starting-room table at (21.28, 2.88, 61.63). Select it and press E on a locked playable ordinary door. It remains reusable. Existing decorative panels stay decorative. F toggles unlocked doors.

Crafting machine outputs the golden main_exit_key on its tray. Select it and press E at the main exit. Silver cannot unlock the exit; gold cannot unlock ordinary doors. Both main leaves must finish opening before victory. Obstruction prevents victory until the swing clears.

Victory displays “Bạn đã thoát khỏi trường” and “Chơi lại”, stops player and monster, stops voice capture, and releases the cursor. Restart reloads School_Test, resetting inventory, crafting, locks and victory state. School_Test is enabled in build settings.

Validation: main-exit-setup.txt (actual scene sweep/configuration); main-exit-check.txt (Play Mode input routing, key mismatch, reusable silver, blocked swing, single victory, frozen controls, real scene reload). Unity exited 0; no compile errors or runtime exceptions in the test log. Temporary test harnesses/logs removed after passing. Backup: School_Test-before-main-exit.unity. Reusable setup: Assets/Recovery/Editor/MainExitSetup.cs.
