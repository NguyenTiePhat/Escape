# M4: Chase, mất dấu và Search

- Chase dùng chaseSpeed, cập nhật đích mỗi 0.2 giây. Chỉ ghi nhận vị trí khi nhìn thấy hoặc nhận được tiếng ồn, không theo dõi vị trí sống của người đang khuất.
- Mất thị giác hơn 1.5 giây: Search quanh LastKnownPosition. Tiếng ồn không kéo dài bộ đếm mất thị giác.
- Tự tìm các PlayerMotor đang tồn tại mỗi 0.5 giây. Ưu tiên người đang thấy gần nhất; giữ mục tiêu ít nhất 0.75 giây và chỉ đổi khi người khác gần hơn ít nhất 25%. Khi mục tiêu khuất, có thể chọn người khác đang thấy ngay. Nếu không thấy ai, chọn người phát tiếng ồn lớn nhất còn được ghi nhớ trong 2 giây.
- Khi bắt đầu Chase phát roarClip qua AudioSource; tự lấy âm thanh từ BossPrototypeMotion đã có trên boss. MonsterConfig.roarCooldown mặc định 6 giây. Kết thúc cú vồ không phát lại tiếng gầm.
- Search đến vị trí cuối, rồi đi qua 3–5 điểm NavMesh ngẫu nhiên trong bán kính 8m, chỉ nhận đường đi hoàn chỉnh. Mỗi điểm dừng nhìn quanh 1.2 giây. Khu vực quá hẹp hoặc không liên thông có thể tạo ít điểm hơn; không cố đi xuyên tường để đủ số lượng.
- Hết searchDuration quay về Patrol, dù chưa đi hết các điểm. Có thể tăng searchDuration nếu muốn boss tìm lâu hơn với patrolSpeed thấp.
- Thấy người trong Search: Chase ngay. Nghe tiếng ồn vượt hearingThreshold: Investigate. Investigate đến nơi nhìn quanh 3 giây rồi bắt đầu Search thật.
- Giữ nguyên cú vồ và tốc độ animation đã chỉnh trước đó. Không thay PlayerLook, scene hay rig.

Kiểm tra Play Mode: xem monster-chase-search-check.txt. Test dùng scene/NavMesh riêng, không bật micro.
