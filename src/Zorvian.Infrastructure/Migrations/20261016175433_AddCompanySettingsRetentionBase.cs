using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySettingsRetentionBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetentionBase",
                table: "CompanySettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "taxable_gross");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetentionBase",
                table: "CompanySettings");
        }
    }
}
