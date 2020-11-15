using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Webshop.Models;

namespace Webshop.Data
{
    public class WebshopContext : IdentityDbContext<SiteUser, IdentityRole<int>, int>
    {
        public WebshopContext(DbContextOptions<WebshopContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder)
            builder.Entity<SiteUser>().ToTable("SiteUsers");
        }

        public DbSet<CaffFile> CaffFiles { get; set; }
    }
}
