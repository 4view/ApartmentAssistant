using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentAssistant.Data.src.ApartmentAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntityChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IndicationEntity_UserEntity_userId",
                table: "IndicationEntity");

            migrationBuilder.DropIndex(
                name: "IX_IndicationEntity_userId",
                table: "IndicationEntity");

            migrationBuilder.RenameColumn(
                name: "userId",
                table: "IndicationEntity",
                newName: "UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "IdicationId",
                table: "UserEntity",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "KitchenHotWater",
                table: "IndicationEntity",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "KitchenColdWater",
                table: "IndicationEntity",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "BathroomHotWater",
                table: "IndicationEntity",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "BathroomColdWater",
                table: "IndicationEntity",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

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
                name: "IdicationId",
                table: "UserEntity");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "IndicationEntity",
                newName: "userId");

            migrationBuilder.AlterColumn<int>(
                name: "KitchenHotWater",
                table: "IndicationEntity",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "KitchenColdWater",
                table: "IndicationEntity",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "BathroomHotWater",
                table: "IndicationEntity",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "BathroomColdWater",
                table: "IndicationEntity",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "IX_IndicationEntity_userId",
                table: "IndicationEntity",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK_IndicationEntity_UserEntity_userId",
                table: "IndicationEntity",
                column: "userId",
                principalTable: "UserEntity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
