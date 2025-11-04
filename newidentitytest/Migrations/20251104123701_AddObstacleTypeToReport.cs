using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace newidentitytest.Migrations
{
    /// <inheritdoc />
    public partial class AddObstacleTypeToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObstacleType",
                table: "reports",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObstacleType",
                table: "reports");
        }
    }
}
