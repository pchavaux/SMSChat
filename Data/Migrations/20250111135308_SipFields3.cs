using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMSChat.Migrations
{
    /// <inheritdoc />
    public partial class SipFields3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friends_ApplicationUser_UserId",
                table: "Friends");

            migrationBuilder.DropTable(
                name: "ApplicationUser");

    

            migrationBuilder.AddColumn<string>(
                name: "SipNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
 
 

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_AspNetUsers_UserId",
                table: "Friends",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friends_AspNetUsers_UserId",
                table: "Friends");

            migrationBuilder.DropColumn(
                name: "SipAccount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SipNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SipPassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SipServer",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "ApplicationUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SipAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SipNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SipPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SipServer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUser", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_ApplicationUser_UserId",
                table: "Friends",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
