# M4 — Boss mesh tĩnh và chuyển động thử

**Cập nhật 04/11/2026:** bản này đã được thay bằng rig và Animator trong School_Test. Xem `boss-rig.md`; phần dưới ghi lại giai đoạn prototype mesh tĩnh trước đó.

Đã cập nhật trực tiếp prefab `Assets/Recovery/Boss/M4Boss.prefab` và boss trong `School_Test` ngày 03/11/2026.

- FBX gốc có đủ bốn PNG nhúng: BaseColor, Normal, Roughness, Metallic. Đã trích xuất nguyên dữ liệu, đối chiếu tên các Video/Content trong FBX. Không dùng texture tự tạo thay thế.
- Import tối đa 2048, mipmaps; BaseColor sRGB, normal đúng Texture Type, dữ liệu metallic/roughness tuyến tính. MetallicSmoothness đóng gói R=metallic, G=1, A=1-roughness từ ảnh gốc. Ảnh nguồn 4096 vẫn được giữ.
- Project hiện dùng Built-in, material BossPBR dùng Standard thực của Unity. Bộ kênh cũng dùng được cho URP Lit; setup chọn URP Lit khi project thực sự đang dùng URP. Chưa chuyển toàn bộ map và các shader khôi phục sang URP.
- Cao 2.2 m, gốc ở chân, root scale 1. VisualPivot riêng để thay bằng Animator khi có rig.
- Native Mesh LOD của Unity 6.5: 50000 / 26608 / 13818 / 7034 tam giác, không chồng thêm LODGroup.
- CharacterController cao 2.2m, radius .42m. Visual toàn thân có thể vượt ngoài capsule ở tay; chưa có hitbox chi tiết từng chi.
- BossPrototypeMotion: Idle thở nhẹ, Walk nhún/nghiêng theo di chuyển thực, Roar ngửa/rung thân và AudioSource 3D, Attack báo trước .32s rồi lao ngắn, hồi tư thế. Tường chặn thân qua CharacterController.
- Chuỗi thử tự chạy quanh vị trí ban đầu; đây chưa phải AI tìm đường. F6 bật/tắt chuỗi thử, F7 gầm, F8 tấn công khi con trỏ đang khóa trong game. Dùng clip PumpkinGrowl sẵn có làm âm thanh tạm.
- OnStateChanged và OnAttackContact dành cho tích hợp tiếp. Chưa trừ máu người chơi, chưa phát hiện/đuổi mục tiêu, chưa có xương/animation bước chân riêng.

Kiểm tra: `boss-prototype-check.txt` PASS cho cấu hình, số tam giác, CC di chuyển, động tác gầm, lao bị tường chặn và hồi phục. `boss-prototype-play-check.txt` PASS trong School_Test Play Mode cho đi tại spawn, gầm, clip âm thanh hợp lệ, hồi phục sau lao và tiếp đất. Micro tắt tạm thời và âm thanh boss được mute trong lượt test; trạng thái micro được khôi phục khi về Edit Mode. Ảnh `boss-prototype-preview.png` đã kiểm tra trực quan từ phía trước dưới đèn kiểm tra tạm; đèn/camera chụp không lưu vào scene.

Sao lưu scene: `School_Test-before-boss-prototype.unity`. Búp bê cũ không bị thay đổi. Chưa commit/push.
