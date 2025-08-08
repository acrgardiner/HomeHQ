using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectaardvarkx2.data.migrations
{
    /// <inheritdoc />
    public partial class v000ParentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentType",
                table: "Attachments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentType",
                table: "Attachments");
        }
    }
}
