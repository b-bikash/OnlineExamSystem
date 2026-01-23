using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentCollege : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollegeId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeId",
                table: "Students",
                column: "CollegeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Colleges_CollegeId",
                table: "Students",
                column: "CollegeId",
                principalTable: "Colleges",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Colleges_CollegeId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CollegeId",
                table: "Students");
        }
    }
}
