using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectaardvarkx2.data.migrations
{
    /// <inheritdoc />
    public partial class AttachtoParentnavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ParentId",
                table: "Attachments",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Assets_ParentId",
                table: "Attachments",
                column: "ParentId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Assets_ParentId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_ParentId",
                table: "Attachments");
        }
    }
}
