using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SearchService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeSummaries",
                columns: table => new
                {
                    SummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSummaries", x => x.SummaryId);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSummarySkills",
                columns: table => new
                {
                    SummarySkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSummarySkills", x => x.SummarySkillId);
                    table.ForeignKey(
                        name: "FK_EmployeeSummarySkills_EmployeeSummaries_SummaryId",
                        column: x => x.SummaryId,
                        principalTable: "EmployeeSummaries",
                        principalColumn: "SummaryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSummaries_EmployeeId",
                table: "EmployeeSummaries",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSummarySkills_SummaryId_SkillName",
                table: "EmployeeSummarySkills",
                columns: new[] { "SummaryId", "SkillName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSummarySkills");

            migrationBuilder.DropTable(
                name: "EmployeeSummaries");
        }
    }
}
