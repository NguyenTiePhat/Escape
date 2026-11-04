# Boss nhanh và vồ gấp hơn

Chỉnh ngày 04/11/2026: MonsterConfig.chaseSpeed 1.3 → 4.8 m/s, tốc độ lao mục tiêu 6 m/s, agent acceleration18 và angularSpeed240. Run Animator speed1.75, Lunge1.6, Investigate1.2, Alert1.25; blend từ.12 xuống.07s. Các clip nguồn và skin weights không đổi; độ lanh lẹ đến từ tăng nhịp phát animation.

MonsterAI giờ thực sự di chuyển khi Chase tới vị trí đang nhìn thấy, hoặc vị trí cuối đã thấy. Nếu mất dấu và đến điểm cuối thì quay Patrol tạm. Trong2m và thấy mục tiêu, phát Lunge: thu người.1875s, lao.25s, hết động tác ở.625s, cooldown.65s. Chỉ lao khi đường thẳng trên NavMesh không bị chặn; không gây sát thương ở bước này. Đây là hành vi cơ bản, chưa phải Search/Attack đầy đủ.

Scene/prefab/config/controller được lưu. Script prototype cũng đồng bộ runSpeed4.8/lungeSpeed6 và thời gian clip1.6x nhưng vẫn tắt khi AI điều khiển.

Test `boss-aggression-check.txt`: Play Mode thật trong School_Test, chạy tới mục tiêu, phát Lunge, hồi phục đúng thời lượng và kiểm tra capsule không xuyên collider trên tuyến test. Giá trị speed là tốc độ mục tiêu của NavMeshAgent, không phải giới hạn cứng của vận tốc tức thời khi avoidance/correction hoạt động. Chưa kiểm thử toàn bộ map. Backup `School_Test-before-faster-boss.unity`. Chưa commit/push.
