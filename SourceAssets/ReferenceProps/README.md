# Bộ 8 vật phẩm dựng theo ảnh tham chiếu

Dự án đích: `RecoveredProjects/TheGhost_School_Unity6` (Unity 6000.5.7f1, Built-in Render Pipeline).

| Ảnh | Model | Vật phẩm đang dùng trong game |
| --- | --- | --- |
| 1 | MetalBlank | metal_blank |
| 2 | GoldKey | main_exit_key |
| 3 | SilverKey | silver_key |
| 4 | SawBlade | saw_blade |
| 5 | SawHandle | saw_handle |
| 6 | Screws | screws |
| 7 | AssembledSaw | assembled_saw |
| 8 | KeyMold | key_frame |

Mesh dựng mới theo hình dáng trong 8 ảnh; những mặt khuất được suy dựng. Có độ dày thực, cạnh vát, lỗ vòng chìa/tay cầm, răng cưa, ren vít xoắn, rãnh đầu vít và khuôn chìa lõm. Texture 512 × 512 được tạo bằng thuật toán, không cần tải asset hay shader ngoài.

## Sử dụng trong Unity

- Mở đúng project `RecoveredProjects/TheGhost_School_Unity6`.
- Các FBX, material và prefab nằm ở `Assets/Recovery/ReferenceProps`.
- Mở `Assets/Recovery/ReferenceProps/Scenes/ReferenceProps_Showcase.unity` để xem cả bộ ở kích thước thật.
- Các prefab nhặt đồ cũ trong `Assets/Recovery/M6/Prefabs` được cập nhật hình ảnh; `ItemData` và GUID/fileID của thành phần nhặt đồ gốc được giữ. Icon cũ được cập nhật tại cùng đường dẫn để UI tiếp tục tham chiếu.
- Không thay đổi gameplay, vị trí vật phẩm hay scene `School_Test`.
- Mesh dùng đơn vị mét, nằm trên mặt phẳng XZ, trục Y hướng lên; đáy prefab nằm ở Y = 0. Collider hộp phục vụ nhặt/thả; collider không mô phỏng các lỗ nhỏ của mesh.
- Prefab có Rigidbody kinematic như cấu hình nhặt đồ hiện tại. Hệ thống inventory/lắp ráp sẵn có quản lý thả vật phẩm.

## Nguồn chỉnh sửa

- `ReferenceProps.blend`: 8 collection độc lập, bố trí cách nhau để dễ chỉnh; texture được đóng gói trong file.
- `Previews/EightProps.png`: ảnh tổng hợp render bằng Blender, mỗi món được phóng vừa khung, không thể hiện tỷ lệ giữa các món.
- `Previews/UnityEightProps.png`: ảnh tổng hợp render thực tế trong Unity, cùng góc nhìn và material của các icon dùng trong game.
- `mesh-report.json`: số tam giác và kích thước từng mẫu.
- `Tools/ReferenceProps/build_reference_props.py`: script dựng lại mesh, texture, FBX và ảnh preview. Chạy bằng Blender `--background --python`.
- Unity menu `Tools > The Ghost > Reference Props > Import and apply eight models`: nhập material, cập nhật prefab/icon và tạo scene trưng bày. Chạy lại sẽ áp dụng lại model sinh từ FBX, do đó hãy giữ riêng các chỉnh sửa thủ công vào prefab.
- `RecoveryReports/ReferenceProps/Before` trong project Unity lưu bản sao các prefab, ItemData, icon và metadata trước lần áp dụng đầu tiên.
- `RecoveryReports/ReferenceProps/validation.txt`: kết quả kiểm tra Unity sau khi nhập.

Các asset mới sử dụng Standard shader của Built-in Render Pipeline. Nếu chuyển sang URP/HDRP, cần chuyển đổi material cho pipeline đó.

## Kiểm tra đã hoàn tất

Unity 6000.5.7f1 biên dịch, nhập FBX/material, tạo prefab/icon và lưu scene trưng bày thành công (exit code 0). Cả 8 vật phẩm đạt kiểm tra mesh, texture, kích thước mét, Collider/Rigidbody và liên kết ItemData/pickupPrefab. SHA-256 của `School_Test.unity` trước/sau giống nhau. Đã xem ảnh render của cả bộ trong Unity. Chưa chạy lại toàn bộ luồng gameplay trong Play Mode.
