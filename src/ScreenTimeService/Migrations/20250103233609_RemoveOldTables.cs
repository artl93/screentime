using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenTimeAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConfigurations");

            migrationBuilder.DropTable(
                name: "UserEvents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceMinutes = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarningIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    WarningTimeMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Event = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEvents", x => x.Id);
                });
        }
    }
}
