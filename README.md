# The Ghost — IUH abandoned campus

## Bản trường học đang chỉnh sửa

Mở thư mục `RecoveredProjects/TheGhost_School_Unity6` bằng Unity **6000.5.7f1**, sau đó mở scene `Assets/Recovery/Scenes/School_Test.unity`. Trên máy gặp lỗi đồ họa DX12, dùng `Open-Unity-DX11.cmd` trong thư mục dự án này (kiểm tra đường dẫn Unity trong file nếu dùng máy khác).

Repository chứa mã nguồn, scene và tài nguyên của bản Unity6. Unity sẽ tạo lại Library khi mở lần đầu. Gói APK/XAPK, dữ liệu giải nén, bản xuất nguyên trạng và báo cáo tạm không được đưa vào Git. Dự án ở thư mục gốc là bản blockout IUH riêng, được mô tả bên dưới.

Bản blockout 3D cho Unity PC, lấy cảm hứng từ IUH. Đây là khối giảng đường hư cấu hai tầng, không mô phỏng chính xác trường thật. Phạm vi hiện tại: môi trường và đi bộ góc nhìn thứ nhất; chưa có quái vật, jumpscare hoặc nhiệm vụ nhặt đồ.

## Mở bản đồ

1. Unity Hub → Add project from disk → chọn thư mục này. Unity đã tìm thấy trên máy: **6000.5.7f1**. Dùng Built-in Render Pipeline.
2. Mở scene có sẵn `Assets/Scenes/IUH_Preview.unity`. Khi cần dựng lại, chọn menu **The Ghost → Build IUH Preview** để tạo scene, vật liệu và nhân vật từ dữ liệu.
3. Nhấn Play rồi click vào Game view. WASD di chuyển, chuột nhìn, Shift chạy, F đèn pin, L đổi sáng, Esc thả chuột, R về sân.
4. Nếu dự án được mở bằng cấu hình Input System mới, đặt Player → Active Input Handling thành **Input Manager (Old)** hoặc **Both**.

Menu Build sẽ dựng lại scene từ dữ liệu; lưu thay đổi thủ công sang scene khác trước khi dựng lại.

## Bố cục và định hướng

- Tầng 1 (cốt 0 m): A101/A102 giảng đường, A103 y tế, A104 giáo vụ, A105 kho, A106 vệ sinh.
- Tầng 2 (cốt 4,2 m): A201 giảng đường, A202 thư viện, A203 máy tính, A204 thí nghiệm, A205 kỹ thuật, A206 vệ sinh.
- Hành lang rộng 4 m, cửa rộng 2 m, hai cầu thang rộng 3,2 m, bậc cao 0,2 m. Hai cầu thang dẫn ra sân; các phòng mở trực tiếp ra hành lang.
- Cây, ghế đá, nhà bảo vệ, hàng rào, cổng phụ, mái che xe và cột cờ tạo bối cảnh. Nhà bảo vệ là khối ngoại cảnh, chưa có nội thất.
- Mảng xanh lam, tường bạc màu, vệt ẩm, mảnh vụn và nội thất giản lược. Đây là blockout có màu, chưa phải đồ họa hiện thực hoặc asset hoàn thiện.

## Ảnh xem trước

`Preview/01-courtyard.png`, `02-gallery.png`, `03-classroom.png` được render bằng Blender từ cùng dữ liệu hình học xuất cho Unity. Ánh sáng và vật liệu giữa hai phần mềm có thể khác. File Blender: `Preview/IUH-campus.blend`.

Nguồn tham khảo kiến trúc: https://iuh.edu.vn/ và https://iuh.edu.vn/vi/co-so-vat-chat.html . Các mô hình trong dự án được dựng bằng hình học thủ tục, không trích xuất asset của IUH hoặc Panicore.

## Tạo lại dữ liệu / ảnh

Chạy Blender ở chế độ background với `--python Tools/build_campus.py`. Script tạo `Assets/Resources/campus.json` và ảnh xem trước. Sau đó chạy menu Build trong Unity. Kích thước dùng mét.

Chưa tối ưu mesh batching, light bake, LOD hoặc occlusion culling. Bản thử hiện tại dành cho duyệt bố cục và hướng mỹ thuật.

## Kiểm tra đã thực hiện

- Blender đã tạo 1.518 thành phần hình học và ba ảnh render; đã xem ảnh, chỉnh lại ánh sáng nội thất.
- Unity 6000.5.7f1 đã biên dịch C# và chạy CampusBuilder.Build thành công, lưu scene cùng vật liệu; batchmode kết thúc với mã 0.
- Chưa kiểm thử trực tiếp di chuyển trong Play Mode hoặc build bản Windows standalone.
