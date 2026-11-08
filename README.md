# Auction Arena

Ứng dụng quản lý sàn đấu giá trực tiếp trên Windows, sử dụng C# WinForms, .NET Framework 4.8, Entity Framework 6 và SQL Server.

## Chức năng

Quản lý tài khoản và phân quyền; khách hàng, chủ hàng, hiện vật; hợp đồng ký gửi và nhập kho; phiên đấu giá, đăng ký và trả giá; hóa đơn, thu tiền, hoàn cọc, bàn giao và chi trả chủ hàng; báo cáo và in chứng từ.

## Cài đặt

Gói có sẵn `AuctionArena_Setup.exe`. Mở bộ cài trên Windows, chọn thư mục cài đặt và SQL Server. Bộ cài tạo shortcut, đăng ký gỡ cài đặt và có tùy chọn tạo database bằng Windows Authentication. SQL Server phải được cài riêng hoặc có sẵn trên máy chủ. Gỡ ứng dụng không xóa database.

Kịch bản và công cụ kiểm thử nghiệp vụ nằm trong thư mục `KIEM_THU`.

## Chạy ứng dụng

1. Giải nén toàn bộ gói vào một thư mục mới trên Windows.
2. Nhấp đúp `CHAY_AUCTION_ARENA.cmd` ở thư mục ngoài cùng.
3. Chọn đúng SQL Server đang lưu dữ liệu của bạn. Ứng dụng chuẩn bị cấu trúc thanh toán, xuất kho còn thiếu trước khi đăng nhập.

Gói có sẵn EXE Release và thư viện tại `DONG_GOI\BAN_CHAY_LOCAL\AuctionArena`. Chạy ứng dụng không cần Visual Studio; máy cần .NET Framework 4.8 và quyền kết nối SQL Server. Nếu thiếu quyền sửa cấu trúc lần đầu, quản trị viên chạy tệp `SQL\04_DongBoThanhToanXuatKho.sql` trên đúng database rồi mở lại ứng dụng.

## Chạy từ mã nguồn

Máy biên dịch cần Visual Studio 2022 với **.NET desktop development**, bộ công cụ .NET Framework 4.8 và kết nối Internet để khôi phục thư viện NuGet.

1. Giải nén vào một thư mục trên Windows.
2. Chạy `CHAY_AUCTION_ARENA.cmd`. Nếu chưa có bản chạy, chương trình biên dịch và tạo bản chạy.
3. Chọn đúng tên SQL Server, ví dụ `.\SQLEXPRESS` hoặc `MAYCHU\SQLEXPRESS`.
4. Nếu chưa có `AuctionArenaDB`, công cụ khởi tạo cơ sở dữ liệu bằng tài khoản Windows có quyền tạo database. Nếu đã có, công cụ bổ sung cấu trúc thanh toán, xuất kho còn thiếu rồi kiểm tra trước khi mở ứng dụng.
5. Tạo tài khoản quản trị khi ứng dụng yêu cầu, sau đó đăng nhập.

Sau khi sửa mã nguồn, chạy `BIEN_DICH_VA_CHAY.cmd` để biên dịch lại. Cấu hình kết nối của bản chạy tại máy được giữ lại.

Có thể mở `AuctionArena.sln` trong Visual Studio, chọn `AuctionArena.WinForms` làm Startup Project rồi nhấn F5. Khi chạy theo cách này, cơ sở dữ liệu cần được chuẩn bị trước và kết nối được chọn trong ứng dụng.

## Đóng gói ứng dụng

| Thao tác | Tệp thực hiện | Kết quả |
| --- | --- | --- |
| Tạo gói chạy | `DONG_GOI\TAO_BAN_CHAY.cmd` | `AuctionArena_BanChay.zip` |
| Tạo bộ cài | `DONG_GOI\TAO_SETUP.cmd` | `AuctionArena_Setup.exe` |

Tạo lại bộ cài cần thêm NSIS 3 Unicode. Các tệp kết quả nằm cùng thư mục với `AuctionArena.sln`. Gói chạy chứa EXE, DLL, công cụ cấu hình cơ sở dữ liệu và hướng dẫn sử dụng. Người sử dụng gói chạy cần Windows 10/11, .NET Framework 4.8 và quyền kết nối SQL Server. SQL Server được cài đặt riêng hoặc sử dụng máy chủ trong mạng nội bộ.

## Cơ sở dữ liệu

| Tệp | Công dụng |
| --- | --- |
| `SQL\01_TaoDatabase.sql` | Khởi tạo `AuctionArenaDB` khi cài đặt mới. |
| `SQL\02_DongBoCauTruc.sql` | Bổ sung bảng, cột và ràng buộc cần thiết cho cơ sở dữ liệu đang sử dụng. |
| `SQL\03_KiemTraDatabase.sql` | Kiểm tra kết nối, cấu trúc và đối soát dữ liệu chứng từ. |
| `SQL\04_DongBoThanhToanXuatKho.sql` | Đồng bộ cấu trúc thanh toán và xuất kho; ứng dụng tự thực hiện khi khởi động. |
| `SQL\SchemaColumns.tsv` | Danh sách cột để công cụ cài đặt kiểm tra cấu trúc. |

Sao lưu và chọn đúng SQL Server trước khi chạy tệp đồng bộ cấu trúc. Không dùng tệp khởi tạo để xử lý cơ sở dữ liệu đang có dữ liệu. Khi khởi động, ứng dụng bổ sung các cột thanh toán, xuất kho còn thiếu và các ràng buộc liên quan trong cùng một giao dịch. Các giá trị chứng từ hiện có được giữ nguyên. Nếu thiếu quyền ALTER, quản trị viên chạy `SQL\04_DongBoThanhToanXuatKho.sql` một lần trên đúng cơ sở dữ liệu. Khi cấu trúc đã đủ, tài khoản sử dụng thông thường không cần quyền ALTER.

Công cụ khởi tạo tự động dùng Windows Authentication. Nếu sử dụng SQL Authentication, quản trị viên chuẩn bị cơ sở dữ liệu trước, sau đó mở `AuctionArena.WinForms.exe` và nhập thông tin đăng nhập SQL ở màn hình cấu hình kết nối.

## Cấu trúc mã nguồn

- `AuctionArena.Data`: các bảng dữ liệu, DbContext, repository và xử lý mật khẩu.
- `AuctionArena.Services`: phân quyền, điều kiện nghiệp vụ, giao dịch và truy vấn báo cáo.
- `AuctionArena.WinForms`: giao diện, xuất Excel và mẫu in RDLC.
- `SQL`: khởi tạo, đồng bộ cấu trúc và kiểm tra dữ liệu.
- `DONG_GOI`: biên dịch, tạo gói chạy, bộ cài và công cụ cấu hình.
