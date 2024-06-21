namespace SmartHotel.Registration.Wcf.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class initialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Bookings",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    From = c.DateTime(nullable: false),
                    To = c.DateTime(nullable: false),
                    CustomerId = c.String(),
                    CustomerName = c.String(),
                    Passport = c.String(),
                    Address = c.String(),
                    Amount = c.Int(nullable: false),
                    Total = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.Bookings");
        }
    }
}