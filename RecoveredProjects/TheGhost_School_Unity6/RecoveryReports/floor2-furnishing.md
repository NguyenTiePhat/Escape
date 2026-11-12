# Nội thất tầng 2 — 12/11/2026

Đã bổ sung vào `Assets/Recovery/Scenes/School_Test.unity`, nhóm `Floor 2 - XAPK furnishings`.

## Nguồn và bố trí

Kiểm tra trực tiếp XAPK `The+Ghost+-+Multiplayer+Horror_1.87.15_APKPure.xapk`: có APK chính, UnityDataAssetPack.apk và ARM64 split. Dùng tài nguyên đã trích xuất từ gói này trong bản khôi phục gốc. SHA-256 của cả sáu mesh được dùng trùng khớp giữa bản xuất nguyên trạng và dự án hiện tại. Không tải tài nguyên Drive.

- Sảnh phía tây: 3 cụm locker, 1 ghế băng, 1 thùng rác nhỏ.
- Khoảng nghỉ phía bắc: 2 bàn đọc, 4 ghế, 2 chồng sách, 1 thùng rác nhỏ.
- Hành lang dẫn tới thư viện: 2 ghế băng, 1 thùng rác nhỏ.
- Tổng: 17 đối tượng mesh, dùng texture/material có sẵn của trường học. Hai vị trí ghế dự kiến khác bị loại vì chạm hình học cũ.

Thư viện và kho tầng 2 vốn đã có kệ sách, bàn ghế và thùng giấy. Giữ nguyên những vật này. Nội thất bổ sung tập trung vào khoảng trống của hành lang và sảnh. Không thay đổi tường, sàn, cầu thang, cửa, ánh sáng, vật phẩm, nhiệm vụ, player, boss hoặc dữ liệu NavMesh. Vật cản điều hướng chỉ được gắn lên nội thất mới.

## Xác minh

- Unity 6000.5.7f1 biên dịch, lưu và mở lại scene thành công; 17 mesh/material/texture đều hợp lệ.
- So sánh trước khi lưu: 16.435 đối tượng/component cũ không đổi.
- So sánh YAML sau khi lưu: tất cả 16.344 khối dữ liệu cũ ngoài SceneRoots giữ nguyên nội dung; SceneRoots chỉ thêm một root mới. Unity có sắp xếp lại vị trí một số khối trong file.
- 10 điểm đến vốn đi được vẫn kết nối sau khi thêm, với kiểm tra capsule bán kính 0,34 m.
- CharacterController di chuyển thực tế trong kiểm tra Editor qua ba tuyến: hành lang tây–đông, lối vào góc đọc và nhánh hành lang phía nam đều đạt.
- Vị trí mới được kiểm tra sàn đỡ ở bốn góc và không đè lên collider hiện có. Ảnh kiểm tra đọc sách, tủ và hành lang đã được xem.
- Ba điểm mẫu sâu trong thư viện không kết nối ngay từ trước thay đổi. Không sửa phần va chạm cũ đó trong công việc này. Chưa kiểm thử chơi toàn bộ map hoặc boss trên tầng 2.

Chi tiết: `floor2-furnishing-check.txt`. Ảnh sử dụng đèn kiểm tra tạm thời, không lưu đèn này vào scene.

Bản sao trước thay đổi: `School_Test-before-floor2-furnishing.unity`. Công cụ `FloorTwoFurnishing.Build` từ chối tạo trùng nếu nhóm nội thất đã tồn tại.
