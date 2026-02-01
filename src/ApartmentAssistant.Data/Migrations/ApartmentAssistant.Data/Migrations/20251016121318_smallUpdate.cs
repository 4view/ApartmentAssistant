using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentAssistant.Data.src.ApartmentAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class smallUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IndicationEntity_UserEntity_UserId",
                table: "IndicationEntity");

            migrationBuilder.DropIndex(
                name: "IX_IndicationEntity_UserId",
                table: "IndicationEntity");

            migrationBuilder.DropColumn(
                name: "IdicationId",
                table: "UserEntity");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "IndicationEntity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdicationId",
                table: "UserEntity",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "IndicationEntity",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_IndicationEntity_UserId",
                table: "IndicationEntity",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_IndicationEntity_UserEntity_UserId",
                table: "IndicationEntity",
                column: "UserId",
                principalTable: "UserEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
