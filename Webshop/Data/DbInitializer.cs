using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Webshop.Models;

namespace Webshop.Data
{
    public class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration Configuration, WebshopContext context)
        {
            //context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return;
            }
            //adding customs roles
            var RoleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var UserManager = serviceProvider.GetRequiredService<UserManager<SiteUser>>();
            string[] roleNames = { "Admin", "User" };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                //creating the roles and seeding them to the database
                var roleExist = await RoleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await RoleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            //creating a super user who could maintain the web app
            var admin = new SiteUser
            {
                UserName = Configuration.GetSection("AppSettings")["AdminName"],
                Email = Configuration.GetSection("AppSettings")["AdminEmail"]
            };

            string adminPassword = Configuration.GetSection("AppSettings")["AdminPassword"];
            var user = await UserManager.FindByEmailAsync(Configuration.GetSection("AppSettings")["AdminEmail"]);

            if (user == null)
            {
                var createPowerUser = await UserManager.CreateAsync(admin, adminPassword);
                if (createPowerUser.Succeeded)
                {
                    //here we tie the new user to the "Admin" role 
                    await UserManager.AddToRoleAsync(admin, "Admin");

                }
            }

            /*
            // Look for any students.
            if (context.Cities.Any())
            {
                return;   // DB has been seeded
            }

            var cities = new City[]
                        {
                new City {Name = "Budapest"},
                new City {Name = "Győr"},
                        };

            foreach (City c in cities)
            {
                context.Cities.Add(c);
            }
            */
            context.SaveChanges();
        }
    }
}
