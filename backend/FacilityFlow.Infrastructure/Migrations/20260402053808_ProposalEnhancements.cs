using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProposalEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstimatedDuration",
                table: "Proposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Proposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginPercentage",
                table: "Proposals",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NotToExceedPrice",
                table: "Proposals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedStartDate",
                table: "Proposals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Proposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SummaryGeneratedByAi",
                table: "Proposals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TermsAndConditions",
                table: "Proposals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseNtePricing",
                table: "Proposals",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "VendorCost",
                table: "Proposals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Proposals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProposalAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalAttachments_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalAttachments_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProposalVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProposalId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VendorCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MarginPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ScopeOfWork = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    NotToExceedPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangeNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalVersions_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAttachments_AttachmentId",
                table: "ProposalAttachments",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalAttachments_ProposalId_AttachmentId",
                table: "ProposalAttachments",
                columns: new[] { "ProposalId", "AttachmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposalVersions_ProposalId",
                table: "ProposalVersions",
                column: "ProposalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposalAttachments");

            migrationBuilder.DropTable(
                name: "ProposalVersions");

            migrationBuilder.DropColumn(
                name: "EstimatedDuration",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "MarginPercentage",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "NotToExceedPrice",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "ProposedStartDate",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "SummaryGeneratedByAi",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "TermsAndConditions",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "UseNtePricing",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "VendorCost",
                table: "Proposals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Proposals");
        }
    }
}
