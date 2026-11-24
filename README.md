# SmartGymBooking

**Đồ án ứng dụng web quản lý phòng gym, đăng ký gói tập và đặt lịch tập.**

## Bài toán và mục tiêu

Khách hàng cần biết gói tập của mình còn hiệu lực đến khi nào, có bao gồm huấn luyện viên cá nhân (PT) hay không và lịch đã đặt đang ở trạng thái nào. Phòng gym cần theo dõi giao dịch, lịch tập và lượt check-in để nhân viên có thể xử lý công việc hằng ngày và quản trị viên có thể theo dõi hoạt động.

SmartGymBooking tập trung các nghiệp vụ này vào một hệ thống web với ba vai trò. Hệ thống cũng cung cấp giáo án và chatbot để hỗ trợ khách hàng trong quá trình tập luyện.

## Chức năng theo vai trò

| Vai trò | Chức năng |
| --- | --- |
| Khách hàng | Đăng ký bằng địa chỉ Gmail hợp lệ, cập nhật họ tên trong **Hồ sơ của tôi**, mua gói tập, chuyển khoản qua mã QR, đặt hoặc hủy lịch, xem trạng thái check-in, lịch PT và sử dụng tính năng hỗ trợ tập luyện. |
| Nhân viên | Kiểm tra và xác nhận giao dịch chuyển khoản, xử lý lịch đặt và ghi nhận check-in. |
| Quản trị viên | Quản lý khách hàng, nhân viên, PT, lịch PT, dịch vụ, gói tập và thư viện bài tập. Theo dõi doanh thu và tình trạng đặt lịch trên dashboard. |

## Quy trình nghiệp vụ chính

1. **Mua gói tập.** Khách hàng chọn gói thường hoặc gói có PT. Hệ thống tạo giao dịch và hiển thị mã QR để chuyển khoản.
2. **Kích hoạt gói.** Nhân viên đối chiếu giao dịch và xác nhận thanh toán. Hệ thống ghi nhận giao dịch đã thanh toán và kích hoạt gói cho khách hàng.
3. **Đặt lịch và check-in.** Khách hàng có gói hợp lệ chọn dịch vụ và ngày tập. Hệ thống kiểm tra lịch trùng và xác định khung check-in theo giờ mở cửa (06:00–22:00, giờ Việt Nam) và thời hạn gói. Admin hoặc nhân viên ghi nhận check-in tại quầy theo lịch đặt hoặc cho khách đủ điều kiện đến trực tiếp. Lịch hết khung check-in được hệ thống tự động hủy nếu khách chưa đến.
4. **Phân PT.** Nhân viên chọn giờ PT trong khung của lịch đã đặt. Hệ thống kiểm tra quyền lợi gói tập và lịch trùng của khách và PT.

## Hỗ trợ tập luyện

Khách hàng có thể tạo giáo án theo mục tiêu, trình độ và số buổi tập mỗi tuần. Khi dùng tính năng đề xuất, ứng dụng chạy mô hình ONNX để dự đoán loại giáo án và chọn bài tập phù hợp từ thư viện. Mô hình được huấn luyện bằng Python và chạy trực tiếp trong ứng dụng .NET thông qua ONNX Runtime.

Chatbot hỗ trợ giải đáp câu hỏi về tập luyện. Ứng dụng có chế độ demo và tích hợp OpenAI tùy cấu hình.

## Kiến trúc và công nghệ

| Thành phần | Công nghệ và vai trò |
| --- | --- |
| Giao diện | Razor Views và Bootstrap hiển thị các trang theo vai trò. |
| Ứng dụng | ASP.NET Core MVC trên .NET 10 xử lý yêu cầu, xác thực và phân quyền. |
| Dữ liệu | Entity Framework Core và SQL Server lưu tài khoản, gói tập, giao dịch, lịch đặt và giáo án. |
| Đề xuất giáo án | Python huấn luyện mô hình và ONNX Runtime chạy dự đoán trong ứng dụng. |
