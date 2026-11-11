# M6 bước 2–3: vật phẩm và máy làm chìa

Đã chốt với người dùng: không dầu bôi trơn; tạo model phù hợp trường học cũ; tự chọn phòng/vị trí; máy 10 giây, loudness 0.9; cần giữ nguyên liệu trong người, bỏ nguyên liệu mất tiến độ; chìa hoàn thành để trên máy cho người chơi nhặt. Người dùng đồng ý sửa NavMesh để boss đi được tới máy.

## Trong School_Test

Máy `M6 Key Cutting Machine` đặt trong phòng phía tây, gần bàn làm việc và bình chữa cháy, tọa độ (14.51, 1.99, 53.76). Khay nhận chìa ở phía trước bên phải máy. Giữ E khi đủ khung in và phôi, thanh donut đầy sau 10 giây. Thả E/nhìn đi chỗ khác/bị đánh vẫn giữ tiến độ nếu còn nguyên liệu; bỏ một nguyên liệu thì mất tiến độ và cần nhả E trước khi làm lại. Hoàn thành tiêu hao đúng một khung và một phôi, tạo đúng một chìa khóa cửa chính trên khay. Túi đầy không làm mất chìa. Chưa nối chìa này với cửa thoát (bước 4).

Mỗi lượt Play tạo đúng một món mỗi loại, chọn ngẫu nhiên trong 3 điểm đã kiểm tra ở khu tương ứng:
- Khung in: khu bàn phía tây nam, x10–12, z52–57.
- Phôi kim loại: khu bàn sát phía tây, x3–4, z54–55.
- Lưỡi cưa: khu bàn phía tây bắc, x3–5, z66–70.
- Tay cầm: khu bàn/tủ phía tây giữa, x2–3, z58–60.
- Ốc vít: khu bàn phía bắc, x13–14, z67–71.

Các Transform điểm đặt có thể chỉnh ở `M6 Escape Items`. Chi tiết tọa độ: m6-items-setup.txt. Các lần chơi có thể tình cờ lặp lại điểm do chọn ngẫu nhiên; không tạo trùng món hoặc trùng điểm trong một lượt. Chưa làm lắp cưa, cưa gỗ hoặc thắng game.

## Model và vật liệu

FBX gồm khung in có lòng khuôn, phôi thép, lưỡi cưa có răng thật, tay cầm gỗ có lỗ nắm, bộ vít và chìa thành phẩm. Máy gồm bàn gỗ, thân sơn cũ, trục/lưỡi cắt, hai ê tô, núm vặn và khay. Dùng texture vết cũ, màu thép xỉn/gỗ sẫm và độ bóng thấp; shader Standard khớp pipeline Built-in thực tế của project. Không chuyển pipeline.

Assets: Assets/Recovery/M6 (Models, Materials, Items, Prefabs, Icons). Nguồn sửa model: SourceAssets/M6/M6Props.blend; script tái tạo: Tools/M6/build_props.py.

## Logic và NavMesh

HoldNoiseAction thêm IHoldActionGate và ResetProgress, giữ hành vi bước 1 cho vật thể không có recipe. KeyCraftingStation theo dõi Inventory, kiểm tra nguyên liệu mỗi lúc làm, reset khi số nguyên liệu giảm, tiêu hao nguyên liệu bằng RemoveRecipe nguyên tử rồi tạo PickupItem trên khay. EscapeItemSpawner chọn trọn bộ vị trí trước khi tạo đồ, có kiểm tra trùng và chỉ chạy một lần mỗi instance.

NavMesh mới SchoolConnectedNavMesh.asset được bake từ collider hiện có, loại người chơi/boss, vật nhặt và cánh cửa động. Các collider phục hồi sai tọa độ hàng triệu mét chỉ bị loại khỏi nguồn bake, không bị xóa khỏi scene. Giữ tường thật. Khu phía đông đang bị tường kín chặn nên không sử dụng cho M6 lần này. Bản NavMesh cũ và scene backup được giữ. Boss đã được test đi thực tế từ vị trí xuất phát tới nguồn tiếng ồn ở mặt trước máy.

## Kiểm tra

m6-craft-check.txt: PASS Play Mode trong School_Test; thiếu từng nguyên liệu; giữ/nhả; Drop thật reset; không vượt qua ngắt khi vẫn giữ E; túi đầy; trừ đúng hai nguyên liệu; chìa trên khay; nhặt sau khi có ô trống; không tạo thêm chìa; 24 lượt spawn kiểm tra số lượng/vùng/trùng; icon và mesh; boss nghe và tới máy.

m6-navigation-check.txt: PASS đường đi từ boss tới phòng bắt đầu và các khu M6. Test di chuyển dùng bản sao config với patrolSpeed 3 để rút thời gian; asset gameplay không đổi. Micro bị tắt riêng trong phiên test, scene được tải lại từ bản lưu sau test. Đã xem trực quan icon và máy trong phòng. Dọn script/log/ảnh test tạm, giữ report và công cụ setup có thể dùng lại.
