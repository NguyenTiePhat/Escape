# Điểm tiếp tục — 2026-10-28

Người dùng muốn khôi phục đầy đủ map trường học The Ghost từ XAPK, mở xem và chuyển workspace ra khỏi OneDrive sang D:/UnityProjects/theghost.

- Dự án gốc ở root là blockout IUH; không ghi đè.
- Bản xuất nguyên trạng: RecoveredProjects/TheGhost_Original/ExportedProject (Unity 2022.3.62f2).
- Bản tham quan: RecoveredProjects/TheGhost_School_Unity6 (Unity 6000.5.7f1).
- Scene xem: Assets/Recovery/Scenes/School_Explore.unity. Unity đã import, compile và lưu thành công.
- CHƯA hoàn thiện: ảnh RecoveryReports/school-spawn.png và school-reverse.png cho thấy camera dưới sàn. Spawn hiện (27.01,0,57.61), nhiều nội thất ở y≈1.95; cần sửa điểm đứng, kiểm tra collider.
- validation.txt: 9844 transforms, 2468 MeshFilters, 430 null mesh slots, 3882 colliders, 272 null material slots. Cần phân biệt ô trống có sẵn/LOD/đối tượng ẩn với thiếu dữ liệu thật; không tuyên bố khôi phục 100%.
- 8342 script component không hoạt động đã gỡ trong scene tham quan; bản gốc giữ nguyên. 163 placeholder shaders thay bằng Standard. Gameplay chưa phục hồi.
- Preview/school-dependency-audit.json: 1873 dependencies, 16 unresolved GUID thuộc font atlas. Tools/audit_recovered.mjs dùng kiểm tra.
- AssetRipper 2.0.0 tại Tools/AssetRipper, đã dừng. Dữ liệu giải nén tại ImportSource/TheGhost/GameData. XAPK gốc giữ nguyên.
- Điều khiển tham quan: click, WASD, Shift, F bay/đi, Q/E lên/xuống, L đèn, R reset, Esc nhả chuột. Chưa kiểm thử Play Mode trực tiếp.
- Unity exe: D:/Unity/6000.5.7f1/Editor/Unity.exe.

Việc tiếp theo: hoàn tất chuyển thư mục, mở project Unity6; sửa spawn trước khi cho chơi; kiểm tra mesh/material trống, chụp lại ảnh và ghi giới hạn còn lại.

Migration complete: 42,490 files / 9.811 GiB copied to D:/UnityProjects/theghost without Robocopy errors. Preview scene SHA256 verified. OneDrive original retained as backup. Continue work from D: copy.

Latest edits on D: FinishSchool.cs restored 250 exact-name meshes, disabled WarmUpShaders root, corrected grounded spawn to (27.01,2.10,57.61), confirmed FirstFloor collider at y=1.98. Saved School_Explore and captured screenshots. 180 empty mesh slots and 272 material slots remain; not a complete recovery. Unity launched with FinishSchool.Run to enter Play Mode. Continue investigate remaining slots and visual layout; see RecoveryReports/finish.txt.

First-person hands added: Assets/Recovery/FirstPersonHands.cs procedural prototype arms, right-hand flashlight, movement sway/bob. SchoolExplorer attaches it at runtime; L toggles hand light. HandsPreview.Run saves component into School_Explore and renders RecoveryReports/first-person-hands.png. Unity compiled and screenshot inspected. Not a realistic rigged character; map recovery issues remain. Latest beam fix uses a separate forward-facing light object.

Start room added in School_Explore: START ROOM — editable intro / StartRoom component with noteTitle and noteText serialized in Inspector. Table has flashlight and paper, E raycast interactions (ignores own player collider, respects walls), initial hands hidden until pickup, L toggles acquired light, note modal blocks SchoolExplorer and unlocks cursor. F flight disabled for intro scene. Both prop reachability checks PASS at ~1.5 m; see RecoveryReports/start-room.txt and start-room.png. BuildStartRoom.Run recreates intro and resets default note, so edit note on scene and do not rerun builder to preserve custom writing. Unity launched in Play Mode.

2026-10-29 surface repair: FixRoomSurfaces.Run saved School_Explore. Added 139 architectural mesh colliders, opaque two-sided shader to architecture materials, restored starting floor slab bounds x23.5..28.5 z56.9..63.3 top1.95, controller radius .34, near clip .025. Unity batch success, no shader/CS errors; ground raycast FirstFloor PASS, screenshot repaired-floor.png inspected. Full map walking not tested; outside patch may still have recovery defects. Earlier attempted mesh duplication crashed Unity; replaced with shader approach.

Crash workaround: latest Hub launch failed D3D12 CreateCommittedResource 887a0005 / graphics initialization. Opened via -force-d3d11 successfully; process responded with School_Explore window <DX11>. Use RecoveredProjects/TheGhost_School_Unity6/Open-Unity-DX11.cmd. No claim driver root cause resolved; no driver changes.

2026-10-29 start floor fix: active scene School_Test.unity repaired by RepairStartFloor. Previous patch at y=1.95 overlapped the original visual floor. Raised patch top and BoxCollider together to y=2.005, above existing collision at 1.98. 99/99 local floor collider samples PASS; start-floor-fixed.png rendered and inspected. Scene backup: RecoveryReports/School_Test-before-floor-fix.unity. School_Explore was not changed in this floor repair. Updated FixRoomSurfaces builder height to prevent recreating coplanar patch. Full walking validation still pending; no Windows memory settings changed.

2026-10-29 movement repair in School_Test: found scene-batch meshes incorrectly assigned to per-object MeshColliders by FixRoomSurfaces. Disabled 21 such colliders intersecting the starting room, retained original wall/furniture colliders. Expanded floor patch to x16.90..32.82,z56.88..63.70,top2.005 with dedicated StartRoomFullFloor material. CharacterController tests: 3 routes blocked before, all 3 pass after (6m,14m,14m). Four cardinal wall rays still hit original wall colliders. Image room-movement-fixed.png inspected against user screenshot. Backup School_Test-before-movement-fix.unity; report room-repair.txt. FixRoomSurfaces now skips adding colliders from Combined Mesh (root: scene) assets. School_Explore unchanged. Full map not validated.

2026-10-29 ceiling/wall lighting repair in School_Test: added opaque ceiling slab underside7.10, x16.90..32.82,z56.88..63.70, 9/9 upward coverage checks pass. Dedicated ceiling material, 2 shadowed room point lights at x21/29 y6.6. Disabled RECOVERY inspection daylight after before/after renders confirmed it caused jagged exterior silhouette shadows on interior wall. Ceiling-after.png inspected; wall artifact removed. No stale lightmap bindings found (0). Scene backup School_Test-before-ceiling-fix.unity. Movement colliders unchanged. Other map areas may be darker because inspection directional light was scene-wide.

2026-10-30 CompactStartRoom applied to School_Test. Interior 4.8x3.8m, ceiling3m above floor, clean solid partition walls inside previous hall; west doorway1.4x2.4m opens to retained hall. Table moved north against wall, note preserved, player/home updated. CharacterController doorway traversal PASS; both item raycasts PASS. Backup School_Test-before-compact-room.unity; compact-room.txt/png. Existing recovered walls outside new room retained.

2026-10-30 original-room correction replaces nested room. OriginalRoomAudit removed START ROOM compact walls. ReworkOriginalRoom keeps original west/south/north boundaries; one east partition x24.5 with doorway z59.2..60.6 joins existing walls, reducing original room. Blocked DoorNarrowSolid(6) disabled and opening sealed; table at21.8,2.005,61.93 against original north wall, spawn/home21.8,2.06,60.6. CharacterController exit traversal and both item raycasts PASS. FinishOriginalWall replaced protruding west corner patches with continuous face aligned original boundary. Scene saved; images original-rework-0/1/2 and original-west-final inspected. Backups in RecoveryReports. Not a new inner box room.
