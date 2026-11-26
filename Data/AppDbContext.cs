using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Models;

namespace WpfApp1.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<PickupPoint> PickupPoints { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var connection = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Shop.sqlite"),
                Mode = SqliteOpenMode.ReadWrite,
                ForeignKeys = true
            };
            options.UseSqlite(connection.ToString());
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<OrderItem>().HasKey(item => new { item.OrderId, item.ProductId });
        }
    }
}
