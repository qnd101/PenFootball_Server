using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using PenFootball_Server.Models;
using System.Collections;
using System.Diagnostics;

namespace PenFootball_Server.DB
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
        {   
            PasswordHasher<object> _passwordHasher = new PasswordHasher<object>();
            using (var context = new UserDataContext(
                serviceProvider.GetRequiredService<
                    DbContextOptions<UserDataContext>>()))
            {
                if (context == null || context.Users == null)
                {
                    throw new ArgumentNullException("Null RazorPagesMovieContext");
                }

                

                var seedplayers = (new List<int> { 1, 2, 3,4, 5, 6, 7, 8 }).Select((i) => new UserModel
                {
                    Name = $"BOT{i}",
                    Password = _passwordHasher.HashPassword(null, $"iambot{i}"),
                    Role = Roles.Player,
                    Rating = 1000,
                    JoinDate = new DateTime(1972, 11, 21)
                });

                var seedservers = configuration.GetSection("GameServers")?.GetSection("ServerAccounts").GetChildren()
                    .Select(item =>
                    new UserModel
                    {
                        Name = item.Key,
                        Password = _passwordHasher.HashPassword(null, item.GetValue<string>("Password") ?? throw new Exception("something wrong with config")),
                        Role = Roles.Server
                    }) ?? new List<UserModel>();

                context.Database.ExecuteSqlRaw("DELETE FROM Users;"); //SQLite Environment
                context.Users.AddRange(seedplayers.Concat(seedservers));
                context.SaveChanges();
            }
        }
    }
}
