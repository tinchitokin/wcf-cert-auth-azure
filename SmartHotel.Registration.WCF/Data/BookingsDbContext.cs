using System;
using System.Data.Entity;

namespace SmartHotel.Registration.Wcf.Data
{
    public class BookingsDbContext : DbContext
    {
        public BookingsDbContext() : base(Environment.GetEnvironmentVariable("DefaultConnection") ?? "DefaultConnection")
        {
            Database.SetInitializer(new BookingsDbContextInitializer());
        }

        public DbSet<Booking> Bookings { get; set; }
    }
}