# M4 phần 6: Attack và cửa

- Vào Attack khi mục tiêu đang thấy cách dưới 1.5m. Dừng NavMeshAgent, phát animation Attack (Lunge đã có) và attackClip; nếu chưa gán attackClip thì dùng roarClip hiện có.
- Chốt người nhận đòn tại lúc bắt đầu. Sau 0.6 giây kiểm tra lại khoảng cách và thị giác/vật cản rồi gọi PlayerHealth.Kill qua component cùng tên. Project hiện chưa có PlayerHealth nên ghi log Kill; chưa triển khai màn hình chết hoặc khóa điều khiển người chơi.
- Mỗi đòn chỉ tác động một lần. Kết thúc sau 0.8 giây, hồi 0.65 giây trước đòn tiếp. Người chơi chạy khỏi tầm hoặc nấp sau vật cản có thể né.
- MonsterDoorNavigation tự gắn khi MonsterAI Awake, kiểm tra capsule trước đường đi và bảo vệ khi khung hình bước dài. Cửa thường gọi Door.Open(1), chờ ít nhất một giây và đến khi mở hết rồi mới đi tiếp. Cửa vẫn dùng kiểm tra va chạm và DoorNoise hiện có, không mở cánh trang trí.
- Cửa khóa/trang trí giữ nguyên. DoorNavBlocker tạo chướng ngại carving theo các BoxCollider để tìm đường vòng. Nếu hành lang không có đường vòng thì boss bị chặn. Mở khóa tắt chướng ngại, cho phép mở và đi tiếp.
- Bộ đếm mất thị giác và hết thời gian Search tiếp tục chạy khi chờ cửa. Boss bỏ qua tiếng cửa do chính nó mở.

Test Play Mode trên NavMesh hành lang riêng: PASS, xem monster-attack-door-check.txt. Đã kiểm tra độ trễ, một hit mỗi đòn, né đòn, mở/đi qua cửa thường, tiếng ồn không lặp, không xuyên cửa khóa và tiếp tục đi sau mở khóa. Không bật micro trong test. Cần PlayerHealth ở giai đoạn sau để Kill có hiệu ứng chết thực tế.
