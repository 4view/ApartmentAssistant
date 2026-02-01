using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentAssistant.Data.src.ApartmentAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "IndicationEntity",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_IndicationEntity_UserId",
                table: "IndicationEntity",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_IndicationEntity_UserEntity_UserId",
                table: "IndicationEntity",
                column: "UserId",
                principalTable: "UserEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IndicationEntity_UserEntity_UserId",
                table: "IndicationEntity");

            migrationBuilder.DropIndex(
                name: "IX_IndicationEntity_UserId",
                table: "IndicationEntity");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "IndicationEntity");
        }
    }
}
