using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenTimeAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHeartbeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserState",
                table: "Heartbeats",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserState",
                table: "Heartbeats");
        }
    }
}
