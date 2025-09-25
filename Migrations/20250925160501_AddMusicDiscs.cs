using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace library_management_system.Migrations
{
    /// <inheritdoc />
    public partial class AddMusicDiscs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicDisc",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Artist = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    EAN = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicDisc", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LoanMusicDisc",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LoanID = table.Column<int>(type: "INTEGER", nullable: false),
                    MusicDiscID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanMusicDisc", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LoanMusicDisc_Loan_LoanID",
                        column: x => x.LoanID,
                        principalTable: "Loan",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanMusicDisc_MusicDisc_MusicDiscID",
                        column: x => x.MusicDiscID,
                        principalTable: "MusicDisc",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanMusicDisc_LoanID",
                table: "LoanMusicDisc",
                column: "LoanID");

            migrationBuilder.CreateIndex(
                name: "IX_LoanMusicDisc_MusicDiscID",
                table: "LoanMusicDisc",
                column: "MusicDiscID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanMusicDisc");

            migrationBuilder.DropTable(
                name: "MusicDisc");
        }
    }
}
