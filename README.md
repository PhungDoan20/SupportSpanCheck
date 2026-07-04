# Support Span Check cho AutoCAD Plant 3D

**Support Span Check** là một plugin (add-on) mạnh mẽ dành riêng cho phần mềm **AutoCAD Plant 3D**. Công cụ này tự động hóa hoàn toàn quy trình kiểm tra khoảng cách (nhịp) giữa các gối đỡ (Support) trên đường ống, giúp kỹ sư phát hiện nhanh các vị trí vi phạm tiêu chuẩn thiết kế.

---

## 🌟 Các tính năng nổi bật
- **Quét siêu tốc:** Thuật toán tối ưu hóa (O(1)) truy xuất trực tiếp vào cơ sở dữ liệu `PipeRunComponent` và `PipingLineGroup` của Plant 3D, không gây giật lag kể cả với các dự án siêu lớn.
- **Tự động nhận diện bọc bảo ôn (Insulation):** Tool thông minh tự động đọc độ dày bảo ôn của đường ống và phân loại tiêu chuẩn khoảng cách cho ống trần và ống bọc bảo ôn.
- **Tính toán U-Bend chuẩn xác:** Có khả năng đọc hiểu và đo chính xác chiều dài qua các đoạn Co chữ U (U-Bend / Return Bend).
- **Phát hiện Support "mồ côi":** Cảnh báo lập tức nếu có Support nào chưa được bắt chính xác vào tâm đường ống.
- **Đồng bộ với mô hình thực tế:** Chạm 2 lần (Double-click) vào bất kỳ cảnh báo nào trên giao diện, màn hình AutoCAD sẽ tự động Zoom thẳng đến 2 Support đó trên bản vẽ 3D.
- **Tuỳ biến tiêu chuẩn linh hoạt:** Người dùng có thể dễ dàng thêm, bớt, sửa đổi kích thước ống và khoảng cách tối đa theo từng tiêu chuẩn dự án.

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
- Nền tảng: AutoCAD Plant 3D (Khuyên dùng bản 2021 trở lên).
- Framework: .NET Framework tương thích với phiên bản AutoCAD hiện tại.

---
*Phát triển bởi Phùng Doãn.*
