using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace YachtCRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddYardServiceRequestsFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YardID",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerFeedbacks",
                columns: table => new
                {
                    CustomerFeedbackID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Comments = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedOn = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFeedbacks", x => x.CustomerFeedbackID);
                    table.ForeignKey(
                        name: "FK_CustomerFeedbacks_Customers_CustomerID",
                        column: x => x.CustomerID,
                        principalTable: "Customers",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerFeedbacks_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    ServiceRequestID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectID = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Maintenance"),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false, defaultValue: "Open"),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.ServiceRequestID);
                    table.ForeignKey(
                        name: "FK_ServiceRequests_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Yards",
                columns: table => new
                {
                    YardID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yards", x => x.YardID);
                });

            migrationBuilder.InsertData(
                table: "Yards",
                columns: new[] { "YardID", "Brand", "Country", "Name" },
                values: new object[,]
                {
                    { 1, "Cantieri Demo", "Italy", "Cantieri Demo S.p.A." },
                    { 2, "Ferretti", "Italy", "Ferretti Group" },
                    { 3, "Azimut", "Italy", "Azimut|Benetti" },
                    { 4, "Sanlorenzo", "Italy", "Sanlorenzo" },
                    { 5, "Feadship", "Netherlands", "Feadship" },
                    { 6, "Lürssen", "Germany", "Lürssen" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_YardID",
                table: "Projects",
                column: "YardID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_CustomerID_ProjectID",
                table: "CustomerFeedbacks",
                columns: new[] { "CustomerID", "ProjectID" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedbacks_ProjectID",
                table: "CustomerFeedbacks",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_ProjectID_Status",
                table: "ServiceRequests",
                columns: new[] { "ProjectID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Yards_Name_Brand",
                table: "Yards",
                columns: new[] { "Name", "Brand" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Yards_YardID",
                table: "Projects",
                column: "YardID",
                principalTable: "Yards",
                principalColumn: "YardID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Yards_YardID",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "CustomerFeedbacks");

            migrationBuilder.DropTable(
                name: "ServiceRequests");

            migrationBuilder.DropTable(
                name: "Yards");

            migrationBuilder.DropIndex(
                name: "IX_Projects_YardID",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "YardID",
                table: "Projects");
        }
    }
}
