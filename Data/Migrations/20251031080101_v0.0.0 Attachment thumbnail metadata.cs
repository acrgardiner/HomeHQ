using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectaardvarkx2.data.migrations
{
    /// <inheritdoc />
    public partial class v000Attachmentthumbnailmetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Thumb_ContentType",
                table: "Attachments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Thumb_Extension",
                table: "Attachments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "Thumb_FileSize",
                table: "Attachments",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "Thumb_LocalFileName",
                table: "Attachments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Thumb_ContentType",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "Thumb_Extension",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "Thumb_FileSize",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "Thumb_LocalFileName",
                table: "Attachments");
        }
    }
}
