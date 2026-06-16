using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zorvian.Infrastructure.Migrations
{
    public partial class BaselineSync : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline migration: all tables already exist in the database.
            // This migration exists solely to synchronize the EF Core model snapshot.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Cannot roll back the baseline migration.");
        }
    }
}
