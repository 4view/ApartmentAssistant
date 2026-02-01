using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentAssistant.Data.src.ApartmentAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNameNewParam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicationEntity");

            migrationBuilder.CreateTable(
                name: "TenementIndicationEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BathroomHotWater = table.Column<decimal>(type: "numeric", nullable: false),
                    BathroomColdWater = table.Column<decimal>(type: "numeric", nullable: false),
                    KitchenHotWater = table.Column<decimal>(type: "numeric", nullable: false),
                    KitchenColdWater = table.Column<decimal>(type: "numeric", nullable: false),
                    ContributionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenementIndicationEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenementIndicationEntity_UserEntity_UserId",
                        column: x => x.UserId,
                        principalTable: "UserEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenementIndicationEntity_UserId",
                table: "TenementIndicationEntity",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenementIndicationEntity");

            migrationBuilder.CreateTable(
                name: "IndicationEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BathroomColdWater = table.Column<decimal>(type: "numeric", nullable: false),
                    BathroomHotWater = table.Column<decimal>(type: "numeric", nullable: false),
                    KitchenColdWater = table.Column<decimal>(type: "numeric", nullable: false),
                    KitchenHotWater = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicationEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicationEntity_UserEntity_UserId",
                        column: x => x.UserId,
                        principalTable: "UserEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndicationEntity_UserId",
                table: "IndicationEntity",
                column: "UserId");
        }
    }
}
