using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.Api.DAL.Migrations
{
    public partial class ConfigEntityUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanDelete",
                table: "Configs");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Configs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Bans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Bans");

            migrationBuilder.AddColumn<bool>(
                name: "CanDelete",
                table: "Configs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
