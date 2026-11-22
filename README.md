# SmartGymBooking

**Đồ án ứng dụng web quản lý phòng gym, đăng ký gói tập và đặt lịch tập.**

## Bài toán và mục tiêu

Khách hàng cần biết gói tập của mình còn hiệu lực đến khi nào, có bao gồm huấn luyện viên cá nhân (PT) hay không và lịch đã đặt đang ở trạng thái nào. Phòng gym cần theo dõi giao dịch, lịch tập và lượt check-in để nhân viên có thể xử lý công việc hằng ngày và quản trị viên có thể theo dõi hoạt động.

SmartGymBooking tập trung các nghiệp vụ này vào một hệ thống web với ba vai trò. Hệ thống cũng cung cấp giáo án và chatbot để hỗ trợ khách hàng trong quá trình tập luyện.

## Chức năng theo vai trò

| Vai trò | Chức năng |
| --- | --- |
| Khách hàng | Đăng ký tài khoản, xem và mua gói tập, chuyển khoản qua mã QR, xem lịch sử giao dịch và gói đang sử dụng, đặt hoặc hủy lịch, xem lịch PT và sử dụng tính năng hỗ trợ tập luyện. |
| Nhân viên | Kiểm tra và xác nhận giao dịch chuyển khoản, xử lý lịch đặt và ghi nhận check-in. |
| Quản trị viên | Quản lý khách hàng, nhân viên, PT, lịch PT, dịch vụ, gói tập và thư viện bài tập. Theo dõi doanh thu và tình trạng đặt lịch trên dashboard. |

## Quy trình nghiệp vụ chính

1. **Mua gói tập.** Khách hàng chọn gói thường hoặc gói có PT. Hệ thống tạo giao dịch và hiển thị mã QR để chuyển khoản.
2. **Kích hoạt gói.** Nhân viên đối chiếu giao dịch và xác nhận thanh toán. Hệ thống ghi nhận giao dịch đã thanh toán và kích hoạt gói cho khách hàng.
3. **Đặt lịch và check-in.** Khách hàng có gói hợp lệ chọn dịch vụ và khung giờ. Hệ thống kiểm tra lịch trùng. Nhân viên xử lý lịch đặt và check-in khi khách đến. Lịch quá giờ chưa có check-in được hệ thống tự động hủy.

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

## Chạy và trình diễn

1. Cài .NET 10 SDK và chuẩn bị cơ sở dữ liệu SQL Server có đầy đủ lược đồ SmartGymBooking.
2. Cấu hình `ConnectionStrings:DefaultConnection` theo máy chủ và cơ sở dữ liệu đang sử dụng.
3. Chạy `dotnet run` ở thư mục gốc và mở địa chỉ hiển thị trong terminal.
4. Trình diễn bằng cách đăng nhập lần lượt với ba vai trò, xem gói PT của khách hàng và mở lịch sử giao dịch, màn hình xử lý lịch đặt và dashboard quản trị.

Script `database/06_DemoAccounts.sql` tạo dữ liệu trình diễn trên cơ sở dữ liệu đã có lược đồ và gói PT đang bán. Để chạy bản web với chatbot và giáo án tắt, dùng `dotnet run -p:EnableAiFeatures=false`.

Chạy script `database/07_BookingPT.sql` trên cơ sở dữ liệu đang dùng trước khi triển khai bản có tính năng phân PT từ lịch đặt.

Chạy thêm `database/08_PaymentReconciliation.sql` trước khi triển khai bản có tính năng đối soát giao dịch quá hạn.
