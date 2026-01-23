using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineExamSystem.Migrations
{
    public partial class AddUniqueRollNumberPerCollege : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure RollNumber has a fixed length (index-safe)
            migrationBuilder.AlterColumn<string>(
                name: "RollNumber",
                table: "Students",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Create UNIQUE index only when RollNumber is NOT NULL
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IX_Students_CollegeId_RollNumber
                  ON Students (CollegeId, RollNumber)
                  WHERE RollNumber IS NOT NULL"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DROP INDEX IF EXISTS IX_Students_CollegeId_RollNumber ON Students"
            );

            migrationBuilder.AlterColumn<string>(
                name: "RollNumber",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)");
        }
    }
}
