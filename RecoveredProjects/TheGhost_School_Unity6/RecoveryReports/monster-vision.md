# M4 phần 3 — Thị giác

MonsterAI gọi MonsterVision trước xử lý Patrol/Investigate. Nón nhìn theo hướng root boss, mắt lấy vị trí xương Head (không dùng trục forward của xương vì hướng import khác hướng mặt). Raycast riêng tới camera/đầu và tâm CharacterController người chơi; chỉ cần một điểm trong góc/tầm và không bị vật cản che thì thấy. Bỏ qua collider của chính boss/người chơi và trigger; LayerMask obstacles mặc định tất cả layer.

MonsterConfig hiện có sightAngle=100 độ (50 độ mỗi phía), sightRange=14m, awarenessGainPerSecond=.5, awarenessLossPerSecond=.3. Awareness tăng khi thấy, giảm khi mất dấu, clamp0–1. Chỉ khi đầy mới chuyển Chase, hủy việc chờ/điều tra và dừng agent. Hành vi chạy đuổi/mất dấu/tấn công chưa triển khai ở phần thị giác này.

Hệ số nhân: ngồi x.6; đèn pin thực sự active/enabled, intensity>0 x1.5; tối hoàn toàn khi không bật đèn x.4. Tầm đứng14m, ngồi8.4m, tối tắt đèn5.6m, bật đèn21m, ngồi+bật đèn12.6m. Đèn cất trong túi có GameObject inactive không tính là bật.

Bóng tối là dữ liệu gameplay: MonsterVision.playerInCompleteDarkness có thể được hệ thống đèn/room cập nhật; MonsterDarknessZone với BoxCollider trigger đánh dấu vùng hoàn toàn tối. Đổi enabled của zone áp dụng ngay, hỗ trợ vùng chồng nhau. Chưa tự suy đoán ánh sáng từ ảnh/render hoặc gắn nhãn tối cho các phòng có đèn của map khôi phục. Hiện scene không thêm vùng tối giả định. Các hệ số bóng tối được kiểm tra bằng fixture có vùng tối thật trong test.

Scene School_Test và prefab đã gắn MonsterVision, mắt và MonsterAI. Scene giữ tham chiếu PlayerMotor, prefab không giữ tham chiếu player ngoài prefab. Chọn boss thấy awareness trên Handles.Label. PlayerLook/PlayerMotor/Flashlight không sửa.

`monster-vision-check.txt`: kiểm tra Play Mode nón/tầm, sau tủ không thấy, lộ đầu vẫn thấy, hệ số ngồi/đèn/tối/kết hợp, đèn inactive, vùng tối bật/tắt, awareness không phát hiện tức thì, mất dấu giảm về0, thấy liên tục mới Chase. Test dùng config clone, mic tắt tạm và khôi phục khi về Edit Mode. Backup trước cấu hình: School_Test-before-monster-vision.unity.

Chưa commit/push.
