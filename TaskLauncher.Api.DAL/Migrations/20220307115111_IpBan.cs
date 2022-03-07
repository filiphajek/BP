using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.Api.DAL.Migrations
{
    public partial class IpBan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Banned",
                table: "Ips",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banned",
                table: "Ips");
        }
    }
}
