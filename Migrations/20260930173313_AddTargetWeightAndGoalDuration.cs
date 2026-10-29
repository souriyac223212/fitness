using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fitness.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetWeightAndGoalDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoalDurationMonths",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TargetWeight",
                table: "UserProfiles",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoalDurationMonths",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TargetWeight",
                table: "UserProfiles");
        }
    }
}
