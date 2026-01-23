using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueRollNumberIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students",
                columns: new[] { "CollegeId", "RollNumber" },
                unique: true,
                filter: "[CollegeId] IS NOT NULL AND [RollNumber] IS NOT NULL"
            );
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "RollNumber",
                table: "Students",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students",
                columns: new[] { "CollegeId", "RollNumber" },
                unique: true,
                filter: "[CollegeId] IS NOT NULL");
        }
    }
}
