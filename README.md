# Support Span Check cho AutoCAD Plant 3D

**Support Span Check** là một plugin (add-on) mạnh mẽ dành riêng cho phần mềm **AutoCAD Plant 3D**. Công cụ này tự động hóa hoàn toàn quy trình kiểm tra khoảng cách (nhịp) giữa các gối đỡ (Support) trên đường ống, giúp kỹ sư phát hiện nhanh các vị trí vi phạm tiêu chuẩn thiết kế.

---

## 📥 Hướng dẫn cài đặt và sử dụng

### 1. Tải và nạp Tool (Load Plugin)
1. Trong màn hình AutoCAD Plant 3D, gõ lệnh `NETLOAD` vào dòng lệnh (Command line).
2. Tìm đến file đã biên dịch: `SupportSpanCheck_v19.dll` (hoặc phiên bản mới nhất) trong thư mục `bin\Debug\`.
3. Bấm Open để nạp plugin vào phần mềm.

### 2. Khởi chạy và Quét Support
1. Sau khi `NETLOAD` thành công, gõ lệnh `CHECKSPAN` vào dòng lệnh AutoCAD.
2. Giao diện **SUPPORT SPAN CHECK** sẽ xuất hiện.
3. Bấm nút **Quét / Check Supports**. Tool sẽ quét toàn bộ mô hình 3D và liệt kê tất cả các nhịp Support ra màn hình.

### 3. Đọc hiểu giao diện
Tool cung cấp 2 chế độ hiển thị:
- **ALL:** Xem toàn bộ các nhịp đã quét được.
- **Vượt quá giới hạn:** Lọc nhanh và chỉ hiển thị các đoạn nhịp vi phạm khoảng cách tiêu chuẩn (khoảng cách thực tế bị bôi đỏ).

Các cột thông tin chính:
- **Line Number:** Tên tuyến ống (Process Line).
- **Size:** Kích thước danh định của ống.
- **Insulation Thickness:** Độ dày bọc bảo ôn (nếu có).
- **Khoảng cách thực tế:** Khoảng cách đo được giữa 2 Support trên mô hình.
- **Giới hạn:** Khoảng cách tối đa cho phép theo tiêu chuẩn đã cấu hình.

### 4. Định vị Support trên bản vẽ
- Chỉ cần **nháy đúp chuột (Double-click)** vào bất kỳ hàng nào trên bảng, AutoCAD sẽ tự động xoay góc nhìn và Zoom sát vào 2 Support tương ứng để bạn kiểm tra và xử lý.

### 5. Cấu hình tiêu chuẩn dự án
- Chuyển sang Tab **Tiêu chuẩn nhịp (Standards)**.
- Tại đây, bạn có thể thiết lập cấu hình:
  - **Size (mm):** Kích thước ống.
  - **Khoảng cách tối đa:** Dành cho ống trần không bọc bảo ôn.
  - **Khoảng cách tối đa (INSULATION):** Dành cho ống có bọc bảo ôn.

---

## 🛠 Yêu cầu hệ thống
- Nền tảng: AutoCAD Plant 3D 2027.
- Framework: .NET 10.

---
*Ghi chú: Toàn bộ mã nguồn của công cụ này được phát triển 100% bằng AI, xuất phát từ ý tưởng và trải nghiệm thực tế của người dùng.*
