# M6 bước 1 — hành động giữ phím gây ồn

Theo các lựa chọn đã chốt: giữ E khi nhìn vào vật thể; khóa di chuyển ngang nhưng vẫn dùng C để ngồi/đứng; nhả E, nhìn đi chỗ khác, bị đẩy ra khỏi vị trí, mất phạm vi tương tác hoặc bị boss đánh đều dừng và giữ tiến độ. Sau khi bị đánh cần nhả E rồi giữ lại. Phạm vi dùng PlayerInteractor hiện có. Không sửa PlayerLook.

School_Test có vật thể M6 Hold Action Test ở bên trái bàn hướng dẫn trong phòng bắt đầu, vị trí (20.70, 2.66, 61.78). Thời gian thử 10 giây; độ ồn 0.85. Vòng donut 64px quanh tâm ngắm, chạy đủ một vòng là hoàn thành, không có tên hành động. Vật thể thử chỉ hoàn thành một lần mỗi lượt Play; chưa gắn chế tạo, tiêu hao vật phẩm hoặc mở lối.

HoldNoiseAction lưu tiến độ trên từng vật thể, có duration/loudness và UnityEvent onCompleted để các bước sau dùng lại. NoiseSource.Action được thêm ở cuối enum để giữ nguyên giá trị nguồn cũ. Khi làm, phát xung tiếng ồn mỗi 0.1s tại vị trí vật thể; không phát khi dừng hoặc hoàn thành. Đây là tín hiệu cho hệ thống thính giác boss; chưa có clip âm thanh máy/cưa vì hai vật thể đó thuộc bước sau.

PlayerHoldAction quản lý input và khóa di chuyển, nhận event hit của MonsterAI. HoldProgressRing vẽ vòng donut bằng UI mesh, không cần texture. HoldActionSetup là công cụ gắn lại scene có kiểm tra trùng và kiểm tra chỗ đặt.

Test Play Mode PASS: m6-hold-check.txt. Đã kiểm tra raycast, giữ/nhả, quay mặt, crouch/stand, không đi/chạy khi làm, hit ngắt, phải nhả rồi giữ lại sau hit, bị dịch chuyển, ngoài phạm vi, hoàn thành một lần, tiếng ồn và MonsterAI Investigate. Ảnh render donut đã được kiểm tra trực quan. Các script/log/ảnh test tạm được dọn; giữ vật thể test theo yêu cầu người dùng.
