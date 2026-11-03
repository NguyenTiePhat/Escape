# Creature animations

Nhân vật rig mới và 7 clip FBX, 30 fps. Các file FBX gồm mesh và rig, dùng Animation Type **Generic**. Chưa gắn nhân vật vào scene hoặc AI hiện có.

| Clip | Giây | Loop Time |
| --- | --- | --- |
| Idle | 4 | Bật |
| Walk | 2.4 | Bật |
| Alert | 1 | Tắt |
| Investigate | 3.2 | Bật |
| Run | 0.8 | Bật |
| Lunge | 1 | Tắt |
| Search | 4 | Bật |

Trong Unity, chọn từng FBX > Animation, đặt Loop Time theo bảng và Apply. Walk, Investigate và Run chạy tại chỗ. Lunge dịch chuyển root; chọn điều khiển vị trí bằng root motion hoặc gameplay, tránh cộng cả hai. Khoảng kích hoạt vồ 2 m và hướng nguồn âm thanh phải được nối với AI trong game.

Nguồn Blender, GIF xem trước, hướng dẫn và báo cáo kiểm tra FBX nằm tại `ArtSource/Creature` ở gốc repository. Model giữ kích thước gốc, cao khoảng 1.019 đơn vị; chỉnh scale phù hợp game. Mỗi FBX đã được nhập lại trong Blender để kiểm tra mesh, rig và thời lượng; chưa kiểm thử trong Unity của dự án.
