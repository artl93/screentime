using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenTimeAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceMinutes = table.Column<int>(type: "int", nullable: false),
                    WarningIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    WarningTimeMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExtensionRequestResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtensionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ForDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionRequestResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Heartbeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    Extensions = table.Column<TimeSpan>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Heartbeats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScreenTimeSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    Extensions = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenTimeSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SundayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MondayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TuesdayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WednesdayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ThursdayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FridayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SaturdayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_DefaultId",
                        column: x => x.DefaultId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_FridayId",
                        column: x => x.FridayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_MondayId",
                        column: x => x.MondayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_SaturdayId",
                        column: x => x.SaturdayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_SundayId",
                        column: x => x.SundayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_ThursdayId",
                        column: x => x.ThursdayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_TuesdayId",
                        column: x => x.TuesdayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WeeklyConfigurations_DailyConfigurations_WednesdayId",
                        column: x => x.WednesdayId,
                        principalTable: "DailyConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExtensionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    ExtensionRequestResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtensionRequests_ExtensionRequestResponses_ExtensionRequestResponseId",
                        column: x => x.ExtensionRequestResponseId,
                        principalTable: "ExtensionRequestResponses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExtensionRequests_ExtensionRequestResponseId",
                table: "ExtensionRequests",
                column: "ExtensionRequestResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_DefaultId",
                table: "WeeklyConfigurations",
                column: "DefaultId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_FridayId",
                table: "WeeklyConfigurations",
                column: "FridayId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_MondayId",
                table: "WeeklyConfigurations",
                column: "MondayId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_SaturdayId",
                table: "WeeklyConfigurations",
                column: "SaturdayId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_SundayId",
                table: "WeeklyConfigurations",
                column: "SundayId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_ThursdayId",
                table: "WeeklyConfigurations",
                column: "ThursdayId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_TuesdayId",
                table: "WeeklyConfigurations",
                column: "TuesdayId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyConfigurations_WednesdayId",
                table: "WeeklyConfigurations",
                column: "WednesdayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtensionRequests");

            migrationBuilder.DropTable(
                name: "Heartbeats");

            migrationBuilder.DropTable(
                name: "ScreenTimeSummaries");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WeeklyConfigurations");

            migrationBuilder.DropTable(
                name: "ExtensionRequestResponses");

            migrationBuilder.DropTable(
                name: "DailyConfigurations");
        }
    }
}
