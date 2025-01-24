using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMSChat.Migrations
{
    /// <inheritdoc />
    public partial class SipFields9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SipPhoneNumber",
                table: "Friends",
                newName: "FriendSipPhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FriendSipPhoneNumber",
                table: "Friends",
                newName: "SipPhoneNumber");
        }
    }
}
