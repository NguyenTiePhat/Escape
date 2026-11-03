# M3 — Micro, cài đặt và HUD tiếng ồn

Đã tích hợp vào `Assets/Recovery/Scenes/School_Test.unity`.

## Sử dụng

- F2 mở bảng micro, F2/Esc đóng; khi mở bảng, chuột được thả và thao tác nhìn/di chuyển bị chặn.
- Chọn micro hoặc mặc định hệ thống, chỉnh Độ nhạy 0.5–2. Thanh đầu vào hiển thị mức thu trực tiếp.
- Bấm Hiệu chỉnh: im lặng 3 giây, rồi nói bình thường 3 giây theo hướng dẫn. Giọng bình thường được đặt khoảng 60%.
- Nút Tắt phát hiện giọng nói dừng thu. Thiết bị, bật/tắt, độ nhạy và các ngưỡng được lưu bằng PlayerPrefs, tải khi mở game.
- HUD trên giữa màn hình lấy tiếng chân/giọng nói cao nhất của chính Player trong 0.5 giây gần nhất. Xanh dưới 0.25, vàng dưới 0.65, đỏ từ 0.65; thanh hạ mượt 1.5 đơn vị/giây khi mức giảm.

## Cách hoạt động

`MicrophoneNoiseDetector` thu loop clip 1 giây ở 16 kHz, đọc tối đa 800 mẫu mới mỗi 50 ms. RMS chuyển thành dBFS, làm mượt attack 0.035 giây và release 0.35 giây. Đây là mức tín hiệu số, không phải dB SPL đã hiệu chuẩn ngoài đời.

Mặc định noiseFloorDb=-50, maxDb=-10, sensitivity=1. Khi tín hiệu thô vượt nền, phát Voice tối đa mỗi 0.2 giây tại Player, loudness chuẩn hóa và giới hạn 0–1. Cổng tín hiệu thô ngăn đuôi release tiếp tục phát tiếng ồn khi đầu vào đã trở về nền.

Hiệu chỉnh dùng percentile 90 của nền cộng 6 dB và percentile 70 của giọng nói. Nếu giọng không tách đủ khỏi nền, giữ cấu hình cũ. Trong lúc hiệu chỉnh không phát Voice; mất micro sẽ hủy hiệu chỉnh an toàn.

Không có thiết bị, từ chối quyền, API lỗi, clip không hợp lệ, mất thiết bị hoặc đầu thu ngừng cập nhật đều được xử lý và cảnh báo. Kiểm tra danh sách thiết bị mỗi giây; thử nối lại khi thiết bị phù hợp xuất hiện. Không phát lại, ghi file hoặc gửi âm thanh micro qua mạng.

`NoiseMeterUI` lắng nghe `NoiseManager.OnNoiseEmitted`, chỉ nhận Footstep/Voice đúng owner. Coroutine cập nhật 20 Hz chỉ chạy khi còn mẫu hoặc thanh đang hạ; không polling mỗi frame lúc yên lặng. `PlayerLook` không thay đổi.

## Kiểm chứng ngày 03/11/2026

- `mic-noise-check.txt`: PASS — vòng đệm/mẫu mới, RMS, giọng nhỏ/to, attack/release, giới hạn tần suất, các lỗi thiết bị giả lập, hiệu chỉnh thực thi đủ 3+3 giây, hủy khi mất micro, lọc nền và HUD.
- `mic-prefs-restart-check.txt`: PASS — ghi và đọc cài đặt từ hai tiến trình Unity độc lập; xóa khóa test sau kiểm tra.
- `mic-visual-check.txt`: PASS — mở bảng trong School_Test, thả chuột/chặn điều khiển, mức đầu vào giả lập. Đã xem `mic-settings-preview.png` và `noise-meter-preview.png`.
- Chưa kiểm thử nói, hét, tiếng quạt, hộp thoại quyền hệ điều hành hoặc rút micro vật lý. Các bài test âm thanh dùng backend giả lập, không thu micro thật.

Scene sao lưu: `School_Test-before-microphone.unity`. Thay đổi hiện nằm trong workspace, chưa commit/push.
