using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns first
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            // Split existing Name into FirstName/LastName
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""FirstName"" = SPLIT_PART(""Name"", ' ', 1),
                    ""LastName""  = CASE
                        WHEN POSITION(' ' IN ""Name"") > 0
                        THEN SUBSTRING(""Name"" FROM POSITION(' ' IN ""Name"") + 1)
                        ELSE ''
                    END,
                    ""Status""    = 'Active',
                    ""IsAdmin""   = CASE WHEN ""Role"" = 'Operator' AND ""Email"" = 'admin@facilityflow.com' THEN true ELSE false END,
                    ""UpdatedAt"" = NOW() AT TIME ZONE 'UTC'
            ");

            // Drop the old Name column
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add Name column
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Merge FirstName + LastName back into Name
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""Name"" = TRIM(""FirstName"" || ' ' || ""LastName"")
            ");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");
        }
    }
}
