using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorStatusAndDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleProfileUrl",
                table: "Vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Vendors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Vendors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Vendors",
                type: "text",
                nullable: true);

            // Backfill Status from existing boolean fields
            migrationBuilder.Sql("""
                UPDATE "Vendors" SET "Status" = 'Dnu' WHERE "IsDnu" = true;
                UPDATE "Vendors" SET "Status" = 'Inactive' WHERE "IsActive" = false AND "IsDnu" = false;
                UPDATE "Vendors" SET "Status" = 'Active' WHERE "IsActive" = true AND "IsDnu" = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleProfileUrl",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Vendors");
        }
    }
}
