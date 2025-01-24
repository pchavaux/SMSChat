using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMSChat.Migrations
{
    /// <inheritdoc />
    public partial class SipFields7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "APIPassword",
                table: "SystemCredentials",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "APIUrl",
                table: "SystemCredentials",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "APIPassword",
                table: "SystemCredentials");

            migrationBuilder.DropColumn(
                name: "APIUrl",
                table: "SystemCredentials");
        }
    }
}
