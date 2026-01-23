using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCollegeIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Colleges",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Colleges");
        }
    }
}
