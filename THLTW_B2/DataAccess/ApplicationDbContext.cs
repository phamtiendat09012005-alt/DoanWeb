using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using THLTW_B2.Models;

namespace THLTW_B2.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {   
        }
        public DbSet<MatchRequest> MatchRequests { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<SoccerField> SoccerFields { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Team> Teams { get; set; }
<<<<<<< HEAD
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
=======
        public DbSet<Expense> Expenses { get; set; }
>>>>>>> 32b5efddd0d7135f92f57639fa4765e6abfa33af
    }

}
