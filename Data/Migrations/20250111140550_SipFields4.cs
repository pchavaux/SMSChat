using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMSChat.Migrations
{
    /// <inheritdoc />
    public partial class SipFields4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SipNumber",
                table: "AspNetUsers",
                newName: "SipUsername");

            migrationBuilder.RenameColumn(
                name: "SipAccount",
                table: "AspNetUsers",
                newName: "SipPhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SipUsername",
                table: "AspNetUsers",
                newName: "SipNumber");

            migrationBuilder.RenameColumn(
                name: "SipPhoneNumber",
                table: "AspNetUsers",
                newName: "SipAccount");
        }
    }
}
