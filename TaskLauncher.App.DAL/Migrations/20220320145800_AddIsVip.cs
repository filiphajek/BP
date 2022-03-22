using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.App.DAL.Migrations
{
    public partial class AddIsVip : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPriority",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVip",
                table: "Stats",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPriority",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsVip",
                table: "Stats");
        }
    }
}
