using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.DataAccess;
using THLTW_B2.Hubs;
using THLTW_B2.Models; // BỔ SUNG: Khai báo thư mục Models để nhận diện ApplicationUser

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Database (Giữ nguyên của bạn)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddSignalR(); // <--- Thêm dòng này

// BỔ SUNG 1: Cấu hình Identity (Đăng nhập, Đăng ký, Phân quyền Role)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false; // Tạm tắt bắt buộc xác thực email qua mail
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI(); // Cần thiết để bung giao diện đăng nhập mặc định
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        // Copy 2 mã ở Bước 1 dán vào giữa cặp ngoặc kép này:
        options.ClientId = "511913349936-btbv0aqiu873nrqcuh3uc6884q8npcaf.apps.googleusercontent.com";
        options.ClientSecret = "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";
    });
// Cấu hình session (Giữ nguyên của bạn)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Thêm MVC vào dự án
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // BỔ SUNG 2: Bắt buộc phải có để chạy được màn hình Identity (Login/Register)

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Gọi hàm khởi tạo dữ liệu (Role + Admin User)
        await DbSeeder.SeedDataAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu mẫu.");
    }
}

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapHub<MatchHub>("/matchHub"); // <--- Thêm dòng này để tạo điểm kết nối (endpoint)

app.UseRouting();

app.UseSession();

// BỔ SUNG 3: Bật tính năng Xác thực (Ai đang đăng nhập?) TRƯỚC khi Phân quyền
app.UseAuthentication();
app.UseAuthorization();

// BỔ SUNG 4: Cấu hình đường dẫn cho các Khu vực (Areas: Admin, Employee) - Phải đặt TRƯỚC default
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Cấu hình routing mặc định (Giữ nguyên)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // BỔ SUNG 5: Kích hoạt các trang Razor (Login/Register của Identity)

app.Run();