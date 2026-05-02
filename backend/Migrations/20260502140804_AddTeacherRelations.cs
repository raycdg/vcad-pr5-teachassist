using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeachAssist.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_teachers",
                columns: table => new
                {
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    teacher_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_teachers", x => new { x.course_id, x.teacher_id });
                    table.ForeignKey(
                        name: "FK_course_teachers_courses_course_id",
                        column: x => x.course_id,
                        principalTable: "courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discipline_teachers",
                columns: table => new
                {
                    discipline_id = table.Column<int>(type: "integer", nullable: false),
                    teacher_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discipline_teachers", x => new { x.discipline_id, x.teacher_id });
                    table.ForeignKey(
                        name: "FK_discipline_teachers_disciplines_discipline_id",
                        column: x => x.discipline_id,
                        principalTable: "disciplines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_teachers");

            migrationBuilder.DropTable(
                name: "discipline_teachers");
        }
    }
}
