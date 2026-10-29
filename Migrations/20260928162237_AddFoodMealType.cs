using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fitness.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodMealType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MealType",
                table: "Foods",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealType",
                table: "Foods");
        }
    }
}
