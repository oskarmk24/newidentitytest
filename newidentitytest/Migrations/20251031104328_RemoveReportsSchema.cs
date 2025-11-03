using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace newidentitytest.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReportsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "reports",
                schema: "obstacledb",
                newName: "reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "obstacledb");

            migrationBuilder.RenameTable(
                name: "reports",
                newName: "reports",
                newSchema: "obstacledb");
        }
    }
}
