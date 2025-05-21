# 🏰 Tower of Rings

Một trò chơi sắp xếp vòng (ring sorting) với giao diện đồ họa sử dụng thư viện **Pygame**, kết hợp với thuật toán **A\*** để tự động tìm lời giải. Đây là một dự án trực quan giúp hiểu sâu hơn về cách hoạt động của các thuật toán tìm kiếm trong AI.

---

## 📌 Giới thiệu

**Tower of Rings** mô phỏng một trò chơi sắp xếp các vòng màu từ 6 cột ban đầu, trong đó mục tiêu là di chuyển các vòng sao cho cuối cùng có 4 cột, mỗi cột chứa 4 vòng cùng màu, và 2 cột còn lại trống.

Bạn có thể:
- Tự chơi bằng cách kéo thả các vòng.
- Nhấn nút **Solve** để thuật toán A* tự động tìm lời giải.

---

## 🧠 Tính năng nổi bật

- 🎨 Giao diện trực quan bằng `pygame`
- 🔍 Tự động giải bằng thuật toán A*
- 🖱️ Kéo thả vòng bằng chuột
- 🔄 Nút Reset để chơi lại từ đầu
- 🤖 Nút Solve để chạy thuật toán tìm lời giải
- 📊 Hiển thị số bước đã đi và giới hạn bước

---

## 🚀 Cách chạy

### ⚙️ Yêu cầu

- Python 3.x
- Thư viện `pygame`

Cài đặt pygame nếu chưa có:

```bash
pip install pygame
```
▶️ Chạy chương trình
```bash
python tower_of_rings.py
```
🎮 Luật chơi
Mỗi lần chỉ được di chuyển 1 nhóm vòng cùng màu đang nằm ở đỉnh 1 cột.

Không được vượt quá 4 vòng trong 1 cột.

Mục tiêu: tạo ra 4 cột, mỗi cột gồm 4 vòng cùng màu.

Bạn có thể nhấn Solve để máy tự giải bằng thuật toán A*.

Số bước tối đa: 50 bước. Vượt quá số bước này, bạn sẽ thua.

📸 Giao diện minh họa
![image](https://github.com/user-attachments/assets/5b91d8e0-cc78-44aa-aec8-2a015ac45168)


⚙️ Thuật toán A*
Trạng thái (state): danh sách gồm 6 cột, mỗi cột chứa 0–4 vòng màu.

Hàm heuristic: số cột chưa đạt trạng thái hoàn chỉnh.

Successors: sinh các trạng thái mới bằng cách di chuyển vòng hợp lệ.

Điều kiện kết thúc: có 4 cột hoàn chỉnh và 2 cột trống.
🗂️ Cấu trúc file (gợi ý)

      tower_of_rings/
      ├── tower_of_rings.py         # File chính chạy game
      ├── README.md                 # Tài liệu hướng dẫn
      ├── requirements.txt          # (Tuỳ chọn) Liệt kê thư viện
📄 Giấy phép
Dự án được phát triển cho mục đích học tập. Bạn có thể sử dụng, chỉnh sửa và chia sẻ tự do.

👨‍💻 Tác giả
Tên đề tài: Tower of Rings

Người thực hiện: Quang Trường

Liên hệ: https://github.com/2bllikigai

🎯 Chúc bạn chơi vui và hiểu sâu hơn về thuật toán A!*
