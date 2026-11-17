# M7 step 1: round state and one-hit death

School_Test now has GameStateManager on the existing M6 Escape Game object, PlayerHealth on the player, and an outside threshold volume beyond the main double door. Existing serialized EscapeWinController references remain valid through a compatibility facade.

- States: MainMenu, Loading, Playing, Won, Lost. OnStateChanged reports each transition. School_Test starts Playing; restart transitions through Loading and reloads the saved scene.
- Each player starts with one health. The monster's existing hit timing, distance and visibility checks call typed PlayerHealth.Kill(). Death is idempotent and emits OnPlayerDied once. Dead/escaped players are excluded from monster vision.
- Debug shortcut K toggles invulnerability while focused in gameplay with the mouse locked. It defaults off each round and prevents Kill without interfering with escape. No additional UI. Toggling after death does not revive the player. Targeted checks are recorded in m7-godmode-check.txt.
- Death stops movement, looking, inventory, held interactions, first-person hand input and microphone input. The camera drops to 22 cm above the CharacterController's feet and rolls over 0.9 seconds using unscaled time. The loss overlay fades in while the fall remains visible.
- Outcome uses the registered participant roster: no result for an empty roster; all dead loses; at least one living player and all living players escaped wins. Escaped players cannot subsequently die. This is local gameplay state, not a networking implementation.
- Gold key still opens the main double door. Both leaves must be fully open, and the player's controller centre must enter the outside volume, before that player counts as escaped. Opening the door alone no longer wins. The exit volume is centred at (35.89, 3.29, 64.63), facing +Z, size (2.25, 2.44, 0.90).
- Final outcomes stop the monster and gameplay, release the cursor and offer Chơi lại. Restart restores a fresh scene, controls, health and timescale.

Validation: m7-round-check.txt records Play Mode checks against the saved School_Test scene: actual monster hit handler, duplicate Kill/event guards, control locks, unscaled camera fall at timescale zero, restart, two-participant death/escape combinations, key gating and real CharacterController movement through the main door. The second participant is a local test object. No online multiplayer test was performed.

Scene preservation compared with School_Test-before-M7-round.unity: no existing YAML blocks removed; only the player and game-controller GameObject component lists and SceneRoots changed; six blocks added. Existing furniture and gameplay placements retained.

Editor tools: M7RoundSetup.Run installs the scene components and runs validation; M7RoundValidation.Run runs logic checks on the currently loaded scene; M7RoundValidation.Screens captures result screens in a graphical editor session. These explicitly invoked validation tools exit Unity when finished.

Manual check: Play School_Test, let the monster hit you once; verify no movement/look/item use, camera falls, and Bạn đã thua appears. Click Chơi lại. Craft/use the gold key, open both main leaves and walk out; verify Bạn đã thoát khỏi trường. The existing rooftop saw path has not been converted into a second victory exit in this step.

Visual QA: m7-falling.png, m7-lost.png and m7-won.png captured in a graphical Play Mode session and inspected. Camera roll/fall and both Vietnamese result labels/buttons are visible and readable. m7-visual-check.txt PASS. Victory screenshot uses Escape() to isolate result presentation; physical doorway traversal is covered separately in m7-round-check.txt.
