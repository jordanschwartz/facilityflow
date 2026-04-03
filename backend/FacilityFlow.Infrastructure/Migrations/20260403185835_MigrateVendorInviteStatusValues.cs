using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateVendorInviteStatusValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Map old VendorInviteStatus values to new ones
            migrationBuilder.Sql("UPDATE \"VendorInvites\" SET \"Status\" = 'Candidate' WHERE \"Status\" = 'Invited'");
            migrationBuilder.Sql("UPDATE \"VendorInvites\" SET \"Status\" = 'QuoteSubmitted' WHERE \"Status\" = 'Quoted'");
            migrationBuilder.Sql("UPDATE \"VendorInvites\" SET \"Status\" = 'Rejected' WHERE \"Status\" = 'Declined'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
