using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaboratorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollaboratorId",
                table: "ServiceProviders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CollaboratorId",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Collaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CollaboratorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    CollaboratorType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    MaritalStatus = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    BankName = table.Column<string>(type: "text", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "text", nullable: true),
                    BankAccountType = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collaborators", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_CollaboratorId",
                table: "ServiceProviders",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CollaboratorId",
                table: "Employees",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborators_CollaboratorCode",
                table: "Collaborators",
                column: "CollaboratorCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Collaborators_CollaboratorId",
                table: "Employees",
                column: "CollaboratorId",
                principalTable: "Collaborators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProviders_Collaborators_CollaboratorId",
                table: "ServiceProviders",
                column: "CollaboratorId",
                principalTable: "Collaborators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Collaborators_CollaboratorId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceProviders_Collaborators_CollaboratorId",
                table: "ServiceProviders");

            migrationBuilder.DropTable(
                name: "Collaborators");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProviders_CollaboratorId",
                table: "ServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CollaboratorId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CollaboratorId",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "CollaboratorId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Employees");
        }
    }
}
