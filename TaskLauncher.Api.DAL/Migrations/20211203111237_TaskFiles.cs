using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskLauncher.Api.DAL.Migrations
{
    public partial class TaskFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Files_FileId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_FileId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Tasks");

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "Files",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Files_TaskId",
                table: "Files",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Tasks_TaskId",
                table: "Files",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Tasks_TaskId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_TaskId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Files");

            migrationBuilder.AddColumn<Guid>(
                name: "FileId",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_FileId",
                table: "Tasks",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Files_FileId",
                table: "Tasks",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");
        }
    }
}
