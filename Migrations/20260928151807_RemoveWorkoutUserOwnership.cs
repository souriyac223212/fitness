using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fitness.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkoutUserOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Workouts");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Workouts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Workouts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Workouts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
