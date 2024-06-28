using SmartHotel.Registration.Wcf.Data;
using SmartHotel.Registration.Wcf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace SmartHotel.Registration.Wcf
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.PerSession)]
    public class Service : IService
    {
        public IEnumerable<Models.Registration> GetTodayRegistrations()
        {
            return GetTodayRegistrationsInMemory();

            using (var db = new BookingsDbContext())
            {
                var checkins = db.Bookings
                //.Where(b => b.From == DateTime.Today)
                .Select(BookingToCheckin);

                var checkouts = db.Bookings
                    // .Where(b => b.To == DateTime.Today)
                    .Select(BookingToCheckout);

                var registrations = checkins.Concat(checkouts).OrderBy(r => r.Date);
                var result = registrations.ToList();
                return result;
            }
        }

        public RegistrationDaySummary GetTodayRegistrationSummary()
        {
            using (var db = new BookingsDbContext())
            {
                var totalCheckins = db.Bookings
                .Count();

                var totalCheckouts = db.Bookings
                    .Count();

                var summary = new RegistrationDaySummary
                {
                    Date = DateTime.Today,
                    CheckIns = totalCheckins,
                    CheckOuts = totalCheckouts
                };

                return summary;
            }
        }

        public Models.Registration GetCheckin(int registrationId)
        {
            using (var db = new BookingsDbContext())
            {
                var checkin = db.Bookings
                .Where(b => b.Id == registrationId)
                .Select(BookingToCheckin)
                .First();

                return checkin;
            }
        }

        public Models.Registration GetCheckout(int registrationId)
        {
            using (var db = new BookingsDbContext())
            {
                var checkout = db.Bookings
                .Where(b => b.Id == registrationId)
                .Select(BookingToCheckin)
                .First();

                return checkout;
            }
        }

        private Models.Registration BookingToCheckin(Booking booking)
        {
            return new Models.Registration
            {
                Id = booking.Id,
                Type = "CheckIn",
                Date = booking.From,
                CustomerId = booking.CustomerId,
                CustomerName = booking.CustomerName,
                Passport = booking.Passport,
                Address = booking.Address,
                Amount = booking.Amount,
                Total = booking.Total
            };
        }

        private Models.Registration BookingToCheckout(Booking booking)
        {
            return new Models.Registration
            {
                Id = booking.Id,
                Type = "CheckOut",
                Date = booking.To,
                CustomerId = booking.CustomerId,
                CustomerName = booking.CustomerName,
                Passport = booking.Passport,
                Address = booking.Address,
                Amount = booking.Amount,
                Total = booking.Total
            };
        }

        public IEnumerable<Models.Registration> GetTodayRegistrationsInMemory()
        {
            // Example in-memory collection of bookings
            var bookings = new List<Booking>
            {
                // Add some example bookings here
                new Booking { Id = 1, From = DateTime.Today, To = DateTime.Today.AddDays(1), CustomerId = 101.ToString(), CustomerName = "John Doe", Passport = "AB123456", Address = "123 Main St", Amount = 100, Total = 120 },
                new Booking { Id = 2, From = DateTime.Today.AddDays(-1), To = DateTime.Today, CustomerId = 102.ToString(), CustomerName = "Jane Doe", Passport = "CD789012", Address = "456 Elm St", Amount = 100, Total = 120 },
                // Add more bookings as needed for testing
            };

            var checkins = bookings
                .Where(b => b.From == DateTime.Today)
                .Select(BookingToCheckin);

            var checkouts = bookings
                .Where(b => b.To == DateTime.Today)
                .Select(BookingToCheckout);

            var registrations = checkins.Concat(checkouts).OrderBy(r => r.Date);
            return registrations.ToList();
        }
    }
}