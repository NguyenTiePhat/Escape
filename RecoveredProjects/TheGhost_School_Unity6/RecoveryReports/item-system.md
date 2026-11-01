# Item system — 2026-11-02

School_Test has one shared Inventory with five fixed slots. Existing flashlight/note occupy these same slots through the PlayerInventory compatibility adapter. PlayerLook and PlayerMotor are unchanged.

Controls: E picks up a targeted item; otherwise uses the selected item (including when looking at a door). F toggles a targeted door; Shift+F moves it faster. 1–5 or mouse wheel selects; G drops one item from the selected stack. Reading the note blocks gameplay input. The UI is at bottom right and subscribes to inventory/selection/message events.

ItemData assets and corresponding icons, materials and pickup prefabs are in Assets/Recovery/Items:
- room_1_key — Chìa khóa phòng 1, non-stackable, weight .08 kg.
- battery — Pin, stack limit 4, weight .12 kg, restores 25 battery.
- fuse — Cầu chì, stack limit 3, weight .06 kg.

Each ItemData holds id, itemName, icon, description, type (Key/Battery/Medkit/PuzzleItem), stackable, maxStack, weight and pickupPrefab. Medkit and PuzzleItem provide data types for future systems; no health or electrical puzzle behavior is implemented yet. Sample world models are simple placeholder shapes.

Inventory has AddItem(ItemData,int), RemoveItem(string,int), HasItem(string), CountItem(string), UseSelected(), SelectSlot(int), Drop(), GetSlot(int). Add/remove preflight the complete operation: insufficient room/items cause no partial mutation. Empty slots keep their indices. OnInventoryChanged has no arguments, OnSlotSelected sends the selected index, OnMessage sends a notification string. Inventory has no rendering or keyboard code; InventoryInput uses the new Input System, InventoryUI draws the event-driven HUD.

PickupItem implements IInteractable. Successful addition deactivates immediately and destroys the world object at frame end. Failed addition leaves the object in place and emits a full-inventory message. Pickups use layer Interactable. Dropping instantiates the ItemData pickup prefab, removes one item, applies gravity/forward velocity and emits OnItemDropped(Vector3 position,float weight) once. Weight is the mass of that one dropped item. The ray/sphere clearance checks prevent spawning through nearby walls. This event marks release, not a later collision sound. Existing note/flashlight reactivate their preserved scene objects on drop so authored note details are not lost.

A selected key unlocks only the ray-targeted locked Door with matching requiredKeyId, then consumes one key. Wrong keys and already-unlocked doors do not consume it. North exit starts unlocked and opens with F. It retains room_1_key for optional lock/key testing; samples sit beside the original note. Unlocking and opening are separate actions: E then F.

Flashlight.AddBattery returns false at capacity and keeps the battery item. Existing held light now drains 1 unit/second while actually lit; drainPerSecond is editable (0 disables drain). Empty charge turns the beam off and prevents switching it on until recharged. Initial charge/capacity are 100. A battery can be used while the flashlight is unequipped, provided it is in the inventory.

Validation: inventory-logic-check.txt checks stack boundaries, atomic failure, key/battery use, drop payload and blocked placement. item-play-check.txt checks shared legacy/new slots, note reading, destruction after pickup, real physics falling/re-pickup and real-door E/F routing. These call the same interaction-routing methods used by keyboard Update; they do not simulate a physical keyboard. inventory-items-preview.png shows the HUD in Game View.

Scene backup: School_Test-before-items.unity. Editor setup: ItemSystemSetup.RunBatch; do not rerun on a customized scene without reviewing its demo placement and exit lock configuration.

Font follow-up: InteractionFont.asset now supplies a fresh readable dynamic atlas for the interaction prompt. item-font-check.txt PASS: all requested Vietnamese glyphs present. The previous imported TMP atlas was unreliable under dynamic updates; the new font is assigned only to the scene prompt.
