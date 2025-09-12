using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectaardvarkx2.data.migrations
{
    /// <inheritdoc />
    public partial class v000Polymophism : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Assets_ParentId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Attributes_Assets_AssetId",
                table: "Attributes");

            migrationBuilder.DropIndex(
                name: "IX_Attributes_AssetId",
                table: "Attributes");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_ParentId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Attributes");

            migrationBuilder.AddColumn<string>(
                name: "ParentType",
                table: "Attributes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentType = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeValue_Parent",
                table: "Attributes",
                columns: new[] { "ParentId", "ParentType" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_Parent",
                table: "Attachments",
                columns: new[] { "ParentId", "ParentType" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Parent",
                table: "Notes",
                columns: new[] { "ParentId", "ParentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_AttributeValue_Parent",
                table: "Attributes");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_Parent",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "ParentType",
                table: "Attributes");

            migrationBuilder.AddColumn<Guid>(
                name: "AssetId",
                table: "Attributes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attributes_AssetId",
                table: "Attributes",
                column: "AssetId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Attributes_Assets_AssetId",
                table: "Attributes",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
