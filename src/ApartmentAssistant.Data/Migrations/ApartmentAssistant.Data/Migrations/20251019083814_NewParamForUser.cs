using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartmentAssistant.Data.src.ApartmentAssistant.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewParamForUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Login",
                table: "UserEntity",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "UserEntity",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Login",
                table: "UserEntity");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "UserEntity");
        }
    }
}
