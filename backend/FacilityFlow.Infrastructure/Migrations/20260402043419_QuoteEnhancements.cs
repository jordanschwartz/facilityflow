using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuoteEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assumptions",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimatedDurationUnit",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationValue",
                table: "Quotes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Exclusions",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotToExceedPrice",
                table: "Quotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedStartDate",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorAvailability",
                table: "Quotes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuoteLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteLineItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLineItems_QuoteId",
                table: "QuoteLineItems",
                column: "QuoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteLineItems");

            migrationBuilder.DropColumn(
                name: "Assumptions",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationUnit",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationValue",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Exclusions",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "NotToExceedPrice",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ProposedStartDate",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "VendorAvailability",
                table: "Quotes");
        }
    }
}
