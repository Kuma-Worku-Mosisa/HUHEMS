using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEMS.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentExam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "StudentExams",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "TakenExam",
                table: "StudentExams",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "StudentExams");

            migrationBuilder.DropColumn(
                name: "TakenExam",
                table: "StudentExams");
        }
    }
}
