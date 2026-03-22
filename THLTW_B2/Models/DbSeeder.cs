using Microsoft.AspNetCore.Identity;

namespace THLTW_B2.Models
{
    public static class DbSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Tạo Role Admin nếu chưa có
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // BỔ SUNG: Tạo Role Employee (Nhân viên quản lý)
            if (!await roleManager.RoleExistsAsync("Employee"))
            {
                await roleManager.CreateAsync(new IdentityRole("Employee"));
                Console.WriteLine("==== SEEDER: TẠO ROLE Employee THÀNH CÔNG ====");
            }

            // BỔ SUNG: Tạo Role User (Khách hàng vãng lai)
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
                Console.WriteLine("==== SEEDER: TẠO ROLE USER THÀNH CÔNG ====");
            }

            // 2. Kiểm tra và tạo Admin mặc định
            var adminEmail = "admin@qlsb.com";
            var user = await userManager.FindByEmailAsync(adminEmail);

            if (user == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator", // Đảm bảo không NULL
                    EmailConfirmed = true
                };

                // Tạo với mật khẩu cực kỳ chuẩn: Chữ hoa, chữ thường, số, ký tự đặc biệt
                var result = await userManager.CreateAsync(newAdmin, "Admin@123456");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    Console.WriteLine("==== SEEDER: TẠO ADMIN THÀNH CÔNG! ====");
                }
                else
                {
                    Console.WriteLine("==== SEEDER LỖI RỒI: ====");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine("==== SEEDER: ADMIN ĐÃ TỒN TẠI TRONG DB ====");
            }
        }
    }
}