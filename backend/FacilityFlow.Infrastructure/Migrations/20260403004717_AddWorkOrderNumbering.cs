using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNumber",
                table: "ServiceRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderPrefix",
                table: "Clients",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkOrderNumber",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "WorkOrderPrefix",
                table: "Clients");
        }
    }
}
