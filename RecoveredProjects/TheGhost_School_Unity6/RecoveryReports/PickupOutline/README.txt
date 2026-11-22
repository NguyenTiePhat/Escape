White pickup proximity outline

PickupProximityOutline automatically attaches to the player camera through PlayerInteractor. Any active PickupItem within3m of camera-to-collider bounds receives a white2.5px outline without requiring aim at the item. Active-instance registration also covers crafted items and inventory drops; collection/disable removes the outline immediately. No scene editing needed.

Built-in rendering uses a CommandBuffer after transparent rendering, stencil bit64 to protect the object interior and depth-tested expanded mesh silhouettes. The shader lives in Resources so builds retain it. It does not alter the original renderer materials or add new lights. Inactive/finished rounds and dead/escaped local players clear the render commands.

Play Mode validation passed: player camera integration; near white pixels203; beyond3m0; opaque occluder0; disable0; reenable successful; item pickup successful; no outline after collection. Game-start and isolated near images inspected. No saved scene changes.
