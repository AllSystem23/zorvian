using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPalmTrackFeatureFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PalmTrackEnabled",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PalmTrackSsoAutoCreateUsers",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PalmTrackSsoEnabled",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PalmTrackSsoPropagateRoles",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PalmTrackSsoSharedProject",
                table: "CompanySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PalmTrackEnabled",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PalmTrackSsoAutoCreateUsers",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PalmTrackSsoEnabled",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PalmTrackSsoPropagateRoles",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PalmTrackSsoSharedProject",
                table: "CompanySettings");
        }
    }
}
