using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenTimeAPI.Migrations
{
    /// <inheritdoc />
    public partial class RewriteExtensionResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "ExtensionRequestResponses");

            migrationBuilder.RenameColumn(
                name: "ForDate",
                table: "ExtensionRequestResponses",
                newName: "GratedDateTime");

            migrationBuilder.RenameColumn(
                name: "ExtensionRequestId",
                table: "ExtensionRequestResponses",
                newName: "GrantedForUserId");

            migrationBuilder.RenameColumn(
                name: "Duration",
                table: "ExtensionRequestResponses",
                newName: "GrantedDuration");

            migrationBuilder.RenameColumn(
                name: "DateTime",
                table: "ExtensionRequestResponses",
                newName: "GrantedForDate");

            migrationBuilder.AddColumn<Guid>(
                name: "GrantedByUserId",
                table: "ExtensionRequestResponses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrantedByUserId",
                table: "ExtensionRequestResponses");

            migrationBuilder.RenameColumn(
                name: "GratedDateTime",
                table: "ExtensionRequestResponses",
                newName: "ForDate");

            migrationBuilder.RenameColumn(
                name: "GrantedForUserId",
                table: "ExtensionRequestResponses",
                newName: "ExtensionRequestId");

            migrationBuilder.RenameColumn(
                name: "GrantedForDate",
                table: "ExtensionRequestResponses",
                newName: "DateTime");

            migrationBuilder.RenameColumn(
                name: "GrantedDuration",
                table: "ExtensionRequestResponses",
                newName: "Duration");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "ExtensionRequestResponses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
