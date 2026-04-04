using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmailVisibilityConversationLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "InboundEmails",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InReplyToMessageId",
                table: "InboundEmails",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutboundEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientAddress = table.Column<string>(type: "text", nullable: false),
                    RecipientName = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentById = table.Column<Guid>(type: "uuid", nullable: false),
                    SentByName = table.Column<string>(type: "text", nullable: false),
                    EmailType = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundEmails_ServiceRequests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "ServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutboundEmails_Users_SentById",
                        column: x => x.SentById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboundEmailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboundEmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundEmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboundEmailAttachments_OutboundEmails_OutboundEmailId",
                        column: x => x.OutboundEmailId,
                        principalTable: "OutboundEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_ConversationId",
                table: "InboundEmails",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundEmails_InReplyToMessageId",
                table: "InboundEmails",
                column: "InReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmailAttachments_OutboundEmailId",
                table: "OutboundEmailAttachments",
                column: "OutboundEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_ConversationId",
                table: "OutboundEmails",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_SentAt",
                table: "OutboundEmails",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_SentById",
                table: "OutboundEmails",
                column: "SentById");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_ServiceRequestId",
                table: "OutboundEmails",
                column: "ServiceRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboundEmailAttachments");

            migrationBuilder.DropTable(
                name: "OutboundEmails");

            migrationBuilder.DropIndex(
                name: "IX_InboundEmails_ConversationId",
                table: "InboundEmails");

            migrationBuilder.DropIndex(
                name: "IX_InboundEmails_InReplyToMessageId",
                table: "InboundEmails");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "InboundEmails");

            migrationBuilder.DropColumn(
                name: "InReplyToMessageId",
                table: "InboundEmails");
        }
    }
}
