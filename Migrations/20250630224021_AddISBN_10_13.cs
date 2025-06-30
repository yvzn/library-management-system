using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace library_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddISBN_10_13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ISBN",
                table: "Book",
                newName: "ISBN_13");

            migrationBuilder.AddColumn<string>(
                name: "ISBN_10",
                table: "Book",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ISBN_10",
                table: "Book");

            migrationBuilder.RenameColumn(
                name: "ISBN_13",
                table: "Book",
                newName: "ISBN");
        }
    }
}
