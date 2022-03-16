using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.Api.DAL.Migrations
{
    public partial class UpdateConfigEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanDelete",
                table: "Configs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Configs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanDelete",
                table: "Configs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Configs");
        }
    }
}
