# ELearning\_ToanHocHay\_API

This is API project for the system



#### **Cài Đặt**

1\. Cài Đặt NuGet Packages

bashdotnet add package Microsoft.EntityFrameworkCore

dotnet add package Microsoft.EntityFrameworkCore.SqlServer

dotnet add package Microsoft.EntityFrameworkCore.Tools

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

dotnet add package System.IdentityModel.Tokens.Jwt

dotnet add package BCrypt.Net-Next

dotnet add package Swashbuckle.AspNetCore

2\. Cấu Hình Connection String

Cập nhật appsettings.json:

json{

&nbsp; "ConnectionStrings": {

&nbsp;   "DefaultConnection": "Server=YOUR\_SERVER;Database=ELearning\_ToanHocHay;User Id=YOUR\_USER;Password=YOUR\_PASSWORD;TrustServerCertificate=True;"

&nbsp; }

}

3\. Cấu Hình JWT Settings

`JwtSettings:SecretKey` (>= 32 ký tự) KHÔNG được đặt trong appsettings.json. App sẽ
throw khi khởi động nếu thiếu.

- Local: `dotnet user-secrets set "JwtSettings:SecretKey" "<chuỗi ngẫu nhiên >= 32 ký tự>"`
- Server: biến môi trường `JwtSettings__SecretKey`

Các khoá còn lại (`Issuer`, `Audience`, `ExpirationMinutes`, `RefreshTokenDays`) vẫn nằm
trong appsettings.json. Xem `appsettings.Example.json`.

4\. Chạy Migration

bashdotnet ef migrations add InitialCreate

dotnet ef database update



#### **Bảo Mật**

1\. Password Hashing



Sử dụng BCrypt để hash password

Salt rounds: 10 (mặc định của BCrypt.Net)



2\. JWT Token



Token có thời gian hết hạn (mặc định: 24 giờ)

Sử dụng HMAC-SHA256 để ký token

Secret key phải được giữ bí mật và có độ dài tối thiểu 32 ký tự



3\. HTTPS



Luôn sử dụng HTTPS trong production

Token chỉ nên được truyền qua HTTPS



#### **Testing với Swagger**



1. Chạy ứng dụng: dotnet run

2\. Mở browser: https://localhost:5001/swagger

3\. Test Login endpoint

4\. Copy token từ response

5\. Click "Authorize" button ở góc phải trên

6\. Nhập: Bearer {token}

7\. Test các protected endpoints



#### **Lưu Ý Quan Trọng**



Secret Key: Phải thay đổi và giữ bí mật trong production

Connection String: Không commit vào source control

Token Expiration: Cân nhắc thời gian hết hạn phù hợp với ứng dụng

Refresh Token: Có thể implement thêm refresh token cho UX tốt hơn

Rate Limiting: Nên implement rate limiting cho login endpoint

Audit Logging: Log các hoạt động đăng nhập/đăng xuất



