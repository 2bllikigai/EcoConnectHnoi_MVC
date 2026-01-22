using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Services; // Thêm namespace này cho EmailSender
using Microsoft.AspNetCore.Identity; // Thêm namespace này cho IPasswordHasher
using EcoConnect_Hanoi.Models; // Thêm namespace cho Users model
using Microsoft.AspNetCore.Authentication.Cookies; // Thêm namespace cho Cookie Authentication
using System.Security.Claims; // Để dùng ClaimTypes (mặc dù không trực tiếp dùng ở đây, nhưng cần cho Authentication)

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
// ====================================================================

// Add services to the container.
builder.Services.AddControllersWithViews();

// Cấu hình DbContext của bạn
builder.Services.AddDbContext<EcoConnectHnContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EcoConnectDb"))); // Đảm bảo tên ConnectionString khớp với appsettings.json

// Đăng ký dịch vụ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian không hoạt động tối đa của session
    options.Cookie.HttpOnly = true; // Cookie chỉ truy cập được qua HTTP, không qua JavaScript
    options.Cookie.IsEssential = true; // Cookie session là cần thiết cho ứng dụng
});

// Cấu hình EmailSettings và đăng ký EmailSender service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
// Đăng ký IPasswordHasher cho Users model
// Đây là cách sử dụng PasswordHasher mặc định của ASP.NET Core Identity.
// Nếu bạn muốn dùng BCrypt, bạn cần cài đặt package BCrypt.Net.Core
// và tạo một lớp triển khai IPasswordHasher<Users> riêng.
builder.Services.AddSingleton<IPasswordHasher<Users>, PasswordHasher<Users>>();

// Cấu hình Authentication (Cookie-based)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập nếu chưa xác thực
        options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn đến trang báo lỗi quyền truy cập
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Thời gian sống mặc định của cookie
        options.SlidingExpiration = true; // Cookie sẽ được làm mới nếu người dùng hoạt động trong thời gian gần hết hạn
    });


var app = builder.Build();

// ====================================================================
// CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE)
// ====================================================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Phục vụ các file tĩnh (CSS, JS, hình ảnh)

app.UseRouting(); // Xác định endpoint nào sẽ xử lý request

// Đặt app.UseSession() SAU app.UseRouting() và TRƯỚC app.UseAuthentication/Authorization
app.UseSession();

// PHẢI ĐẶT TRƯỚC app.UseAuthorization()
app.UseAuthentication(); // Xác thực người dùng (đọc cookie, tạo User Principal)
app.UseAuthorization();  // Kiểm tra quyền truy cập dựa trên User Principal

// app.MapStaticAssets(); // Giữ nguyên nếu đây là extension method tùy chỉnh của bạn
// app.UseSession(); // Đã di chuyển lên trên, xóa dòng này
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    // .WithStaticAssets(); // Giữ nguyên nếu đây là extension method tùy chỉnh của bạn


app.Run();