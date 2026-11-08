# Kiểm thử Auction Arena

## Chạy kiểm thử nghiệp vụ trên Windows

1. Giải nén toàn bộ dự án. Máy cần .NET Framework 4.8 và SQL Server đang hoạt động.
2. Đóng ứng dụng, chạy `KIEM_THU\CHAY_KIEM_THU.cmd` và nhập tên SQL Server.
3. Tài khoản Windows cần quyền tạo database. Công cụ tạo database riêng có tên `AA_Test_` kèm mã ngẫu nhiên, thực hiện kiểm thử và xóa database tạm khi kết thúc. Công cụ không chạy kiểm thử trên `AuctionArenaDB`.
4. Kết quả ghi vào `KET_QUA_KIEM_THU.txt`. Mã thoát 0 nghĩa là tất cả kiểm thử đạt. Nếu không xóa được database tạm, công cụ báo tên cụ thể để quản trị viên xử lý.

`ServiceChecks.cs` gọi các Service và DLL của chính bản Release. Dữ liệu kiểm thử chỉ được tạo trong database tạm, không nằm trong database của bản cài đặt. Mật khẩu trong tình huống thử được băm trước khi lưu.

## Các kiểm thử tự động

| Mã | Tình huống | Điều kiện đạt |
| --- | --- | --- |
| 01 | Băm và xác minh mật khẩu | Đúng mật khẩu được chấp nhận; sai bị từ chối; salt khác cho hash khác. |
| 02 | Quản trị viên đầu tiên và đăng nhập | Tạo và đăng nhập thành công; không tạo thêm quản trị viên qua luồng khởi tạo. |
| 03 | Phân quyền tại Service | Tài khoản không có quyền không ghi được lượt trả giá. |
| 04 | Điều kiện tiền cọc | Khách thiếu cọc không trả giá được. |
| 05 | Giá khởi điểm và bước giá | Lượt hợp lệ cập nhật giá; tự nâng giá và giá dưới mức tối thiểu bị từ chối. |
| 06 | Rollback hóa đơn | Trigger gây lỗi khi lưu chi tiết sau khi đầu hóa đơn đã được lưu; không còn đầu hóa đơn, chi tiết, thanh toán hoặc khoản cọc bị áp dụng. |
| 07 | Phí VIP, VAT và cọc | Giá búa 300.000; phí 15.000; giảm phí 1.500; VAT 1.350; cọc 50.000; còn phải thu 264.850. Không lập trùng hóa đơn. |
| 08 | Điều kiện xuất kho | Hóa đơn chưa thanh toán đủ không được bàn giao. |
| 09 | Thu tiền và mã chuyển khoản | Không thu vượt số dư; không nhận trùng tham chiếu ngân hàng; thu đủ chuyển sang Paid. |
| 10 | Bàn giao từng phần | Chỉ hiện vật được chọn chuyển sang Delivered; không xuất cùng hiện vật hai lần. |

Lưu ý tình huống 06 chủ động gây lỗi SQL, không mô phỏng mất mạng. Mục đích là chứng minh các lần SaveChanges trong cùng giao dịch không để lại dữ liệu dở dang.

## Phạm vi xác minh của gói phát hành

- Ba project .NET Framework 4.8 được biên dịch Release, không có lỗi hoặc cảnh báo.
- 10/10 kiểm thử nghiệp vụ ở bảng trên đạt trên SQL Server 2022. Mã nguồn Data/Services được biên dịch với EF 6.5.1 trong bộ chạy .NET 8 trên Linux để thực thi kiểm thử; đây không phải kiểm thử giao diện Windows. Bộ kiểm thử gửi kèm sử dụng DLL .NET Framework 4.8 trên Windows.
- 8/8 kiểm thử cấu trúc SQL trước đó đạt: bổ sung đủ chín cột và giữ dữ liệu; đồng bộ khi đã có một phần cấu trúc; rollback khi trùng tham chiếu; từ chối khi thiếu quyền; phát hiện sai kiểu cột; từ chối database không tương thích; bốn lần khởi động đồng thời; tạo database mới và kiểm tra các tệp SQL.
- Bộ cài NSIS được biên dịch và kiểm tra các tệp bên trong so với bản Release.

## Kiểm tra giao diện và cài đặt trên Windows

Các mục dưới đây là kịch bản nghiệm thu, chưa được đánh dấu đạt trong môi trường Linux:

| Nhóm | Thao tác | Kết quả cần kiểm tra |
| --- | --- | --- |
| Cài đặt | Chạy Setup, chọn SQL Server và tạo database mới | Có shortcut, mục gỡ cài đặt; mở được màn hình tạo Admin. |
| Cấu hình | Nhập sai tên server rồi nhập lại đúng | Có thông báo lỗi; chỉ lưu kết nối đã kiểm tra thành công. |
| Dữ liệu cũ | Cài vào thư mục mới, chọn database hiện có | Các khách hàng, chứng từ và số dư vẫn còn; không tạo dữ liệu nghiệp vụ mẫu. |
| Phân quyền | Đăng nhập Admin và tài khoản bị giới hạn | Menu và nút thêm/sửa/xóa/in thay đổi theo quyền; không vượt quyền bằng cách mở lại màn hình. |
| CRUD | Thêm, sửa, tìm kiếm và xóa dữ liệu danh mục | Hiển thị dữ liệu đã lưu; chặn dữ liệu thiếu, trùng hoặc đang được chứng từ tham chiếu. |
| Đấu giá | Tiếp nhận hiện vật, hợp đồng, nhập kho, phiên, đăng ký và cọc | Các trạng thái và điều kiện nghiệp vụ được kiểm soát. |
| Tài chính | Kết thúc phiên, lập hóa đơn, thu tiền và bàn giao | Tổng tiền đúng; không thu vượt; không bàn giao trước khi trả đủ hoặc bàn giao trùng. |
| Báo cáo | Lọc từ ngày đến ngày, mở hóa đơn và phiếu xuất RDLC | Dữ liệu đúng kỳ; không lỗi dataset; xem trước, xuất PDF và in được. |
| Gỡ cài đặt | Đóng ứng dụng rồi gỡ qua Windows hoặc Start Menu | Bỏ chương trình và shortcut; giữ database, cấu hình và nhật ký. |

## Đối chiếu yêu cầu phần mềm

Ứng dụng có 30 Form cụ thể, đủ năm Form khung; CRUD và nghiệp vụ sử dụng Entity Framework/LINQ; phân quyền theo nhóm và thao tác; mật khẩu PBKDF2-HMAC-SHA256; giao dịch nhiều bước; 14 mẫu RDLC và lọc ngày. Phần cấu hình/đồng bộ cấu trúc database sử dụng SQL quản trị; không dùng các câu lệnh SQL dạng chuỗi để thay thế CRUD nghiệp vụ. Những tệp SQL này phục vụ cài đặt và kiểm tra cấu trúc.
