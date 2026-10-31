# Escape — The Ghost

## Bản trường học đang chỉnh sửa

### Mở và chạy game trong Unity

1. Cài **Unity Hub** và Unity Editor **6000.5.7f1** (phiên bản đang dùng của dự án).
2. Cài Git và Git LFS, rồi tải dự án về máy (dùng clone để lấy đủ các file lớn qua LFS):

   ```powershell
   git lfs install
   git clone https://github.com/NguyenTiePhat/Escape.git
   cd Escape
   git lfs pull
   ```

   Nếu đã có dự án trên máy thì bỏ qua bước tải lại.
3. Mở **Unity Hub → Projects → Add → Add project from disk**, chọn thư mục `Escape/RecoveredProjects/TheGhost_School_Unity6`. Trên máy hiện tại, đường dẫn là `D:/UnityProjects/theghost/RecoveredProjects/TheGhost_School_Unity6`. Chọn đúng thư mục này; mô hình và dự án blockout IUH cũ đã được xóa.
4. Bấm vào dự án trong Unity Hub để mở bằng **6000.5.7f1**. Lần đầu cần đợi Unity import tài nguyên và biên dịch script; dữ liệu khá lớn nên có thể mất vài phút hoặc lâu hơn tùy máy.
5. Trong cửa sổ **Project** của Unity, vào `Assets → Recovery → Scenes`, nhấp đúp **School_Test** (`School_Test.unity`).
6. Bấm nút **▶ Play** ở thanh công cụ phía trên. Chọn tab **Game**, rồi click vào hình game để điều khiển nhân vật.
7. Bấm **▶ Play** lần nữa để dừng. Các chỉnh sửa thực hiện khi đang Play thường không được giữ lại sau khi dừng; hãy dừng Play trước khi chỉnh sửa và lưu scene.

### Điều khiển bản trường học

| Phím | Chức năng |
| --- | --- |
| W / A / S / D | Di chuyển |
| Chuột | Nhìn xung quanh |
| Shift | Chạy 6 m/s, tiêu hao thể lực |
| C | Ngồi / đứng (nhấn để đổi trạng thái) |
| 1–5 / Cuộn chuột | Chọn ô trên thanh vật phẩm (tối đa 5 ô) |
| E | Nhặt vật đang nhìn ở gần; nếu không nhắm vật nhặt, sử dụng món đang cầm (bật/tắt đèn pin hoặc đọc giấy) |
| F | Mở/đóng cửa khi đứng gần (tối đa 2,2 m) |
| L | Bật/tắt đèn pin khi đang cầm đèn |
| R | Trở về điểm bắt đầu |
| Esc | Thả con trỏ chuột |

Nếu Unity bị lỗi đồ họa DX12 trên Windows, đóng Editor và chạy `Open-Unity-DX11.cmd` trong thư mục dự án trường học. File này dùng đường dẫn Unity trên máy hiện tại; sửa đường dẫn trong file cho phù hợp nếu mở trên máy khác.

Đây là bản thử nghiệm môi trường và tương tác phòng bắt đầu, chưa phải game hoàn chỉnh; gameplay gốc như multiplayer và AI chưa được phục hồi.

## Dữ liệu có trong repository

Để làm việc nhóm, mở trực tiếp `RecoveredProjects/TheGhost_School_Unity6`: `Assets` (kèm `.meta`), `Packages` và `ProjectSettings` là dữ liệu cần giữ đồng bộ. Không cần chạy AssetRipper hoặc giải nén APK để mở dự án này; các thư mục nguồn/khôi phục khác được giữ để sao lưu và đối chiếu.

## Làm việc nhóm

1. Dùng cùng Unity **6000.5.7f1**. Trước khi làm, lưu/commit thay đổi đang có, rồi cập nhật bằng `git pull --ff-only` và `git lfs pull`.
2. Mỗi người tạo nhánh riêng, ví dụ `git switch -c feature/ten-cong-viec`, và mở đúng scene `Assets/Recovery/Scenes/School_Test.unity`.
3. Di chuyển/đổi tên asset trong cửa sổ Project của Unity để giữ liên kết `.meta`. Commit cả asset và `.meta`; lưu scene trước khi commit.
4. Tránh cùng sửa một scene hoặc prefab trong cùng thời điểm. Thống nhất người phụ trách hoặc tách công việc sang scene/prefab riêng.
5. Sau khi kiểm tra trong Unity, chạy `git add .`, `git commit -m "Describe changes"`, rồi `git push -u origin HEAD`. Tạo Pull Request trên GitHub để nhóm xem và gộp vào `main`.

## Nội dung lưu trữ bổ sung

- `RecoveredProjects/TheGhost_School_Unity6`: toàn bộ dự án Unity6 đang chỉnh sửa, gồm asset, scene, script, cấu hình và `RecoveryReports` (báo cáo, ảnh kiểm tra, scene sao lưu).
- `RecoveredProjects/TheGhost_Original`: bản xuất nguyên trạng cùng `AuxiliaryFiles` để đối chiếu và phục hồi.
- `ImportSource`: dữ liệu giải nén, các APK thành phần và dữ liệu Unity đầu vào.
- `Tools`: công cụ AssetRipper và script kiểm tra dữ liệu.
- `Preview`: các báo cáo, ảnh và log còn lại của quá trình khôi phục.
- `RESUME.md`: lịch sử công việc và các giới hạn đã ghi nhận.

Chỉ bỏ qua cache/trạng thái máy (`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `.vs`, `.idea`) và file thông tin đăng nhập riêng nếu có. Unity tạo lại cache khi mở dự án. Thư mục trống không được Git lưu. Mô hình IUH, dự án Unity gốc của IUH, ảnh render và script dựng IUH đã được xóa khỏi phiên bản hiện tại; lịch sử commit cũ vẫn được giữ.

Gói XAPK đầu vào được giữ riêng trên máy, không nằm trong phiên bản hiện tại của repository. Các file APK, asset bundle, Unity3D và EXE dùng **Git LFS**. Sau khi clone hoặc cập nhật repo, chạy `git lfs pull` để lấy nội dung đầy đủ. Repo không khẳng định đã khôi phục đầy đủ gameplay gốc.


