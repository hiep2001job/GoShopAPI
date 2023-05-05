using GoShop.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoShop.Data
{
    public class GoShopContext:DbContext
    {
        public GoShopContext(DbContextOptions options):base(options)
        {
            
        }
        public DbSet<Product> Products { get; set; }

    }
}
