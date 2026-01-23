using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class MakeRollNumberNullableInStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "RollNumber",
                table: "Students",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students",
                columns: new[] { "CollegeId", "RollNumber" },
                unique: true);
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
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeId_RollNumber",
                table: "Students",
                columns: new[] { "CollegeId", "RollNumber" },
                unique: true);
        }


    }
}
