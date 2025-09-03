using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YachtCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "ServiceRequests");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "ServiceRequests",
                newName: "CompletedOn");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceRequests",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ServiceRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ServiceRequests");

            migrationBuilder.RenameColumn(
                name: "CompletedOn",
                table: "ServiceRequests",
                newName: "Notes");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "ServiceRequests",
                type: "TEXT",
                nullable: true);
        }
    }
}
