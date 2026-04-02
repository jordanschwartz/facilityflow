using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandWorkOrderLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PoAmount",
                table: "ServiceRequests",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoFileUrl",
                table: "ServiceRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoNumber",
                table: "ServiceRequests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PoReceivedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleConfirmedAt",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "ServiceRequests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoAmount",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PoFileUrl",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PoNumber",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "PoReceivedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ScheduleConfirmedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "ServiceRequests");
        }
    }
}
