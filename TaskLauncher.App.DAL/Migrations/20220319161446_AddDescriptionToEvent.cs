using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.App.DAL.Migrations
{
    public partial class AddDescriptionToEvent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Events");
        }
    }
}
