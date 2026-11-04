# M4 phần 1 — AI tuần tra

**Cập nhật:** đã nối thính giác và Investigate; xem `monster-hearing.md`. Các mô tả “chỉ Patrol” bên dưới ghi lại phạm vi phần 1.

Đã tích hợp vào `School_Test` và prefab `M4Boss` ngày 04/11/2026.

- `MonsterAI` điều khiển `NavMeshAgent`, enum `MonsterState` gồm Patrol, Investigate, Chase, Search, Attack. Hiện chỉ chạy Patrol.
- Đi lần lượt qua `Transform[] waypoints`, quay lại đầu danh sách. Khi thực sự đến nơi và dừng, chọn thời gian chờ ngẫu nhiên 2–4 giây. Walk khi di chuyển, Idle khi chờ.
- `Assets/Recovery/Boss/MonsterConfig.asset` chứa patrolSpeed=.3, chaseSpeed=1.3, hearingThreshold=.15, sightRange=12, sightAngle=100, searchDuration=8. AI đọc patrolSpeed từ asset mỗi frame nên chỉnh trong Play Mode có hiệu lực ngay. Các trường còn lại chuẩn bị cho các phần AI tiếp theo.
- `MonsterAIEditor` dùng Handles.Label hiển thị state, thời gian chờ khi chọn boss trong Scene view; vẽ tên và vòng tròn waypoint.
- Script chuyển động thử và CharacterController của boss đã tắt để NavMeshAgent sở hữu di chuyển. Phím F6–F9 của prototype không hoạt động trong chế độ AI này. Bộ rig/Animator/PBR và các clip vẫn được giữ.
- Waypoint null hoặc không có đường hoàn chỉnh được bỏ qua. Không có config/NavMesh/waypoint thì dừng an toàn; khi không có đường, thử lại sau một giây. Không tự teleport qua tường hoặc chấp nhận đường partial.

## NavMesh và scene

Scene lưu ban đầu có `m_NavMeshData: 0` và triangulation bằng 0. Audit bốn asset khôi phục tìm thấy `Assets/NavMeshData/NavMesh_0.asset` đúng vị trí boss, 6.847 tam giác. Component `MonsterNavMesh` nạp asset đã bake này, quản lý vòng đời handle; không bake lại toàn map.

Đã tạo `Monster Patrol Waypoints` với bốn điểm gần boss, kiểm tra mọi đoạn đi bằng đường NavMesh hoàn chỉnh và capsule radius .42 m/height 2.2 m đối chiếu collider hiện có. Chỉ tuyến này được xác minh trong map đã chỉnh sửa; khi thay đổi kiến trúc hoặc mở rộng tuyến, cần cập nhật/kiểm tra lại NavMesh tương ứng.

Prefab giữ config và agent nhưng không chứa tham chiếu waypoint ngoài prefab. Instance trong School_Test giữ bốn waypoint của scene.

## Kiểm tra

`monster-nav-audit.txt`: đối chiếu asset NavMesh và vị trí spawn.

`monster-patrol-setup.txt`: PASS, bốn waypoint và đường đi không giao collider.

`monster-patrol-check.txt`: PASS Play Mode thực. Boss đi đủ vòng và quay lại điểm đầu, các lần chờ trong 2–4 giây và không rời điểm sớm. Fixture riêng có tường dài: agent đi vòng hai chiều, không giao capsule với tường. Đổi patrolSpeed áp dụng ngay cho hai agent, bỏ qua null/điểm không tới được, danh sách rỗng dừng agent. Config test là bản clone, asset gốc không đổi; micro tắt tạm khi test và khôi phục sau đó. Test dùng thời gian game với timeScale=3 để chạy nhanh; scene/config lưu không thay timeScale.

Backup: `School_Test-before-monster-patrol.unity`. Chưa commit/push.
