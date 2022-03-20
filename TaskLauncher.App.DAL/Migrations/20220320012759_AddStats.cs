using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.App.DAL.Migrations
{
    public partial class AddStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllTaskCount = table.Column<int>(type: "int", nullable: false),
                    FinishedTaskCount = table.Column<int>(type: "int", nullable: false),
                    FailedTasks = table.Column<int>(type: "int", nullable: false),
                    SuccessTasks = table.Column<int>(type: "int", nullable: false),
                    TimeoutedTasks = table.Column<int>(type: "int", nullable: false),
                    CrashedTasks = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stats");
        }
    }
}
