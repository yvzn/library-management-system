using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace library_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddCreationDateToLoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate",
                table: "Loan",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Set CreationDate to LoanDate for existing loans
            migrationBuilder.Sql(
                @"UPDATE Loan SET CreationDate = LoanDate WHERE CreationDate = '0001-01-01 00:00:00'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Loan");
        }
    }
}
