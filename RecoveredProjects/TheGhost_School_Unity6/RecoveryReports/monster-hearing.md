# M4 phần 2 — Thính giác và Investigate

MonsterAI implement INoiseListener, đăng ký khi OnEnable và hủy đăng ký khi OnDisable. Không cần gắn thêm component vào scene/prefab đã có MonsterAI.

- Nhận sự kiện từ NoiseManager: chỉ chấp nhận perceivedLoudness lớn hơn hearingThreshold trong MonsterConfig. So sánh mức nghe được sau suy giảm khoảng cách, không so loudness gốc.
- Patrol hoặc Search chuyển sang Investigate. Lưu LastNoisePosition và độ lớn mục tiêu; tiếng mới lớn hơn thay thế mục tiêu, tiếng nhỏ hơn hoặc bằng bị bỏ qua, kể cả trong lúc nhìn quanh.
- Đi bằng NavMeshAgent, phát animation Investigate. Với nguồn ở ngang đầu/cửa, tìm điểm NavMesh dưới nguồn trong phạm vi 2.5m và sai lệch ngang tối đa .6m. Chỉ nhận đường hoàn chỉnh; tiếng không tới được không thay thế mục tiêu hợp lệ đang có. Không dịch chuyển xuyên tường.
- Đến đích theo stoppingDistance của agent, dừng hoàn toàn và phát animation Search đúng 3 giây. Sau đó đi qua state Search rồi tạm quay Patrol ở frame kế tiếp. Search đầy đủ sẽ làm ở phần sau.
- Xóa mức ưu tiên tiếng cũ khi điều tra xong. Nếu bị ngắt khi đang đi tuần, trở lại waypoint đang đi dở; nếu ngắt lúc chờ thì tiếp tục waypoint kế tiếp.
- Bỏ qua tiếng do chính boss hoặc con của boss phát, giá trị không hợp lệ, tiếng trong trạng thái Chase/Attack. Những trạng thái đó chưa được triển khai ở bước này.
- Handles.Label khi chọn boss hiển thị Investigate và thời gian nhìn quanh còn lại.

## Kiểm tra

`monster-hearing-check.txt`: PASS Play Mode thực, qua NoiseManager. Bao gồm tiếng xa, dưới/bằng ngưỡng, đổi ngưỡng trực tiếp, listener disable/re-enable, ưu tiên tiếng mới lớn hơn, so perceived thay vì raw loudness, tiếng bằng/nhỏ bị bỏ qua, nguồn cao được chiếu xuống sàn, nguồn không tới được, ngắt nhìn quanh bằng tiếng lớn hơn, đứng yên đủ 3 giây, Search nhận tiếng mới, quay Patrol và đường vòng qua tường cả hai chiều. Boss trong School_Test cũng đi đến đích và phát Investigate/Search đúng.

Đã sửa thứ tự dừng agent: ResetPath trước, isStopped=true và velocity=0 sau, tránh còn chuyển động khi bắt đầu nhìn quanh. Bộ kiểm tra dùng config clone, tắt micro tạm thời, khôi phục khi kết thúc; không thay config người dùng.

Chưa commit/push.
