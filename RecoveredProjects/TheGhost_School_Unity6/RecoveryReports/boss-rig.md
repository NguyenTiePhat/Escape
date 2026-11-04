# M4 — Rig và animation theo Hành động.docx

**Cập nhật:** School_Test hiện dùng MonsterAI tuần tra; chuỗi/phím xem thử prototype đã tắt. Xem `monster-patrol.md` cho trạng thái gameplay hiện tại. Bộ rig và các clip dưới đây vẫn được dùng.

Nguồn yêu cầu: `D:/Rac/Hành động.docx`, đọc bảng bảy hành động. Tài liệu gốc không bị sửa.

## Tài sản đã tạo

- `SourceAssets/BossRig/M4Boss.blend` ở root repository: nguồn Blender có xương, skin weights, hai control IK bàn chân và các action có thể sửa tiếp.
- `Assets/Recovery/Boss/Rigged/M4BossRigged.fbx`: mesh skinned, rig Generic và clip đã bake 30 fps.
- `BossActions.controller`: Animator Controller với các trạng thái độc lập.
- Prefab `Assets/Recovery/Boss/M4Boss.prefab` và instance `M4 Boss` trong `School_Test` dùng rig mới, chiều cao khoảng 2.2 m, giữ vật liệu PBR.
- Ba mesh skinned dùng LODGroup: 50.000 / 20.000 / 7.000 tam giác, chung skeleton. Không chồng Mesh LOD của bản mesh tĩnh lên bản rig.

## Các hành động

| Clip | Thời lượng | Lặp | Chuyển động |
|---|---:|---|---|
| Idle | 3.6 s | Có | Thân thở, vai nhấp nhô, đầu nghiêng nhẹ |
| Walk | 2 s | Có | Chân bước chậm/lê nhẹ, tay đung đưa, đầu quét |
| Alert | 1 s | Không | Đầu giật về một phía rồi giữ tư thế |
| Investigate | 1.6 s | Có | Thân thấp hơn, bước dò với nhịp dừng/bước |
| Run | 0.9 s | Có | Thân chúi, chân bước dài hơn, tay vung mạnh |
| Lunge | 1 s | Không | Thu người 0.3 s, đưa tay/vồ và lao trong khoảng 0.3–0.7 s, hồi tư thế |
| Search | 4 s | Có | Đầu quay theo nhịp giữ, tay quờ phía trước |
| Roar | 1.8 s | Không | Giữ thêm hành động gầm của bản trước |

Các clip di chuyển là in-place. CharacterController điều khiển dịch chuyển và chặn vật cản; Animator điều khiển các khớp. Không còn dùng lắc cả mesh để thay animation khi đã có BossAnimationDriver.

## Tích hợp

`BossPrototypeMotion.SetState(...)` chọn hành động, phát clip qua `BossAnimationDriver`. `AlertAt(position)` và `SetAttentionPoint(position)` cho phép phần AI truyền vị trí tiếng động; đầu có thể xoay theo hướng đó ở các trạng thái chú ý. Bộ này chưa tự tìm đường, nghe NoiseManager, truy đuổi hoặc trừ máu người chơi.

Chuỗi xem thử tự chạy qua các hành động. F6 bật/tắt chuỗi, F7 gầm, F8 vồ, F9 chuyển hành động thử tiếp theo. Các phím thử chỉ nhận khi con trỏ khóa trong game.

Rig là bản đầu làm từ mesh đã ở tư thế khom, không phải nhân vật T-pose. Các ngón tay đi cùng xương bàn tay; chưa có rig ngón riêng, biểu cảm mặt hoặc chỉnh chân thích nghi cầu thang. Nguồn Blender được giữ để tinh chỉnh các khớp và động tác tiếp.

## Kiểm tra và sao lưu

`boss-rig-check.txt` ghi kiểm tra skin weights, số tam giác/bones, độ dài clip, mesh thực sự biến dạng khi sample và độ liền mạch của loop. `boss-rig-play-check.txt` ghi kết quả chạy Animator trong Play Mode; xem trạng thái cuối trong file này. Các ảnh từng hành động được lưu dưới `RecoveryReports/BossActions` sau khi test thành công.

**Kết quả cuối 04/11/2026: PASS.** Unity được mở lại bằng tiến trình riêng để kiểm tra liên kết đã lưu. Cả tám trạng thái chạy trong Play Mode, có thay đổi góc khớp thực; tiến trình kết thúc mã 0. Đã kiểm tra trực quan các ảnh Walk, Lunge và Search. Lỗi mất controller do reimport sau khi gán đã được sửa bằng cách giữ và ghi rõ các override của instance FBX trước khi lưu prefab/scene.

Trong test, micro được tắt tạm và âm thanh boss được mute; khôi phục trạng thái micro khi trở lại Edit Mode. Scene trước thay rig: `School_Test-before-boss-rig.unity`.

Project hiện vẫn dùng Built-in; không đổi toàn bộ map sang URP trong bước rig. Chưa commit/push.
