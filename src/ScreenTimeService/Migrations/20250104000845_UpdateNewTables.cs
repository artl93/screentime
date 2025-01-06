using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenTimeAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extensions",
                table: "Heartbeats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "Extensions",
                table: "Heartbeats",
                type: "time",
                nullable: true);
        }
    }
}
