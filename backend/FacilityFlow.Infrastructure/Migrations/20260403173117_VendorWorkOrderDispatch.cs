using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VendorWorkOrderDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicToken",
                table: "VendorInvites",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkOrderDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorInviteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    PdfUrl = table.Column<string>(type: "text", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderDocuments_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkOrderDocuments_VendorInvites_VendorInviteId",
                        column: x => x.VendorInviteId,
                        principalTable: "VendorInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvites_PublicToken",
                table: "VendorInvites",
                column: "PublicToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderDocuments_ServiceRequestId",
                table: "WorkOrderDocuments",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderDocuments_VendorInviteId",
                table: "WorkOrderDocuments",
                column: "VendorInviteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrderDocuments");

            migrationBuilder.DropIndex(
                name: "IX_VendorInvites_PublicToken",
                table: "VendorInvites");

            migrationBuilder.DropColumn(
                name: "PublicToken",
                table: "VendorInvites");
        }
    }
}
