using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeHQ.data.migrations
{
    /// <inheritdoc />
    public partial class v000LastLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogin",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "Users");
        }
    }
}
