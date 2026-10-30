# The Ghost — bản khôi phục map trường học

## Hai bản được giữ riêng

- `TheGhost_Original/ExportedProject`: bản xuất AssetRipper từ XAPK, nhắm Unity 2022.3.62f2. Giữ scene, tài nguyên và kiểu script khôi phục được. Đây không phải mã nguồn gốc; các hàm gameplay IL2CPP được xuất dưới dạng stub.
- `TheGhost_School_Unity6`: bản dùng để xem môi trường trên Unity 6000.5.7f1 đã có trên máy. Có thêm công cụ tạo scene tham quan `Assets/Recovery/Scenes/School_Explore.unity`.

## Mở trong Unity

1. Unity Hub → Add project from disk → chọn thư mục `TheGhost_School_Unity6`.
2. Mở `Assets/Recovery/Scenes/School_Explore.unity`. Nếu chưa có scene này, dùng menu **Recovery → Prepare School Preview**.
3. Nhấn Play, click vào Game view. WASD đi, chuột nhìn, Shift tăng tốc, F chuyển đi bộ/bay, Q/E bay xuống/lên, L đèn pin, R về điểm bắt đầu, Esc thả chuột.

Scene gốc nằm tại `Assets/Scenes/SchoolLevel.unity`. Bản tham quan giữ hình học, bố cục và các liên kết tài nguyên có thể khôi phục; gỡ component script gameplay không còn hoạt động, tắt camera/UI game, thêm camera tham quan và ánh sáng dễ nhìn. Shader riêng của game được thay thế bằng shader Standard khi cần, vì thế các hiệu ứng và màu sắc có thể khác bản Android. Bản xuất gốc ở thư mục riêng không bị sửa.

## Phạm vi

Đã xuất toàn bộ 8.791 mục tài nguyên mà công cụ đọc được từ gói, gồm cả dữ liệu dùng chung và các map khác. Không phải tất cả đều chỉ thuộc map trường học. Không cam kết khôi phục hệ thống multiplayer, AI, nhiệm vụ, hoạt ảnh điều khiển bởi script hoặc shader gốc từ APK.

Kết quả kiểm tra Unity nằm trong `TheGhost_School_Unity6/RecoveryReports/validation.txt` khi bước chuẩn bị hoàn tất. Ảnh kiểm tra được tạo bởi phương thức `SchoolRecovery.Capture`.

Kiểm tra GUID trên bản xuất gốc tìm thấy 1.873 file phụ thuộc trực tiếp/gián tiếp của SchoolLevel; 16 GUID chưa giải quyết thuộc font atlas/UI (không phải mesh hoặc texture môi trường). Danh sách đầy đủ: `../Preview/school-dependency-audit.json`. Kiểm tra này xác nhận liên kết file, không thay thế kiểm tra hình ảnh hoặc gameplay.

Nguồn đầu vào: `The+Ghost+-+Multiplayer+Horror_1.87.15_APKPure.xapk`, package `com.Gameplier.TheGhost`, phiên bản 1.87.15 (426). Công cụ xuất: AssetRipper 2.0.0, https://github.com/AssetRipper/AssetRipper/releases/tag/2.0.0 .
